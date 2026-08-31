namespace RimTelligence.CustomEvents
{

    public class LlmParseResult<T>
    {
        public bool isSuccess;

        public T data;

        public string errorMessage;

        public string responsePreview;

        public static LlmParseResult<T> Success(T data)
        {
            return new LlmParseResult<T> { isSuccess = true, data = data };
        }

        public static LlmParseResult<T> Failure(string errorMessage)
        {
            return new LlmParseResult<T> { isSuccess = false, errorMessage = errorMessage };
        }
    }
}
