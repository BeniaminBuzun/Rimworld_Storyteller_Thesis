using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RimTelligence.CustomEvents.Handlers;
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

        private static readonly JsonSerializer LenientSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Error = HandleMemberConversionError
        });


        public static LlmParseResult<T> Parse<T>(string rawResponse)
        {
            string trimmed = (rawResponse ?? string.Empty).TrimStart('\uFEFF').Trim();
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
