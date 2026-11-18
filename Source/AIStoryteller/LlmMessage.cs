using LlmTornado.Chat;

namespace RimTelligence.Source.AIStoryteller
{
    internal class LlmMessage : ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}