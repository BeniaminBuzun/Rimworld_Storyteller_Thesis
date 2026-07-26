using LudeonTK;
using Newtonsoft.Json;
using RimTelligence.Source.CustomEvents;
using RimTelligence.Source.CustomEvents.Handlers;
using RimTelligence.Source.CustomEvents.Handlers.Implementations.ItemsRelated;
using RimTelligence.Source.CustomEvents.Workers;
using RimTelligence.Source.CustomEvents.Workers.RimTelligence.Source.Debug;
using RimTelligence.Source.DataSnapshots;
using RimTelligence.Source.Memory;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimTelligence.Source.Debug
{
    public static class RimTelligenceDebugActions
    {
        [DebugAction("RimTelligence", "Test Effect Handler", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestEffectHandler()
        {
            Map map = Find.CurrentMap;
            List<DebugMenuOption> options = new List<DebugMenuOption>();

            foreach (var handler in EffectHandlerRegistry.GetAllHandlers())
            {
                var capturedHandler = handler;
                options.Add(new DebugMenuOption(
                    capturedHandler.EffectName,
                    DebugMenuOptionMode.Action,
                    () => OpenParamDialog(capturedHandler, map)
                ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("RimTelligence", "Test RAG Query", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestRagQuery()
        {
            Find.WindowStack.Add(new Dialog_RagQuery());
        }

        [DebugAction("RimTelligence", "Open Event History Debug", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OpenEventDebugWindow()
        {
            Find.WindowStack.Add(new DebugEventWindow());
        }

        private static void OpenParamDialog(IEffectHandler handler, Map map)
        {
            string colonistName = map.mapPawns.FreeColonists.FirstOrDefault()?.Name.ToStringShort ?? "";

            GameEffect effect = new GameEffect
            {
                type = handler.EffectName,
                defName = "",
                count = 0,
                value = SeverityTypes.None,
                targetPawn = colonistName
            };

            Find.WindowStack.Add(new Dialog_EffectTest(effect, map));
        }
    }

    public class Dialog_RagQuery : Window
    {
        private string queryBuf = "";
        private int topK = 5;
        private string topKBuf = "5";
        private List<RagResult> lastResults = new List<RagResult>();
        private Vector2 resultsScroll;

        public override Vector2 InitialSize => new Vector2(600f, 500f);

        public Dialog_RagQuery()
        {
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard ui = new Listing_Standard();
            ui.Begin(inRect);

            Text.Font = GameFont.Medium;
            ui.Label("RAG Query Test");
            Text.Font = GameFont.Small;
            ui.GapLine();

            ui.Label("Query text:");
            queryBuf = ui.TextEntry(queryBuf);

            ui.Gap(6f);
            ui.TextFieldNumericLabeled("topK: ", ref topK, ref topKBuf, 1f, 20f);

            ui.Gap(10f);
            GUI.color = Color.green;
            if (ui.ButtonText("Run Query"))
            {
                RunQuery();
            }
            GUI.color = Color.white;

            ui.GapLine();
            ui.Label($"Results ({lastResults.Count}):");

            Rect outRect = ui.GetRect(250f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, lastResults.Count * 60f);

            Widgets.BeginScrollView(outRect, ref resultsScroll, viewRect);
            float y = 0f;
            foreach (var r in lastResults)
            {
                Rect rowRect = new Rect(0f, y, viewRect.width, 56f);
                Widgets.DrawMenuSection(rowRect);

                Rect scoreLabel = new Rect(rowRect.x + 4f, rowRect.y + 2f, rowRect.width - 8f, 18f);
                Widgets.Label(scoreLabel, $"[{r.Score:F3}] ({r.Source})");

                Rect textLabel = new Rect(rowRect.x + 4f, rowRect.y + 20f, rowRect.width - 8f, 34f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(textLabel, r.Text);
                Text.Font = GameFont.Small;

                y += 60f;
            }
            Widgets.EndScrollView();

            ui.End();
        }

        private void RunQuery()
        {
            if (string.IsNullOrWhiteSpace(queryBuf))
            {
                Log.Message("[RimTelligence] Empty query, skipped.");
                return;
            }

            float[] embedding = GameObjectsCapturer.EmbeddingProvider.Embed(queryBuf);
            var store = new StaticRagStore(RagBootstrap.DbPath);
            lastResults = store.Query(embedding, topK: topK);
            resultsScroll = Vector2.zero;

            Log.Message($"[RimTelligence] Query: \"{queryBuf}\" — {lastResults.Count} results:");
            foreach (var r in lastResults)
                Log.Message($"  [{r.Score:F3}] ({r.Source}) {r.Text}");
        }
    }

    public class Dialog_EffectTest : Window
    {
        private readonly Map map;
        private readonly GameEffect effect;

        private string defNameBuf;
        private string defDescriptionBuf;
        private string pawnBuf;
        private string countBuf;

        private Vector2 pawnScroll;
        private Vector2 defScroll;
        private Vector2 mainScroll; // Added for the main window scrolling

        private const float ButtonHeight = 30f;
        private const float ScrollSectionHeight = 150f;

        public override Vector2 InitialSize => new Vector2(500f, 700f);

        public Dialog_EffectTest(GameEffect effect, Map map)
        {
            this.effect = effect;
            this.map = map;

            defNameBuf = effect.defName ?? "";
            defDescriptionBuf = effect.defDescription ?? "";
            pawnBuf = effect.targetPawn ?? "";
            countBuf = effect.count.ToString();

            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 1. Define the viewing area (outRect) and the scrolling area (viewRect)
            // Subtract space for the default "Close" button at the bottom
            Rect outRect = new Rect(0f, 0f, inRect.width, inRect.height - CloseButSize.y - 10f);

            // We set a large height for viewRect to accommodate all fields and lists
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, 1000f);

            Widgets.BeginScrollView(outRect, ref mainScroll, viewRect);

            Listing_Standard ui = new Listing_Standard();
            ui.Begin(viewRect);

            ui.Label($"<color=orange>Handler: {effect.type}</color>");
            ui.GapLine();

            var handler = EffectHandlerRegistry.GetHandler(effect.type);
            if (handler != null)
            {
                Text.Font = GameFont.Tiny;
                ui.Label($"Prompt Hint: {handler.PromptStructure}");
                Text.Font = GameFont.Small;
                ui.Gap();
            }

            //  Fields 
            defNameBuf = ui.TextEntryLabeled("defName:        ", defNameBuf);
            defDescriptionBuf = ui.TextEntryLabeled("defDescription: ", defDescriptionBuf);
            pawnBuf = ui.TextEntryLabeled("targetPawn:     ", pawnBuf);
            ui.TextFieldNumericLabeled("count: ", ref effect.count, ref countBuf, 0f, 100000f);

            //  ValueType selector 
            ui.Gap(4f);
            ui.Label($"Value Strategy: <color=cyan>{effect.value}</color>");

            Rect valueRow = ui.GetRect(ButtonHeight);
            float btnW = valueRow.width / 4f;

            DrawValueButton(valueRow, 0, btnW, SeverityTypes.None, "None");
            DrawValueButton(valueRow, 1, btnW, SeverityTypes.Low, "LOW");
            DrawValueButton(valueRow, 2, btnW, SeverityTypes.Medium, "MEDIUM");
            DrawValueButton(valueRow, 3, btnW, SeverityTypes.High, "HIGH");

            ui.GapLine();

            //  Pawn quick-fill 
            ui.Label("Quick-fill targetPawn:");
            DrawScrollableButtons(
                ui,
                map.mapPawns.FreeColonists.Select(p => p.Name.ToStringShort).ToList(),
                ref pawnScroll,
                val => pawnBuf = val
            );

            //  Def quick-fill 
            List<string> defList = GetDefList(effect.type);
            if (defList != null && defList.Count > 0)
            {
                ui.GapLine();
                ui.Label($"Quick-fill defName ({effect.type}):");
                DrawScrollableButtons(
                    ui,
                    defList,
                    ref defScroll,
                    val => defNameBuf = val
                );
            }

            ui.Gap(25f);

            //  Fire Button 
            GUI.color = Color.green;
            if (ui.ButtonText("FIRE EFFECT NOW"))
            {
                FireEffect();
            }
            GUI.color = Color.white;

            // Update the scroll height dynamically based on the Listing_Standard cursor position
            float finalHeight = ui.CurHeight + 50f;
            if (finalHeight > viewRect.height)
            {
                // This will trigger a re-draw on the next frame with a correct scroll height
                // but usually fixed numbers (like 1000f) work fine for debug tools.
            }

            ui.End();
            Widgets.EndScrollView();
        }

        private void DrawValueButton(Rect row, int index, float btnW, SeverityTypes v, string label)
        {
            Rect r = new Rect(row.x + index * btnW, row.y, btnW - 2f, row.height);

            bool active = effect.value == v;
            if (active)
                Widgets.DrawBoxSolid(r, new Color(0.3f, 0.6f, 0.3f, 0.4f));

            if (Widgets.ButtonText(r, label))
                effect.value = v;
        }

        private void DrawScrollableButtons(
            Listing_Standard ui,
            List<string> items,
            ref Vector2 scroll,
            System.Action<string> onSelect)
        {
            if (items == null || items.Count == 0)
            {
                ui.Label("  (none available)");
                return;
            }

            float totalHeight = items.Count * ButtonHeight;
            float viewHeight = Mathf.Min(totalHeight, ScrollSectionHeight);

            // Get a rect from the listing
            Rect outerRect = ui.GetRect(viewHeight);
            Widgets.DrawMenuSection(outerRect); // Visual border

            Rect innerRect = new Rect(0f, 0f, outerRect.width - 20f, totalHeight);

            Widgets.BeginScrollView(outerRect, ref scroll, innerRect);
            float y = 0f;
            foreach (string item in items)
            {
                Rect btnRect = new Rect(5f, y, innerRect.width - 5f, ButtonHeight - 2f);
                if (Widgets.ButtonText(btnRect, item))
                    onSelect(item);
                y += ButtonHeight;
            }
            Widgets.EndScrollView();
            ui.Gap(4f);
        }

        private static List<string> GetDefList(string effectType)
        {
            string family = effectType.Contains("_")
                ? effectType.Substring(0, effectType.IndexOf('_'))
                : effectType;

            switch (family)
            {
                case "ResourcePodCrash":
                case "PawnFoundItem":
                case "LoseItems":
                    return EventItemMapping.GetAllowedDefs(effectType);

                case "DiseasePawn":
                    return GameObjectsCapturer.DiseaseList.Select(m => m.DefName).ToList();

                case "DamagePawn":
                    return GameObjectsCapturer.DamageTypeList.Select(m => m.DefName).ToList();

                case "AddExpPoints":
                case "LoseExpPoints":
                    return GameObjectsCapturer.AllSkillsList.Select(m => m.DefName).ToList();

                case "MiracleHealDisease":
                    return GameObjectsCapturer.DiseaseList.Select(m => m.DefName).ToList();

                case "ChangeWeather":
                    return GameObjectsCapturer.AllWeathersList.Select(m => m.DefName).ToList();

                case "GameCondition":
                    return GameObjectsCapturer.GameConditionList.Select(m => m.DefName).ToList();

                case "AnimalInsanity":
                    return GameObjectsCapturer.AnimalList.Select(m => m.DefName).ToList();

                default:
                    return null;
            }
        }

        private void FireEffect()
        {
            effect.defName = defNameBuf;
            effect.defDescription = defDescriptionBuf;
            effect.targetPawn = pawnBuf;

            LlmCustomEvent evt = new LlmCustomEvent
            {
                letterTitle = $"Test: {effect.type}",
                letterText = $"Debug manual trigger of {effect.type}.",
                effects = new List<GameEffect> { effect }
            };

            string testJson = JsonConvert.SerializeObject(evt);
            Log.Message($"[RimTelligence] Firing: {testJson}");

            IncidentWorker_Test worker = new IncidentWorker_Test();
            worker.FireLlmEventAsync(map, testJson);

            Messages.Message("Effect fired. Check Log.", MessageTypeDefOf.PositiveEvent);
            Close();
        }
    }
}