using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using RimTelligence.Source.AIStoryteller;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Verse;

namespace RimTelligence.Source.LlmCom
{
    public class BasicCom
    {
        private TornadoApi api = new TornadoApi(new List<ProviderAuthentication>
        {
            new ProviderAuthentication(LLmProviders.Groq, AIStorytellerMod.settings.grokApiKey ), // idk about this isolation thing quick workaround for now some time from now do it properly should be in different namespaces probably broad namespace just for settings
            new ProviderAuthentication(LLmProviders.Google, "YOUR_GOOGLE_KEY")
        });

        public async Task<string> GetLlmResponse(string prompt)
        {
            try
            {
                Conversation chat = api.Chat.CreateConversation(ChatModel.Groq.Meta.Llama4Scout);
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