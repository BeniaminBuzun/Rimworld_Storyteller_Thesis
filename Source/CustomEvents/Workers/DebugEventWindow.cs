using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimTelligence.Source.CustomEvents.Workers
{
    using global::RimTelligence.Source.CustomEvents.EventSelector;
    using global::RimTelligence.Source.CustomEvents.EventSelector.Modifiers;
    using global::RimTelligence.Source.CustomEvents.Handlers;
    using global::RimTelligence.Source.DataSnapshots;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Verse;

    namespace RimTelligence.Source.Debug
    {
        public class DebugEventWindow : Window
        {
            public override Vector2 InitialSize => new Vector2(820f, 640f);

            private static readonly Color HeaderColor = new Color(0.85f, 0.85f, 0.45f);
            private static readonly Color GoodColor = new Color(0.45f, 0.85f, 0.55f);
            private static readonly Color BadColor = new Color(0.90f, 0.40f, 0.40f);
            private static readonly Color NeutralColor = new Color(0.70f, 0.70f, 0.70f);
            private static readonly Color DimColor = new Color(0.55f, 0.55f, 0.55f);
            private static readonly Color BarBg = new Color(0.15f, 0.15f, 0.15f);

            private enum Tab { History, Weights, DrawMode }
            private Tab _tab = Tab.History;

            private Vector2 _historyScroll;
            private Vector2 _weightsScroll;

            private List<(string type, float weight, EventAlignment align)> _cachedWeights;
            private float _cachedWeightMax;
            private ColonySnapshot _cachedSnapshot;

            public DebugEventWindow()
            {
                doCloseButton = true;
                doCloseX = true;
                absorbInputAroundWindow = false;
                draggable = true;
                resizeable = true;
            }

            public override void DoWindowContents(Rect inRect)
            {
                Text.Font = GameFont.Medium;
                GUI.color = HeaderColor;
                Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "RimTelligence — Event Debug");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                float y = inRect.y + 36f;

                float tabW = 160f;
                if (DrawTab(new Rect(inRect.x, y, tabW, 28f), "History", _tab == Tab.History)) _tab = Tab.History;
                if (DrawTab(new Rect(inRect.x + tabW + 4f, y, tabW, 28f), "Weights", _tab == Tab.Weights)) { _tab = Tab.Weights; RebuildWeights(); }
                if (DrawTab(new Rect(inRect.x + tabW * 2 + 8f, y, tabW, 28f), "Draw Mode", _tab == Tab.DrawMode)) _tab = Tab.DrawMode;
                y += 34f;

                Widgets.DrawLineHorizontal(inRect.x, y, inRect.width);
                y += 6f;

                Rect body = new Rect(inRect.x, y, inRect.width, inRect.yMax - y - 40f);

                switch (_tab)
                {
                    case Tab.History: DrawHistory(body); break;
                    case Tab.Weights: DrawWeights(body); break;
                    case Tab.DrawMode: DrawDrawMode(body); break;
                }
            }

            //  Tab: History 

            private void DrawHistory(Rect rect)
            {
                var history = EventHistory.Current;

                if (history == null)
                {
                    DrawCentered(rect, "EventHistory.Current is null — not yet initialized.");
                    return;
                }

                float stripH = 52f;
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, stripH), BarBg);
                float col = rect.x + 8f;
                DrawStat(ref col, rect.y + 6f, "Consecutive Bad", history.ConsecutiveBadBatches.ToString(), BadColor);
                DrawStat(ref col, rect.y + 6f, "Consecutive Good", history.ConsecutiveGoodBatches.ToString(), GoodColor);
                rect.yMin += stripH + 6f;

                var recentTypes = GetPrivateList<string>(history, "_recentTypes");
                var recentAlignments = GetPrivateList<EventAlignment>(history, "_recentAlignments");

                float half = (rect.width - 8f) / 2f;
                Rect leftPane = new Rect(rect.x, rect.y, half, rect.height);
                Rect rightPane = new Rect(rect.x + half + 8f, rect.y, half, rect.height);

                // Left: recent types (newest first)
                DrawPaneHeader(leftPane, $"Recent Event Types  ({recentTypes?.Count ?? 0} / 20)");
                Rect leftBody = new Rect(leftPane.x, leftPane.y + 22f, leftPane.width, leftPane.height - 22f);

                if (recentTypes == null || recentTypes.Count == 0)
                {
                    DrawCentered(leftBody, "— empty —");
                }
                else
                {
                    var view = new Rect(0, 0, leftBody.width - 16f, recentTypes.Count * 20f);
                    Widgets.BeginScrollView(leftBody, ref _historyScroll, view);

                    for (int i = recentTypes.Count - 1; i >= 0; i--)
                    {
                        int di = recentTypes.Count - 1 - i;
                        float recency = history.RecencyPenalty(recentTypes[i]);
                        Rect row = new Rect(0, di * 20f, view.width, 20f);

                        if (di % 2 == 0) Widgets.DrawBoxSolid(row, new Color(0f, 0f, 0f, 0.12f));

                        GUI.color = Color.Lerp(Color.white, DimColor, (float)di / recentTypes.Count);
                        Widgets.Label(new Rect(row.x, row.y, 28f, 20f), $"−{di}");
                        GUI.color = Color.white;
                        Widgets.Label(new Rect(row.x + 28f, row.y, row.width - 90f, 20f), recentTypes[i]);
                        DrawMiniBar(new Rect(row.xMax - 80f, row.y + 4f, 72f, 12f),
                                    recency, Color.Lerp(GoodColor, BadColor, recency));
                    }

                    Widgets.EndScrollView();
                }

                // Right: batch alignments (newest first)
                DrawPaneHeader(rightPane, $"Batch Alignments  ({recentAlignments?.Count ?? 0} / 10)");
                Rect rightBody = new Rect(rightPane.x, rightPane.y + 22f, rightPane.width, rightPane.height - 22f);

                if (recentAlignments == null || recentAlignments.Count == 0)
                {
                    DrawCentered(rightBody, "— empty —");
                }
                else
                {
                    var scroll = Vector2.zero;
                    var view = new Rect(0, 0, rightBody.width - 16f, recentAlignments.Count * 28f);
                    Widgets.BeginScrollView(rightBody, ref scroll, view);

                    for (int i = recentAlignments.Count - 1; i >= 0; i--)
                    {
                        int di = recentAlignments.Count - 1 - i;
                        Rect row = new Rect(0, di * 28f, view.width, 28f);

                        if (di % 2 == 0) Widgets.DrawBoxSolid(row, new Color(0f, 0f, 0f, 0.12f));

                        GUI.color = AlignColor(recentAlignments[i]);
                        string pill = recentAlignments[i] == EventAlignment.Bad ? "▼ BAD" :
                                      recentAlignments[i] == EventAlignment.Good ? "▲ GOOD" : "● NEUTRAL";
                        Widgets.Label(new Rect(row.x + 4f, row.y + 4f, row.width, 28f), $"−{di}  {pill}");
                        GUI.color = Color.white;
                    }

                    Widgets.EndScrollView();
                }
            }

            //  Tab: Weights 

            private void RebuildWeights()
            {
                _cachedSnapshot = ColonySnapshotCapturer.Capture(Find.CurrentMap);
                var history = EventHistory.Current;

                _cachedWeights = EffectHandlerRegistry.GetAllHandlers()
                    .Select(h =>
                    {
                        float w = h.BaseWeight;

                        if (_cachedSnapshot != null)
                            w *= StateModifiers.Apply(
                                new EventEntry(h.EffectName, h.BaseWeight, h.Alignment),
                                _cachedSnapshot);

                        if (history != null)
                        {
                            string family = h.EffectName.Contains('_') ? h.EffectName.Split('_')[0] : h.EffectName;
                            float penalty = Mathf.Max(
                                history.RecencyPenalty(h.EffectName),
                                history.FamilyRecencyPenalty(family) * 0.5f);
                            w *= (1f - penalty);
                        }

                        return (h.EffectName, w, h.Alignment);
                    })
                    .OrderByDescending(x => x.w)
                    .ToList();

                _cachedWeightMax = _cachedWeights.Count > 0 ? _cachedWeights.Max(x => x.weight) : 1f;
            }

            private void DrawWeights(Rect rect)
            {
                if (Widgets.ButtonText(new Rect(rect.x, rect.y, 130f, 28f), "↺ Refresh"))
                    RebuildWeights();

                Text.Font = GameFont.Tiny;
                GUI.color = DimColor;
                Widgets.Label(new Rect(rect.x + 140f, rect.y + 6f, rect.width - 140f, 28f),
                    "base × state modifiers × recency penalty — as seen by HeuristicEventSelector");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                rect.yMin += 34f;

                if (_cachedWeights == null)
                {
                    DrawCentered(rect, "Press Refresh to compute weights for the current map state.");
                    return;
                }

                float rowH = 24f;
                var view = new Rect(0, 0, rect.width - 16f, _cachedWeights.Count * rowH);
                Widgets.BeginScrollView(rect, ref _weightsScroll, view);

                for (int i = 0; i < _cachedWeights.Count; i++)
                {
                    var (type, weight, align) = _cachedWeights[i];
                    Rect row = new Rect(0, i * rowH, view.width, rowH);

                    if (i % 2 == 0) Widgets.DrawBoxSolid(row, new Color(0f, 0f, 0f, 0.15f));

                    GUI.color = DimColor;
                    Widgets.Label(new Rect(row.x, row.y, 30f, rowH), $"#{i + 1}");
                    GUI.color = AlignColor(align);
                    Widgets.Label(new Rect(row.x + 32f, row.y, 12f, rowH), "●");
                    GUI.color = Color.white;
                    Widgets.Label(new Rect(row.x + 46f, row.y, 260f, rowH), type);

                    float barX = row.x + 310f;
                    float barW = row.width - barX - 58f;
                    DrawMiniBar(new Rect(barX, row.y + 5f, barW, rowH - 10f),
                                _cachedWeightMax > 0f ? weight / _cachedWeightMax : 0f,
                                AlignColor(align));

                    GUI.color = DimColor;
                    Widgets.Label(new Rect(row.xMax - 56f, row.y, 54f, rowH), weight.ToString("F2"));
                    GUI.color = Color.white;
                }

                Widgets.EndScrollView();
            }

            //
            //
            //
            //
            //
            //
            //
            //  Tab: Draw Mode 

            private void DrawDrawMode(Rect rect)
            {
                var snapshot = _cachedSnapshot ?? ColonySnapshotCapturer.Capture(Find.CurrentMap);
                var history = EventHistory.Current;
                float y = rect.y;

                DrawSectionHeader(ref y, rect, "Override Checks");

                if (history == null)
                {
                    GUI.color = BadColor;
                    Widgets.Label(new Rect(rect.x, y, rect.width, 22f), "EventHistory.Current is null");
                    GUI.color = Color.white;
                    return;
                }

                DrawRow(ref y, rect, "Consecutive Bad Batches",
                        history.ConsecutiveBadBatches.ToString(),
                        history.ConsecutiveBadBatches >= 4 ? BadColor : Color.white);
                DrawRow(ref y, rect, "Consecutive Good Batches",
                        history.ConsecutiveGoodBatches.ToString(),
                        history.ConsecutiveGoodBatches >= 3 ? GoodColor : Color.white);

                y += 6f;
                Widgets.DrawLineHorizontal(rect.x, y, rect.width); y += 8f;

                if (history.ConsecutiveBadBatches >= 4)
                {
                    GUI.color = GoodColor;
                    Widgets.Label(new Rect(rect.x, y, rect.width, 22f),
                        "→ OVERRIDE: GoodStreak  (4+ consecutive bad batches)");
                    GUI.color = Color.white;
                    return;
                }
                if (history.ConsecutiveGoodBatches >= 3)
                {
                    GUI.color = BadColor;
                    Widgets.Label(new Rect(rect.x, y, rect.width, 22f),
                        "→ OVERRIDE: BadStreak  (3+ consecutive good batches)");
                    GUI.color = Color.white;
                    return;
                }

                GUI.color = NeutralColor;
                Widgets.Label(new Rect(rect.x, y, rect.width, 22f), "No override active — weighted lottery applies.");
                GUI.color = Color.white;
                y += 28f;

                DrawSectionHeader(ref y, rect, "Active Lottery Nudges");

                if (snapshot == null)
                {
                    GUI.color = DimColor;
                    Widgets.Label(new Rect(rect.x, y, rect.width, 22f), "(no snapshot — map may not be loaded)");
                    GUI.color = Color.white;
                    return;
                }

                float avgMood = snapshot.colonists?.Count > 0
                    ? snapshot.colonists.Average(p => p.needs?.mood ?? 0.5f)
                    : 0.5f;

                DrawRow(ref y, rect, "Avg Colony Mood", avgMood.ToString("P0"));
                DrawRow(ref y, rect, "Days Since Last Raid", snapshot.daysSinceLastRaid.ToString("F1"));
                DrawRow(ref y, rect, "Total Nutrition", snapshot.totalNutrition.ToString("F0"));
                y += 6f;

                bool any = false;
                if (avgMood > 0.80f) { DrawNudge(ref y, rect, "Mood > 80%   → +10% BadStreak", BadColor); any = true; }
                if (avgMood < 0.25f) { DrawNudge(ref y, rect, "Mood < 25%   → +8%  GoodStreak", GoodColor); any = true; }
                if (snapshot.daysSinceLastRaid > 20) { DrawNudge(ref y, rect, "No raid 20d  → +12% BadStreak", BadColor); any = true; }
                if (snapshot.daysSinceLastRaid <= 2) { DrawNudge(ref y, rect, "Recent raid  → −10% BadStreak", NeutralColor); any = true; }
                if (snapshot.totalNutrition < 10f) { DrawNudge(ref y, rect, "Starving     → −8%  PureRandom", NeutralColor); any = true; }

                if (!any)
                {
                    GUI.color = DimColor;
                    Widgets.Label(new Rect(rect.x + 12f, y, rect.width, 22f), "  (no nudges active)");
                    GUI.color = Color.white;
                }
            }

            //  Helpers 

            private bool DrawTab(Rect r, string label, bool active)
            {
                if (active) Widgets.DrawBoxSolid(r, new Color(0.25f, 0.25f, 0.25f));
                GUI.color = active ? HeaderColor : Color.white;
                bool clicked = Widgets.ButtonText(r, label, drawBackground: !active);
                GUI.color = Color.white;
                return clicked;
            }

            private void DrawPaneHeader(Rect pane, string title)
            {
                GUI.color = HeaderColor;
                Widgets.Label(new Rect(pane.x, pane.y, pane.width, 22f), title);
                GUI.color = Color.white;
            }

            private void DrawSectionHeader(ref float y, Rect rect, string title)
            {
                GUI.color = HeaderColor;
                Widgets.Label(new Rect(rect.x, y, rect.width, 22f), title);
                GUI.color = Color.white;
                y += 26f;
            }

            private void DrawRow(ref float y, Rect rect, string label, string value, Color? valueColor = null)
            {
                Widgets.Label(new Rect(rect.x + 4f, y, 260f, 20f), label);
                GUI.color = valueColor ?? Color.white;
                Widgets.Label(new Rect(rect.x + 268f, y, rect.width - 268f, 20f), value);
                GUI.color = Color.white;
                y += 22f;
            }

            private void DrawNudge(ref float y, Rect rect, string text, Color col)
            {
                GUI.color = col;
                Widgets.Label(new Rect(rect.x + 12f, y, rect.width - 12f, 20f), "  • " + text);
                GUI.color = Color.white;
                y += 20f;
            }

            private void DrawStat(ref float x, float y, string label, string value, Color valueColor)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = DimColor;
                Widgets.Label(new Rect(x, y, 130f, 16f), label);
                Text.Font = GameFont.Small;
                GUI.color = valueColor;
                Widgets.Label(new Rect(x, y + 16f, 130f, 22f), value);
                GUI.color = Color.white;
                x += 145f;
            }

            private void DrawMiniBar(Rect r, float fill01, Color col)
            {
                Widgets.DrawBoxSolid(r, BarBg);
                if (fill01 > 0f)
                    Widgets.DrawBoxSolid(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill01), r.height), col);
            }

            private void DrawCentered(Rect rect, string text)
            {
                GUI.color = DimColor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, text);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            private Color AlignColor(EventAlignment align) =>
                align == EventAlignment.Good ? GoodColor :
                align == EventAlignment.Bad ? BadColor : NeutralColor;

            private static List<T> GetPrivateList<T>(object obj, string fieldName)
            {
                var field = obj.GetType().GetField(
                    fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return field?.GetValue(obj) as List<T>;
            }
        }
    }
}
