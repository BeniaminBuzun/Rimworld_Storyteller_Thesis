using LudeonTK;
using Newtonsoft.Json;
using RimTelligence.CustomEvents.Handlers;
using RimTelligence.CustomEvents.Prompting;
using RimTelligence.DataSnapshots;
using RimTelligence.Memory;
using RimTelligence.Source.CustomEvents.Handlers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimTelligence.CustomEvents.Workers
{
    public static class RimTelligenceDebugActions
    {
        [DebugAction("RimTelligence", "Test Effect Handler", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestEffectHandler()
        {
            Map map = Find.CurrentMap;
            List<DebugMenuOption> options = new List<DebugMenuOption>();

            foreach (string handlerName in EffectFactory.GetAllEffectNames())
            {
                string capturedName = handlerName;
                options.Add(new DebugMenuOption(
                    capturedName,
                    DebugMenuOptionMode.Action,
                    () => OpenParamDialog(capturedName, map)
                ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("RimTelligence", "Test RAG Query", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestRagQuery()
        {
            Find.WindowStack.Add(new Dialog_RagQuery());
        }

        [DebugAction("RimTelligence", "Print Active Save ID", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PrintActiveSaveId()
        {
            var component = SaveScopeComponent.Current;
            if (component == null || string.IsNullOrEmpty(component.SaveId))
            {
                ModLogger.Message("No active save loaded — SaveId unavailable.");
                return;
            }
            ModLogger.Message("Active Save ID: " + component.SaveId);
        }

        [DebugAction("RimTelligence", "Test Response Parser", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestResponseParser()
        {
            Find.WindowStack.Add(new Dialog_ParserBattery());
        }

        private static void OpenParamDialog(string effectName, Map map)
        {
            string colonistName = map.mapPawns.FreeColonists.FirstOrDefault()?.Name.ToStringShort ?? "";

            GameEffect effect = new GameEffect
            {
                EffectName = effectName,
                SelectedDefName = new string[0],
                Severity = SeverityTypes.None,
                TargetPawn = colonistName
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

            ui.Gap(6f);
            ui.Label("Active Save ID: " + (SaveScopeComponent.Current?.SaveId ?? "(none)"));

            ui.Gap(10f);
            GUI.color = Color.green;
            if (ui.ButtonText("Run Query"))
            {
                RunQuery();
            }
            GUI.color = Color.white;

            GUI.color = Color.cyan;
            if (ui.ButtonText("Run Dynamic Query (save-scoped)"))
            {
                RunDynamicQuery();
            }
            GUI.color = Color.white;

            ui.GapLine();
            ui.Label($"Results ({lastResults.Count}):");

            Rect outRect = ui.GetRect(250f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, lastResults.Count * 60f);

            Widgets.BeginScrollView(outRect, ref resultsScroll, viewRect);
            float yPosition = 0f;
            foreach (var result in lastResults)
            {
                Rect rowRect = new Rect(0f, yPosition, viewRect.width, 56f);
                Widgets.DrawMenuSection(rowRect);

                Rect scoreLabel = new Rect(rowRect.x + 4f, rowRect.y + 2f, rowRect.width - 8f, 18f);
                Widgets.Label(scoreLabel, $"[{result.Score:F3}] ({result.Source})");

                Rect textLabel = new Rect(rowRect.x + 4f, rowRect.y + 20f, rowRect.width - 8f, 34f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(textLabel, result.Text);
                Text.Font = GameFont.Small;

                yPosition += 60f;
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
            foreach (var result in lastResults)
                Log.Message($"  [{result.Score:F3}] ({result.Source}) {result.Text}");
        }

        private void RunDynamicQuery()
        {
            if (string.IsNullOrWhiteSpace(queryBuf))
            {
                Log.Message("[RimTelligence] Empty query, skipped.");
                return;
            }

            var component = SaveScopeComponent.Current;
            if (component == null || string.IsNullOrEmpty(component.SaveId))
            {
                lastResults.Clear();
                resultsScroll = Vector2.zero;
                Log.Message("[RimTelligence] No active save loaded — dynamic query returned no results.");
                return;
            }

            float[] embedding = GameObjectsCapturer.EmbeddingProvider.Embed(queryBuf);
            var store = new DynamicRagStore(RagBootstrap.DbPath, component.SaveId);
            lastResults = store.Query(embedding, topK: topK);
            resultsScroll = Vector2.zero;

            Log.Message($"[RimTelligence] Dynamic query (save {component.SaveId}): \"{queryBuf}\" — {lastResults.Count} results:");
            foreach (var result in lastResults)
                Log.Message($"  [{result.Score:F3}] ({result.Source}) {result.Text}");
        }
    }

    public class Dialog_EffectTest : Window
    {
        private readonly Map map;
        private readonly GameEffect effect;

        private string defNameBuf;
        private string pawnBuf;

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

            defNameBuf = effect.SelectedDefName != null && effect.SelectedDefName.Length > 0 ? effect.SelectedDefName[0] : "";
            pawnBuf = effect.TargetPawn ?? "";

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

            ui.Label($"<color=orange>Handler: {effect.EffectName}</color>");
            ui.GapLine();

            var handler = EffectFactory.Create(effect.EffectName);
            if (handler != null)
            {
                Text.Font = GameFont.Tiny;
                string description;
                try
                {
                    description = handler.Config.NarrativeDescription;
                }
                catch
                {
                    description = string.Empty;
                }
                ui.Label($"Description: {description}");
                Text.Font = GameFont.Small;
                ui.Gap();
            }

            //  Fields 
            defNameBuf = ui.TextEntryLabeled("defName:        ", defNameBuf);
            pawnBuf = ui.TextEntryLabeled("targetPawn:     ", pawnBuf);

            //  ValueType selector 
            ui.Gap(4f);
            ui.Label($"Value Strategy: <color=cyan>{effect.Severity}</color>");

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
                map.mapPawns.FreeColonists.Select(pawn => pawn.Name.ToStringShort).ToList(),
                ref pawnScroll,
                val => pawnBuf = val
            );

            //  Def quick-fill 
            List<string> defList = GetDefList(effect.EffectName);
            if (defList != null && defList.Count > 0)
            {
                ui.GapLine();
                ui.Label($"Quick-fill defName ({effect.EffectName}):");
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

        private void DrawValueButton(Rect row, int index, float btnW, SeverityTypes severity, string label)
        {
            Rect buttonRect = new Rect(row.x + index * btnW, row.y, btnW - 2f, row.height);

            bool active = effect.Severity == severity;
            if (active)
                Widgets.DrawBoxSolid(buttonRect, new Color(0.3f, 0.6f, 0.3f, 0.4f));

            if (Widgets.ButtonText(buttonRect, label))
                effect.Severity = severity;
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
            float yPosition = 0f;
            foreach (string item in items)
            {
                Rect btnRect = new Rect(5f, yPosition, innerRect.width - 5f, ButtonHeight - 2f);
                if (Widgets.ButtonText(btnRect, item))
                    onSelect(item);
                yPosition += ButtonHeight;
            }
            Widgets.EndScrollView();
            ui.Gap(4f);
        }

        private static List<string> GetDefList(string effectType)
        {
            if (string.IsNullOrEmpty(effectType))
                return null;

            try
            {
                List<GameObjectMeta> defs = EffectContractProvider.GetRelatedDefList(effectType);
                return defs?.Count > 0
                    ? defs.Select(def => def.DefName).ToList()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private void FireEffect()
        {
            effect.SelectedDefName = string.IsNullOrEmpty(defNameBuf) ? new string[0] : new string[] { defNameBuf };
            effect.TargetPawn = pawnBuf;

            LlmCustomEvent evt = new LlmCustomEvent
            {
                letterTitle = $"Test: {effect.EffectName}",
                letterText = $"Debug manual trigger of {effect.EffectName}.",
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

    public class Dialog_ParserBattery : Window
    {
        private static readonly string CleanEventJson =
            "{\"letterTitle\":\"Title A\",\"letterText\":\"Text A\"," +
            "\"effects\":[{\"type\":\"MeteoriteImpact\",\"targetPawn\":\"\",\"defName\":\"\",\"count\":1,\"value\":\"LOW\"}]}";

        private readonly List<BatteryResult> results = new List<BatteryResult>();
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(760f, 520f);

        public Dialog_ParserBattery()
        {
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            RunBattery();
        }

        private void RunBattery()
        {
            results.Clear();

            // US1: fences, prose wrapping, BOM/line endings
            AddRecoverable("fence json", "Here is the JSON:\n```json\n" + CleanEventJson + "\n```");
            AddRecoverable("fence JSON uppercase", "```JSON\n" + CleanEventJson + "\n```");
            AddRecoverable("fence js", "```js\n" + CleanEventJson + "\n```");
            AddRecoverable("fence no tag", "```\n" + CleanEventJson + "\n```");
            AddRecoverable("fence trailing space", "```json \n" + CleanEventJson + "\n```");
            AddRecoverable("prose before", "Sure! Here is your JSON: " + CleanEventJson);
            AddRecoverable("prose after", CleanEventJson + " Hope that helps!");
            AddRecoverable("prose both sides", "Sure! " + CleanEventJson + " Hope that helps!");
            AddRecoverable("BOM + CRLF + fence", "\uFEFF\r\n```json\r\n" + CleanEventJson + "\r\n```\r\n");
            AddRecoverable("two objects, first wins", CleanEventJson + " and also {\"letterTitle\":\"Second\"}",
                data => data.letterTitle == "Title A");
            AddUnrecoverable("braces and quotes in letterText (empty effects)",
                "{\"letterTitle\":\"T\",\"letterText\":\"A {weird} [thing] and \\\"quotes\\\"\",\"effects\":[]}");

            // US2: malformed-but-recoverable JSON
            AddRecoverable("array top level", "[" + CleanEventJson + "]",
                data => data.letterTitle == "Title A");
            AddRecoverable("array prose wrapped", "Here: [" + CleanEventJson + "] done",
                data => data.letterTitle == "Title A");
            AddRecoverable("lowercase severity high",
                "{\"letterTitle\":\"S\",\"letterText\":\"X\",\"effects\":[{\"type\":\"MeteoriteImpact\",\"targetPawn\":\"\",\"defName\":\"\",\"count\":1,\"value\":\"high\"}]}",
                data => data.effects != null && data.effects.Count == 1 && data.effects[0].Severity == SeverityTypes.High);
            AddRecoverable("mixed case severity Medium",
                "{\"letterTitle\":\"S\",\"letterText\":\"X\",\"effects\":[{\"type\":\"MeteoriteImpact\",\"targetPawn\":\"\",\"defName\":\"\",\"count\":1,\"value\":\"Medium\"}]}",
                data => data.effects != null && data.effects.Count == 1 && data.effects[0].Severity == SeverityTypes.Medium);
            AddUnrecoverable("trailing comma in object (empty effects)",
                "{\"letterTitle\":\"TC\",\"letterText\":\"X\",\"effects\":[],}");
            AddRecoverable("trailing comma in array",
                "{\"letterTitle\":\"TC\",\"letterText\":\"X\",\"effects\":[{\"type\":\"MeteoriteImpact\",\"targetPawn\":\"\",\"defName\":\"\",\"count\":1,\"value\":\"LOW\"},]}");

            // US3: unrecoverable input and missing-field defaults
            AddUnrecoverable("garbage then JSON (empty effects)", "garbage {not json} {\"letterTitle\":\"G2J\",\"letterText\":\"X\",\"effects\":[]}");
            AddUnrecoverable("missing fields (empty effects)",
                "{\"letterTitle\":\"Quiet\"}");
            AddUnrecoverable("missing all fields (empty effects)", "{}");
            AddRecoverable("unknown severity degrades to None",
                "{\"letterTitle\":\"U\",\"letterText\":\"X\",\"effects\":[{\"type\":\"MeteoriteImpact\",\"targetPawn\":\"\",\"defName\":\"\",\"count\":1,\"value\":\"bogus\"}]}",
                data => data.effects != null && data.effects.Count == 1 && data.effects[0].Severity == SeverityTypes.None);

            AddUnrecoverable("pure garbage", "this is not json at all");
            AddUnrecoverable("truncated JSON", "{\"letterTitle\":\"Tru\",\"letterText\":\"");
            AddUnrecoverable("empty array", "[]");
            AddUnrecoverable("whitespace only", "   \r\n \t ");
        }

        private void AddRecoverable(string name, string sample, Func<LlmCustomEvent, bool> assert = null)
        {
            LlmParseResult<LlmCustomEvent> result = LlmResponseParser.ParseEvent(sample);
            bool passed = result.isSuccess && result.data != null && (assert == null || assert(result.data));
            string detail = passed ? string.Empty : (result.isSuccess ? "assertion failed" : result.errorMessage);
            results.Add(new BatteryResult(name, passed, detail));
        }

        private void AddUnrecoverable(string name, string sample)
        {
            LlmParseResult<LlmCustomEvent> result = LlmResponseParser.ParseEvent(sample);
            bool passed = !result.isSuccess && result.data == null && !string.IsNullOrEmpty(result.errorMessage);
            string detail = passed ? "clean failure" : (result.isSuccess ? "unexpected success" : "missing reason");
            results.Add(new BatteryResult(name, passed, detail));
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "LLM Response Parser Battery");
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(inRect.x, inRect.y + 36f, inRect.width, inRect.height - 60f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, results.Count * 26f);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float rowY = 0f;
            foreach (BatteryResult result in results)
            {
                Rect rowRect = new Rect(0f, rowY, viewRect.width, 24f);
                GUI.color = result.passed ? Color.green : Color.red;
                Widgets.Label(rowRect, (result.passed ? "[PASS] " : "[FAIL] ") + result.name +
                    (result.detail.Length > 0 ? " — " + result.detail : string.Empty));
                GUI.color = Color.white;
                rowY += 26f;
            }

            Widgets.EndScrollView();
        }

        private class BatteryResult
        {
            public string name;
            public bool passed;
            public string detail;

            public BatteryResult(string name, bool passed, string detail)
            {
                this.name = name;
                this.passed = passed;
                this.detail = detail;
            }
        }
    }
}