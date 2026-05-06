using LudeonTK;
using Newtonsoft.Json;
using RimTelligence.Source.CustomEvents;
using RimTelligence.Source.CustomEvents.Handlers;
using RimTelligence.Source.CustomEvents.Workers;
using RimTelligence.Source.DataSnapshots;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

// this file is to big rewrite it in the future
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
                    capturedHandler.EffectType,
                    DebugMenuOptionMode.Action,
                    () => OpenParamDialog(capturedHandler, map)
                ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        private static void OpenParamDialog(IEffectHandler handler, Map map)
        {
            string colonistName = map.mapPawns.FreeColonists.FirstOrDefault()?.Name.ToStringShort ?? "";

            LlmEffect effect = new LlmEffect
            {
                type = handler.EffectType,
                defName = "Silver",
                count = 10,
                value = 10f,
                targetPawn = colonistName
            };

            Find.WindowStack.Add(new Dialog_EffectTest(effect, map));
        }
    }

    public class Dialog_EffectTest : Window
    {
        private readonly Map map;
        private readonly LlmEffect effect;

        private string defNameBuf;
        private string defDescriptionBuf;
        private string pawnBuf;
        private string countBuf;
        private string valueBuf;

        private Vector2 pawnScroll;
        private Vector2 defScroll;

        private const float ButtonHeight = 30f;
        private const float ScrollSectionHeight = 150f;

        public override Vector2 InitialSize => new Vector2(480f, 620f);

        public Dialog_EffectTest(LlmEffect effect, Map map)
        {
            this.effect = effect;
            this.map = map;

            defNameBuf = effect.defName;
            defDescriptionBuf = effect.defDescription;
            pawnBuf = effect.targetPawn;
            countBuf = effect.count.ToString();
            valueBuf = effect.value.ToString();

            doCloseButton = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard ui = new Listing_Standard();
            ui.Begin(inRect);

            ui.Label($"Handler: {effect.type}");
            ui.GapLine();

            var handler = EffectHandlerRegistry.GetHandler(effect.type);
            if (handler != null)
            {
                ui.Label($"Hint: {handler.PromptDescription}");
                ui.Gap();
            }

            defNameBuf = ui.TextEntryLabeled("defName:    ", defNameBuf);
            defDescriptionBuf = ui.TextEntryLabeled("defDescription: ", defDescriptionBuf);
            pawnBuf = ui.TextEntryLabeled("targetPawn: ", pawnBuf);
            ui.TextFieldNumericLabeled("count: ", ref effect.count, ref countBuf, 0f, 10000f);
            ui.TextFieldNumericLabeled("value: ", ref effect.value, ref valueBuf, 0f, 10000f);

            ui.GapLine();

            // ── pawn quick-fill ──────────────────────────────────────────────
            ui.Label("Quick-fill pawn:");
            DrawScrollableButtons(
                ui, inRect,
                map.mapPawns.FreeColonists.Select(p => p.Name.ToStringShort).ToList(),
                ref pawnScroll,
                val => pawnBuf = val
            );

            // ── def quick-fill ───────────────────────────────────────────────
            List<string> defList = GetDefList(effect.type);
            if (defList != null)
            {
                ui.GapLine();
                ui.Label($"Quick-fill ({effect.type}):");
                DrawScrollableButtons(
                    ui, inRect,
                    defList,
                    ref defScroll,
                    val => defNameBuf = val
                );
            }

            ui.Gap();

            if (ui.ButtonText("Fire Effect"))
                FireEffect();

            ui.End();
        }

        private void DrawScrollableButtons(
            Listing_Standard ui,
            Rect inRect,
            List<string> items,
            ref Vector2 scroll,
            System.Action<string> onSelect)
        {
            if (items == null || items.Count == 0)
            {
                ui.Label("  (none)");
                return;
            }

            float totalHeight = items.Count * ButtonHeight;
            float viewHeight = Mathf.Min(totalHeight, ScrollSectionHeight);
            float innerWidth = inRect.width - 20f;

            Rect outerRect = ui.GetRect(viewHeight);
            Rect innerRect = new Rect(0f, 0f, innerWidth, totalHeight);

            Widgets.BeginScrollView(outerRect, ref scroll, innerRect);

            float y = 0f;
            foreach (string item in items)
            {
                Rect btnRect = new Rect(0f, y, innerWidth * 0.6f, ButtonHeight - 2f);
                if (Widgets.ButtonText(btnRect, item))
                    onSelect(item);
                y += ButtonHeight;
            }

            Widgets.EndScrollView();
            ui.Gap(4f);
        }

        private static List<string> GetDefList(string effectType)
        {
            switch (effectType)
            {
                case "AnimalInsanity": return DataList.AnimalList;
                case "DiseasePawn": return DataList.DiseaseList;
                case "MiracleHeal": return DataList.DiseaseList;
                case "PawnFoundItem": return DataList.ResourceList;
                case "Damage": return DataList.DamageTypeList;
                case "ChangeWeather": return DataList.AllWeathersList;
                case "GameCondition": return DataList.GameConditionList;
                case "AddExpPoints": return DataList.AllSkillsList;
                case "LoseExpPoints": return DataList.AllSkillsList;
                default: return null;
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
                letterText = $"Debug fire of {effect.type}.",
                effects = new List<LlmEffect> { effect }
            };

            string testJson = JsonConvert.SerializeObject(evt);
            Log.Message($"[RimTelligence] Firing: {testJson}");

            IncidentWorker_Test worker = new IncidentWorker_Test();
            worker.FireLlmEventAsync(map, testJson);

            Close();
        }
    }
}