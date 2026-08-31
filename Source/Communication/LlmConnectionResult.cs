namespace RimTelligence.Llm
{
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

        public static LlmConnectionResult Success(string providerId, string model, string responseText,
            int promptTokens = 0, int completionTokens = 0, int reasoningTokens = 0)
        {
            return new LlmConnectionResult
            {
                isSuccess = true,
                providerId = providerId,
                model = model,
                responseText = responseText,
                promptTokens = promptTokens,
                completionTokens = completionTokens,
                reasoningTokens = reasoningTokens
            };
        }

        public static LlmConnectionResult Failure(string providerId, string model, string errorMessage)
        {
            return new LlmConnectionResult
            {
                isSuccess = false,
                providerId = providerId,
                model = model,
                errorMessage = errorMessage
            };
        }
    }
}
