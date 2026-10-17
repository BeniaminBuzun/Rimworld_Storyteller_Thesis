using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.DataSnapshots;
using RimTelligence.Source.CustomEvents.Handlers;
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
            // TrimStart(char) is a Mono-only overload; pass an array so the call also resolves
            // on the desktop CLR (used by the out-of-game benchmark host).
            string trimmed = (rawResponse ?? string.Empty).TrimStart(BomMarker).Trim();
            string preview = BuildPreview(trimmed);

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return WithPreview(LlmParseResult<T>.Failure("Response is empty."), preview);
            }

            string candidate = ExtractFirstJsonValue(trimmed, out ExtractionFailure extractionFailure);
            if (candidate == null)
            {
                return WithPreview(LlmParseResult<T>.Failure(DescribeExtractionFailure(extractionFailure)), preview);
            }

            try
            {
                JToken token = JToken.Parse(candidate);

                if (token.Type == JTokenType.Array)
                {
                    if (token.Children().Any())
                    {
                        token = token[0];
                    }
                    else
                    {
                        return WithPreview(LlmParseResult<T>.Failure("No valid event object found in response."), preview);
                    }
                }

                NormalizeEventToken(token);

                T data = token.ToObject<T>(LenientSerializer);
                if (data == null)
                {
                    return WithPreview(LlmParseResult<T>.Failure("Response did not contain a usable payload."), preview);
                }

                return WithPreview(LlmParseResult<T>.Success(data), preview);
            }
            catch (Exception exception)
            {
                return WithPreview(LlmParseResult<T>.Failure(exception.Message), preview);
            }
        }

        private static void NormalizeEventToken(JToken token)
        {
            if (!(token is JObject obj)) return;

            // Normalize letterText if model returned an array of sentences
            if (obj["letterText"] is JArray textArray)
            {
                var sentences = textArray
                    .Select(t => t.ToString().Trim())
                    .Where(s => !string.IsNullOrEmpty(s));
                obj["letterText"] = string.Join(" ", sentences);
            }

            // If the model put effect properties at the top level instead of inside effects array:
            // e.g. { "letterTitle": "...", "letterText": "...", "effectName": "ResourcePodCrash", "selectedDefName": [...] }
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

            // Normalize effects array
            if (obj["effects"] is JArray effectsArray)
            {
                foreach (JToken effectToken in effectsArray)
                {
                    if (!(effectToken is JObject effectObj)) continue;

                    // Normalize effectName / type
                    if (effectObj["effectName"] == null && effectObj["type"] != null)
                    {
                        effectObj["effectName"] = effectObj["type"];
                    }

                    // Normalize selectedDefName / defName
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
            if (!result.isSuccess || result.data == null)
            {
                return result;
            }

            try
            {
                result.data.effects = LlmEffectValidator.StripInvalid(result.data.effects, chosenEvents);

                // If no valid effects were parsed but we know what the director cued, attempt
                // to recover by extracting the cued effects and any referenced defs from the narrative text.
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

            if (string.IsNullOrWhiteSpace(result.data.letterText))
            {
                result.data.letterText = "Something happened.";
            }
            if (string.IsNullOrWhiteSpace(result.data.letterTitle))
            {
                result.data.letterTitle = "Strange Event";
            }

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
                    {
                        primary = contract.AllowedDefs[0].DefName;
                    }
                    if (!string.IsNullOrEmpty(primary))
                    {
                        selectedDefs.Add(primary);
                    }

                    if (contract.DefCount > 1)
                    {
                        string secondary = FindDefInText(text, contract.SecondaryAllowedDefs);
                        if (string.IsNullOrEmpty(secondary) && contract.SecondaryAllowedDefs.Count > 0)
                        {
                            secondary = contract.SecondaryAllowedDefs[0].DefName;
                        }
                        if (!string.IsNullOrEmpty(secondary))
                        {
                            selectedDefs.Add(secondary);
                        }
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
                {
                    return item.DefName;
                }
            }

            foreach (var item in candidates.OrderByDescending(c => c.DefName?.Length ?? 0))
            {
                if (!string.IsNullOrWhiteSpace(item.DefName) &&
                    text.IndexOf(item.DefName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return item.DefName;
                }
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

        private static string ExtractFirstJsonValue(string raw, out ExtractionFailure failure)
        {
            failure = ExtractionFailure.NoJsonFound;
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

                string candidate = ScanBalanced(raw, index);
                if (candidate == null)
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
                    JToken.Parse(candidate);
                    failure = ExtractionFailure.None;
                    return candidate;
                }
                catch (JsonReaderException)
                {
                    index = index + candidate.Length;
                }
            }

            failure = sawOpener ? ExtractionFailure.Malformed : ExtractionFailure.NoJsonFound;
            return null;
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
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }
                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                }
                else if (current == '{' || current == '[')
                {
                    depth++;
                }
                else if (current == '}' || current == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return raw.Substring(start, position - start + 1);
                    }
                }
            }

            return null;
        }


        private static string BuildPreview(string raw)
        {
            if (raw == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (char character in raw)
            {
                if (char.IsControl(character) && character != '\n' && character != '\r' && character != '\t')
                {
                    builder.Append(' ');
                }
                else
                {
                    builder.Append(character);
                }

                if (builder.Length >= PreviewLength)
                {
                    break;
                }
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
