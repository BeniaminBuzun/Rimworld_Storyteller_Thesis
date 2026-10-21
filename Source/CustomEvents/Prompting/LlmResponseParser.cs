using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimTelligence.CustomEvents.Prompting
{
    public static class LlmResponseParser
    {
        private const int MaxCandidates = 8;
        private const int PreviewLength = 300;

        private static readonly char[] BomMarker = { '\uFEFF' };

        private static readonly JsonSerializer LenientSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Error = HandleMemberConversionError
        });

        public static LlmParseResult<T> Parse<T>(string rawResponse)
        {
            string trimmed = (rawResponse ?? string.Empty).TrimStart(BomMarker).Trim();
            string preview = BuildPreview(trimmed);

            if (string.IsNullOrWhiteSpace(trimmed))
                return WithPreview(LlmParseResult<T>.Failure("Response is empty."), preview);

            // Extraction runs against `workingText`. If the first attempt fails because a
            // key's closing quote is missing (e.g. "selectedDefName:[...]), we re-run the
            // extraction against a repaired copy. Every subsequent offset (endIndex, merge
            // loop positions) is relative to `workingText`, so it must stay in sync.
            string workingText = trimmed;
            string candidate = ExtractFirstJsonValue(workingText,
                                                     out ExtractionFailure extractionFailure,
                                                     out int endIndex,
                                                     out bool repaired);

            bool missingQuoteRepaired = false;
            if (candidate == null)
            {
                string preRepaired = RepairMissingKeyQuotes(workingText);
                if (!string.Equals(preRepaired, workingText, StringComparison.Ordinal))
                {
                    string retryCandidate = ExtractFirstJsonValue(preRepaired,
                                                                  out extractionFailure,
                                                                  out endIndex,
                                                                  out bool retryRepaired);
                    if (retryCandidate != null)
                    {
                        candidate = retryCandidate;
                        workingText = preRepaired;
                        repaired = retryRepaired;
                        missingQuoteRepaired = true;
                        ModLogger.Warning("[Parser] Repaired missing quote in a key name.");
                    }
                }
            }

            if (candidate == null)
                return WithPreview(LlmParseResult<T>.Failure(DescribeExtractionFailure(extractionFailure)), preview);

            try
            {
                JToken token = JToken.Parse(candidate);
                bool mergedMultiple = false;

                if (token is JObject merged)
                {
                    int pos = endIndex;
                    int guard = 0;
                    while (pos < workingText.Length && guard++ < MaxCandidates)
                    {
                        string rest = workingText.Substring(pos);
                        string next = ExtractFirstJsonValue(rest, out _, out int nextEnd, out bool nextRepaired);
                        if (next == null) break;

                        try
                        {
                            JToken nextToken = JToken.Parse(next);
                            if (nextToken is JObject extra)
                            {
                                merged.Merge(extra, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                                mergedMultiple = true;
                                repaired |= nextRepaired;
                            }
                            else if (nextToken is JArray extraArray)
                            {
                                // Model emitted the effects array as a sibling of the letter
                                // object, e.g.  {"letterTitle":...,"letterText":...},"effects":[...]
                                // Adopt the array as the object's effects if not already present.
                                bool emptyOrMissing =
                                    merged["effects"] == null ||
                                    (merged["effects"] is JArray existing && existing.Count == 0);
                                if (emptyOrMissing)
                                {
                                    merged["effects"] = extraArray;
                                    mergedMultiple = true;
                                    repaired |= nextRepaired;
                                    ModLogger.Warning("[Parser] Loose top-level array adopted as 'effects'.");
                                }
                            }
                        }
                        catch (JsonReaderException) { }

                        pos += nextEnd;
                    }
                }

                NormalizeEventToken(token);

                T data = token.ToObject<T>(LenientSerializer);
                if (data == null)
                    return WithPreview(LlmParseResult<T>.Failure("Response did not contain a usable payload."), preview);

                LlmParseResult<T> ok = LlmParseResult<T>.Success(data);
                ok.wasRepaired = repaired || missingQuoteRepaired;
                ok.wasMergedFromMultipleObjects = mergedMultiple;
                return WithPreview(ok, preview);
            }
            catch (Exception exception)
            {
                return WithPreview(LlmParseResult<T>.Failure(exception.Message), preview);
            }
        }

        private static void NormalizeEventToken(JToken token)
        {
            if (!(token is JObject obj)) return;

            if (obj["letterText"] is JArray textArray)
            {
                var sentences = textArray
                    .Select(t => t.ToString().Trim())
                    .Where(s => !string.IsNullOrEmpty(s));
                obj["letterText"] = string.Join(" ", sentences);
            }

            if ((obj["effects"] == null || (obj["effects"] is JArray arr && arr.Count == 0))
                && (obj["effectName"] != null || obj["type"] != null))
            {
                var rootEffect = new JObject();
                if (obj["effectName"] != null) rootEffect["effectName"] = obj["effectName"];
                else if (obj["type"] != null) rootEffect["effectName"] = obj["type"];

                if (obj["selectedDefName"] != null) rootEffect["selectedDefName"] = obj["selectedDefName"];
                else if (obj["defName"] != null) rootEffect["selectedDefName"] = obj["defName"];

                if (obj["severity"] != null) rootEffect["severity"] = obj["severity"];
                if (obj["targetPawn"] != null) rootEffect["targetPawn"] = obj["targetPawn"];

                obj["effects"] = new JArray(rootEffect);
            }

            if (obj["effects"] is JArray effectsArray)
            {
                foreach (JToken effectToken in effectsArray)
                {
                    if (!(effectToken is JObject effectObj)) continue;

                    if (effectObj["effectName"] == null && effectObj["type"] != null)
                        effectObj["effectName"] = effectObj["type"];

                    JToken defToken = effectObj["selectedDefName"] ?? effectObj["defName"];
                    if (defToken != null)
                    {
                        if (defToken.Type == JTokenType.String)
                        {
                            string raw = defToken.ToString().Trim();
                            if (raw.Contains(","))
                            {
                                var parts = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(p => p.Trim())
                                    .Where(p => !string.IsNullOrEmpty(p));
                                effectObj["selectedDefName"] = new JArray(parts);
                            }
                            else if (!string.IsNullOrEmpty(raw))
                            {
                                effectObj["selectedDefName"] = new JArray(raw);
                            }
                            else
                            {
                                effectObj["selectedDefName"] = new JArray();
                            }
                        }
                        else if (defToken.Type == JTokenType.Array)
                        {
                            effectObj["selectedDefName"] = defToken;
                        }
                    }
                }
            }
        }

        public static LlmParseResult<LlmCustomEvent> ParseEvent(string rawResponse, IEnumerable<IEffect> chosenEvents = null)
        {
            LlmParseResult<LlmCustomEvent> result = Parse<LlmCustomEvent>(rawResponse);
            if (!result.isSuccess || result.data == null) return result;

            if (result.wasRepaired)
                ModLogger.Warning("[Parser] Syntax repaired before parsing.");
            if (result.wasMergedFromMultipleObjects)
                ModLogger.Warning("[Parser] Response contained several top-level JSON values; merged.");

            try
            {
                result.data.effects = LlmEffectValidator.StripInvalid(result.data.effects, chosenEvents);

                if ((result.data.effects == null || result.data.effects.Count == 0) && chosenEvents != null && chosenEvents.Any())
                {
                    var recovered = RecoverEffectsFromNarrative(result.data.letterText, chosenEvents);
                    if (recovered.Count > 0)
                    {
                        ModLogger.Warning($"[Parser] Model omitted 'effects' block. Recovered {recovered.Count} effect(s) from letter text.");
                        result.data.effects = LlmEffectValidator.StripInvalid(recovered, chosenEvents);
                    }
                }
            }
            catch (Exception ex)
            {
                return WithPreview(
                    LlmParseResult<LlmCustomEvent>.Failure("Validation threw: " + ex.Message),
                    BuildPreview(rawResponse));
            }

            if (result.data.effects == null || result.data.effects.Count == 0)
            {
                return WithPreview(
                    LlmParseResult<LlmCustomEvent>.Failure("The response contained no effects after validation. An effects array with at least one valid effect is required."),
                    BuildPreview(rawResponse));
            }

            if (string.IsNullOrWhiteSpace(result.data.letterText)) result.data.letterText = "Something happened.";
            if (string.IsNullOrWhiteSpace(result.data.letterTitle)) result.data.letterTitle = "Strange Event";

            return result;
        }

        private static List<GameEffect> RecoverEffectsFromNarrative(string letterText, IEnumerable<IEffect> chosenEvents)
        {
            var recovered = new List<GameEffect>();
            string text = letterText ?? string.Empty;

            foreach (var chosen in chosenEvents)
            {
                if (chosen?.Config == null) continue;
                EffectConfig contract = EffectContractProvider.GetContract(chosen, null);
                var effect = new GameEffect
                {
                    EffectName = contract.EffectName,
                    Severity = SeverityTypes.Medium
                };

                if (contract.IsDefRequired)
                {
                    var selectedDefs = new List<string>();

                    string primary = FindDefInText(text, contract.AllowedDefs);
                    if (string.IsNullOrEmpty(primary) && contract.AllowedDefs.Count > 0)
                        primary = contract.AllowedDefs[0].DefName;
                    if (!string.IsNullOrEmpty(primary)) selectedDefs.Add(primary);

                    if (contract.DefCount > 1)
                    {
                        string secondary = FindDefInText(text, contract.SecondaryAllowedDefs);
                        if (string.IsNullOrEmpty(secondary) && contract.SecondaryAllowedDefs.Count > 0)
                            secondary = contract.SecondaryAllowedDefs[0].DefName;
                        if (!string.IsNullOrEmpty(secondary)) selectedDefs.Add(secondary);
                    }

                    effect.SelectedDefName = selectedDefs.ToArray();
                }
                else
                {
                    effect.SelectedDefName = new string[0];
                }

                recovered.Add(effect);
            }

            return recovered;
        }

        private static string FindDefInText(string text, List<GameObjectMeta> candidates)
        {
            if (candidates == null || candidates.Count == 0 || string.IsNullOrEmpty(text))
                return null;

            foreach (var item in candidates.OrderByDescending(c => c.Label?.Length ?? 0))
            {
                if (!string.IsNullOrWhiteSpace(item.Label) &&
                    text.IndexOf(item.Label, StringComparison.OrdinalIgnoreCase) >= 0)
                    return item.DefName;
            }

            foreach (var item in candidates.OrderByDescending(c => c.DefName?.Length ?? 0))
            {
                if (!string.IsNullOrWhiteSpace(item.DefName) &&
                    text.IndexOf(item.DefName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return item.DefName;
            }

            return null;
        }

        private enum ExtractionFailure
        {
            None,
            NoJsonFound,
            Malformed,
            Truncated
        }

        private static string ExtractFirstJsonValue(string raw, out ExtractionFailure failure,
                                                    out int endIndex, out bool repaired)
        {
            failure = ExtractionFailure.NoJsonFound;
            endIndex = -1;
            repaired = false;
            int candidatesValidated = 0;
            int index = 0;
            int length = raw.Length;
            bool sawOpener = false;

            while (index < length)
            {
                char current = raw[index];
                if (current != '{' && current != '[')
                {
                    index++;
                    continue;
                }

                sawOpener = true;

                string scanned = ScanBalanced(raw, index);
                if (scanned == null)
                {
                    failure = ExtractionFailure.Truncated;
                    return null;
                }

                candidatesValidated++;
                if (candidatesValidated > MaxCandidates)
                {
                    failure = ExtractionFailure.Malformed;
                    return null;
                }

                try
                {
                    JToken.Parse(scanned);
                    failure = ExtractionFailure.None;
                    endIndex = index + scanned.Length;
                    return scanned;
                }
                catch (JsonReaderException)
                {
                    string fixedJson = RepairSyntax(scanned);
                    if (!string.Equals(fixedJson, scanned, StringComparison.Ordinal))
                    {
                        try
                        {
                            JToken.Parse(fixedJson);
                            failure = ExtractionFailure.None;
                            endIndex = index + scanned.Length;
                            repaired = true;
                            return fixedJson;
                        }
                        catch (JsonReaderException) { }
                    }

                    index += scanned.Length;
                }
            }

            failure = sawOpener ? ExtractionFailure.Malformed : ExtractionFailure.NoJsonFound;
            return null;
        }

        /// <summary>
        /// Inserts missing commas between members. String-aware; never touches text
        /// inside string values. This is the existing "missing comma" repair.
        /// </summary>
        private static string RepairSyntax(string json)
        {
            var sb = new StringBuilder(json.Length + 8);
            bool inString = false, escaped = false;
            char lastSignificant = '\0';

            foreach (char c in json)
            {
                if (inString)
                {
                    sb.Append(c);
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') { inString = false; lastSignificant = '"'; }
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                    continue;
                }

                bool opensValue = c == '"' || c == '{' || c == '[';
                bool previousEndsValue = lastSignificant == '"' || lastSignificant == '}' || lastSignificant == ']'
                                         || char.IsLetterOrDigit(lastSignificant);
                if (opensValue && previousEndsValue)
                {
                    int insertAt = sb.Length;
                    while (insertAt > 0 && char.IsWhiteSpace(sb[insertAt - 1])) insertAt--;
                    sb.Insert(insertAt, ',');
                    lastSignificant = ',';
                }

                sb.Append(c);
                if (c == '"') inString = true;
                else lastSignificant = c;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Repairs the common model bug where the closing quote of a key is dropped
        /// immediately before an array/object opener, e.g.
        ///     "selectedDefName:[ ...      →     "selectedDefName":[ ...
        ///     "payload:{ ...             →     "payload":{ ...
        ///
        /// Fires only when we are inside an object, expecting a key, currently inside
        /// a string, and the content accumulated so far looks like a bare identifier.
        /// This keeps legitimate string values containing ':' (e.g. narrative text)
        /// intact, and never touches valid `"key":[` sequences.
        /// </summary>
        private static string RepairMissingKeyQuotes(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;

            var sb = new StringBuilder(json.Length + 8);
            var stack = new Stack<char>();       // 'o' = object, 'a' = array
            var stringContent = new StringBuilder();
            bool inString = false;
            bool escaped = false;
            bool expectingKey = false;
            bool changed = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (inString)
                {
                    if (escaped)
                    {
                        sb.Append(c);
                        if (expectingKey) stringContent.Append(c);
                        escaped = false;
                        continue;
                    }
                    if (c == '\\')
                    {
                        sb.Append(c);
                        if (expectingKey) stringContent.Append(c);
                        escaped = true;
                        continue;
                    }
                    if (c == '"')
                    {
                        sb.Append(c);
                        inString = false;
                        if (expectingKey) expectingKey = false;
                        stringContent.Clear();
                        continue;
                    }

                    // Repair point: inside a key string, hit ':' followed by '[' or '{'.
                    if (c == ':' && expectingKey
                        && stack.Count > 0 && stack.Peek() == 'o'
                        && LooksLikeKey(stringContent))
                    {
                        int j = i + 1;
                        while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
                        if (j < json.Length && (json[j] == '[' || json[j] == '{'))
                        {
                            sb.Append('"');
                            inString = false;
                            expectingKey = false;
                            stringContent.Clear();
                            changed = true;
                            // Fall through to append ':' below.
                        }
                        else
                        {
                            sb.Append(c);
                            stringContent.Append(c);
                            continue;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                        if (expectingKey) stringContent.Append(c);
                        continue;
                    }
                }

                if (char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '"':
                        sb.Append(c);
                        inString = true;
                        if (expectingKey) stringContent.Clear();
                        break;

                    case '{':
                        sb.Append(c);
                        stack.Push('o');
                        expectingKey = true;
                        break;

                    case '[':
                        sb.Append(c);
                        stack.Push('a');
                        expectingKey = false;
                        break;

                    case '}':
                    case ']':
                        sb.Append(c);
                        if (stack.Count > 0) stack.Pop();
                        // After closing a container we've just ended a value; the next
                        // significant token must be ',' or a matching closer. ',' will
                        // re-arm expectingKey if the parent is an object.
                        expectingKey = false;
                        break;

                    case ',':
                        sb.Append(c);
                        expectingKey = stack.Count > 0 && stack.Peek() == 'o';
                        break;

                    case ':':
                        sb.Append(c);
                        expectingKey = false;
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            return changed ? sb.ToString() : json;
        }

        private static bool LooksLikeKey(StringBuilder content)
        {
            if (content == null || content.Length == 0) return false;
            for (int i = 0; i < content.Length; i++)
            {
                char ch = content[i];
                bool ok = (ch >= 'a' && ch <= 'z')
                          || (ch >= 'A' && ch <= 'Z')
                          || (ch >= '0' && ch <= '9')
                          || ch == '_';
                if (!ok) return false;
            }
            return true;
        }

        private static string DescribeExtractionFailure(ExtractionFailure failure)
        {
            switch (failure)
            {
                case ExtractionFailure.Truncated:
                    return "Response was cut off mid-JSON (truncated output). The model likely ran out of output tokens.";
                case ExtractionFailure.Malformed:
                    return "Response contains JSON but it could not be parsed (malformed).";
                default:
                    return "No JSON object or array found in response.";
            }
        }

        private static string ScanBalanced(string raw, int start)
        {
            int depth = 1;
            bool inString = false;
            bool escaped = false;
            int length = raw.Length;

            for (int position = start + 1; position < length; position++)
            {
                char current = raw[position];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (current == '\\') escaped = true;
                    else if (current == '"') inString = false;
                    continue;
                }

                if (current == '"') inString = true;
                else if (current == '{' || current == '[') depth++;
                else if (current == '}' || current == ']')
                {
                    depth--;
                    if (depth == 0) return raw.Substring(start, position - start + 1);
                }
            }

            return null;
        }

        private static string BuildPreview(string raw)
        {
            if (raw == null) return string.Empty;

            StringBuilder builder = new StringBuilder();
            foreach (char character in raw)
            {
                if (char.IsControl(character) && character != '\n' && character != '\r' && character != '\t')
                    builder.Append(' ');
                else
                    builder.Append(character);

                if (builder.Length >= PreviewLength) break;
            }

            return builder.ToString();
        }

        private static void HandleMemberConversionError(object sender, ErrorEventArgs args)
        {
            args.ErrorContext.Handled = true;
        }

        private static LlmParseResult<T> WithPreview<T>(LlmParseResult<T> result, string preview)
        {
            result.responsePreview = preview;
            return result;
        }
    }
}