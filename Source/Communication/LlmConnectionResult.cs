namespace RimTelligence.Llm
{
    public enum LlmOutputMode
    {
        /// <summary>Schema if one is supplied, otherwise plain text.</summary>
        Auto,
        Text,
        Json,
        Schema
    }

    public enum LlmErrorKind
    {
        None,
        Config,       // bad local settings (missing key / model / url). Never retry.
        Auth,         // 401 / 403. Never retry.
        Quota,        // billing or insufficient_quota. Never retry.
        NotFound,     // wrong model or endpoint. Never retry.
        Unsupported,  // provider rejected a feature (json_schema, response_format, reasoning...). Downgrade mode.
        BadRequest,   // other 400 / 422. Downgrade mode for this call only.
        RateLimit,    // 429. Retry with backoff.
        Network,      // connection problems. Retry with backoff.
        Timeout,      // retry with backoff.
        Server,       // 5xx / overloaded. Retry with backoff.
        Unknown
    }

    public class LlmConnectionResult
    {
        public bool isSuccess;
        public string providerId;
        public string model;
        public string responseText;
        public string errorMessage;
        public int promptTokens;
        public int completionTokens;
        public int reasoningTokens;

        public LlmErrorKind errorKind = LlmErrorKind.None;
        public string modeUsed;

        /// <summary>True when trying the same request again later could plausibly work.</summary>
        public bool retryable
        {
            get
            {
                return errorKind == LlmErrorKind.RateLimit
                    || errorKind == LlmErrorKind.Network
                    || errorKind == LlmErrorKind.Timeout
                    || errorKind == LlmErrorKind.Server;
            }
        }

        public static LlmConnectionResult Success(string providerId, string model, string responseText,
            int promptTokens = 0, int completionTokens = 0, int reasoningTokens = 0, string modeUsed = null)
        {
            return new LlmConnectionResult
            {
                isSuccess = true,
                providerId = providerId,
                model = model,
                responseText = responseText,
                promptTokens = promptTokens,
                completionTokens = completionTokens,
                reasoningTokens = reasoningTokens,
                modeUsed = modeUsed
            };
        }

        public static LlmConnectionResult Failure(string providerId, string model, string errorMessage,
            LlmErrorKind kind = LlmErrorKind.Unknown)
        {
            return new LlmConnectionResult
            {
                isSuccess = false,
                providerId = providerId,
                model = model,
                errorMessage = errorMessage,
                errorKind = kind
            };
        }
    }
}