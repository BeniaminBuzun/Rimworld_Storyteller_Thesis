using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace RimTelligence.Llm
{
    public sealed class LlmErrorInfo
    {
        public LlmErrorKind Kind;
        public string Message;        // short, user-facing
        public string Raw;            // lower-cased flattened exception text, for keyword checks
        public TimeSpan? RetryAfter;  // parsed from the message when the provider tells us

        public bool IsRetryable
        {
            get
            {
                return Kind == LlmErrorKind.RateLimit || Kind == LlmErrorKind.Network
                    || Kind == LlmErrorKind.Timeout || Kind == LlmErrorKind.Server;
            }
        }
    }

    /// <summary>
    /// Turns an exception into a kind we can act on. LlmTornado exposes status codes only through
    /// the exception text, so this is keyword based. Verify the keywords against the real
    /// messages your providers produce (check the log on a deliberately wrong key / model).
    /// </summary>
    public static class LlmErrorClassifier
    {
        private static readonly Regex RetryAfterSeconds = new Regex(
            @"(?:retry[- ]after[""':\s]+|try again in\s+|retry in\s+)(\d+(?:\.\d+)?)\s*(ms|s|sec|seconds)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex StatusCode = new Regex(@"\b([45]\d\d)\b", RegexOptions.Compiled);

        public static LlmErrorInfo Classify(Exception exception)
        {
            string raw = Flatten(exception).ToLowerInvariant();
            LlmErrorKind kind = Decide(exception, raw);
            return new LlmErrorInfo
            {
                Kind = kind,
                Raw = raw,
                Message = Describe(kind),
                RetryAfter = ParseRetryAfter(raw)
            };
        }

        public static LlmErrorInfo Make(LlmErrorKind kind, string message)
        {
            return new LlmErrorInfo { Kind = kind, Message = message, Raw = (message ?? "").ToLowerInvariant() };
        }

        private static LlmErrorKind Decide(Exception ex, string raw)
        {
            if (ex is TimeoutException || ex is OperationCanceledException
                || raw.Contains("timed out") || raw.Contains("timeout"))
                return LlmErrorKind.Timeout;

            // OpenAI reports "insufficient_quota" as a 429, but retrying it never helps.
            if (raw.Contains("insufficient_quota") || raw.Contains("exceeded your current quota")
                || raw.Contains("billing") || raw.Contains("credit balance") || raw.Contains("payment required"))
                return LlmErrorKind.Quota;

            if (raw.Contains("429") || raw.Contains("rate limit") || raw.Contains("rate_limit")
                || raw.Contains("too many requests") || raw.Contains("toomanyrequests"))
                return LlmErrorKind.RateLimit;

            if (raw.Contains("401") || raw.Contains("403") || raw.Contains("unauthorized")
                || raw.Contains("invalid api key") || raw.Contains("incorrect api key")
                || raw.Contains("authentication") || raw.Contains("permission denied") || raw.Contains("forbidden"))
                return LlmErrorKind.Auth;

            if (raw.Contains("404") || raw.Contains("model_not_found") || raw.Contains("not found")
                || raw.Contains("does not exist"))
                return LlmErrorKind.NotFound;

            // Feature rejections. Order matters: check before the generic 400.
            if (raw.Contains("response_format") || raw.Contains("json_schema") || raw.Contains("json_object")
                || raw.Contains("structured output") || raw.Contains("reasoning")
                || raw.Contains("unsupported") || raw.Contains("not supported") || raw.Contains("does not support")
                || raw.Contains("unknown parameter") || raw.Contains("unrecognized request argument")
                || raw.Contains("extra inputs are not permitted"))
                return LlmErrorKind.Unsupported;

            if (raw.Contains("400") || raw.Contains("422") || raw.Contains("invalid_request")
                || raw.Contains("bad request"))
                return LlmErrorKind.BadRequest;

            if (raw.Contains("500") || raw.Contains("502") || raw.Contains("503") || raw.Contains("504")
                || raw.Contains("529") || raw.Contains("internal server error") || raw.Contains("bad gateway")
                || raw.Contains("service unavailable") || raw.Contains("overloaded"))
                return LlmErrorKind.Server;

            if (ex is HttpRequestException || ex is WebException || ex is System.IO.IOException
                || raw.Contains("connection") || raw.Contains("socket") || raw.Contains("name resolution")
                || raw.Contains("network") || raw.Contains("ssl") || raw.Contains("could not resolve"))
                return LlmErrorKind.Network;

            if (raw.Contains("invalid uri") || raw.Contains("invalid url") || raw.Contains("uri"))
                return LlmErrorKind.Config;

            // Last resort: a bare status code somewhere in the text.
            Match code = StatusCode.Match(raw);
            if (code.Success)
            {
                int value = int.Parse(code.Groups[1].Value);
                if (value >= 500) return LlmErrorKind.Server;
                if (value == 408) return LlmErrorKind.Timeout;
                return LlmErrorKind.BadRequest;
            }

            return LlmErrorKind.Unknown;
        }

        public static string Describe(LlmErrorKind kind)
        {
            switch (kind)
            {
                case LlmErrorKind.Config: return "Invalid provider configuration or server URL.";
                case LlmErrorKind.Auth: return "Authentication failed. Check the API key.";
                case LlmErrorKind.Quota: return "Provider quota or billing problem. Check your account.";
                case LlmErrorKind.NotFound: return "Provider endpoint or model not found. Check the model name and server URL.";
                case LlmErrorKind.Unsupported: return "The model rejected a request feature (structured output / JSON mode / reasoning).";
                case LlmErrorKind.BadRequest: return "The provider rejected the request (HTTP 400).";
                case LlmErrorKind.RateLimit: return "Rate limit exceeded (HTTP 429).";
                case LlmErrorKind.Network: return "Connection failed. Check the server URL or your network.";
                case LlmErrorKind.Timeout: return "The request timed out.";
                case LlmErrorKind.Server: return "The provider had a server-side error.";
                default: return "LLM error.";
            }
        }

        private static TimeSpan? ParseRetryAfter(string raw)
        {
            Match match = RetryAfterSeconds.Match(raw);
            if (!match.Success) return null;

            double value;
            if (!double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out value))
                return null;

            string unit = match.Groups[2].Value;
            double seconds = unit == "ms" ? value / 1000.0 : value;
            return TimeSpan.FromSeconds(Math.Min(seconds, 60.0));
        }

        private static string Flatten(Exception exception)
        {
            var sb = new StringBuilder();
            for (Exception e = exception; e != null; e = e.InnerException)
                sb.Append(e.GetType().Name).Append(": ").Append(e.Message).Append(' ');
            return sb.ToString();
        }
    }
}