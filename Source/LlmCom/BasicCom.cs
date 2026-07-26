using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Verse;

using RimTelligence.Source.AIStoryteller;


namespace RimTelligence.Source.LlmCom
{
    public class BasicCom
    {
        private TornadoApi api = new TornadoApi(new List<ProviderAuthentication>
        {
            new ProviderAuthentication(LLmProviders.Groq, AIStorytellerMod.Settings.grokApiKey ),
            new ProviderAuthentication(LLmProviders.Google, "YOUR_GOOGLE_KEY")
        });

        public async Task<string> GetLlmResponse(string prompt)
        {
            try
            {
                Conversation chat = api.Chat.CreateConversation(ChatModel.Groq.Meta.Llama3370BVersatile);
                chat.AppendUserInput(prompt);
                string response = await chat.GetResponse();
                return response;
            }
            catch (Exception ex)
            {
                Log.Error($"[FirstMod] LLM error: {ex.Message}");
                return null;
            }
        }
    }
}