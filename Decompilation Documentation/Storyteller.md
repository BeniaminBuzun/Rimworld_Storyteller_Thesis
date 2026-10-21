# RimWorld Storyteller Logic Overview

## 1. Core Attributes & Constants

### Timing & Pacing
*   **`CheckInterval = 1000`**: The Storyteller "thinks" every 1000 ticks (exactly **1 in-game hour**). 
*   **`IntervalsPerDay = 60`**: Since a day is 60,000 ticks, there are 60 logic cycles per day.
*   **`incidentQueue`**: A buffer that holds events scheduled to happen in the future.
*   **`lastIncidentTick`**: Tracks the absolute time of the last generated incident.

### Difficulty & Adaptation
*   **`difficulty`**: The current active settings.
*   **`Notify_PawnEvent()`**: Feeds the **Adaptation System**. If a pawn is downed/killed, this lowers the "Adaptation Score," making the game temporarily easier.

### Anomaly DLC Logic
*   **`recentIncidentsAnomaly`**: A `Queue<bool>` tracking the last **4** major incidents to see if they were Anomaly-related.
*   **`recentAnomalyIncidentFactor`**: The percentage of recent events that were Anomaly-themed.
*   **`anomalyThreatsActive/InactiveFraction`**: Defines how much of the "Threat Pool" is dedicated to Anomaly entities based on whether you have reached certain DLC milestones.

---

## 2.`StorytellerTick()`

```csharp
public void StorytellerTick()
{
    incidentQueue.IncidentQueueTick(); // Process events already in the waiting line

    // Only run main logic every 1000 ticks (1 hour)
    if (Find.TickManager.TicksGame % 1000 != 0 || !DebugSettings.enableStoryteller)
    {
        return;
    }

    // Generate and attempt to fire new incidents
    foreach (FiringIncident item in MakeIncidentsForInterval())
    {
        TryFire(item);
    }
}
```

---

## 3. Incident Generation Workflow

### `MakeIncidentsForInterval`
This function collects potential events from two sources:
1.  **Storyteller Components**: Randomly rolled events (Raids, Traders, Eclipses).
2.  **Quests**: Scripted events (e.g., a quest says "A raid will arrive in 2 hours"). It tags these with `sourceQuestPart` so the game knows they aren't "random."

### `MakeIncidentsForInterval(comp, targets)`
This checks if a specific target (Map, Caravan, or World) is eligible for an event.

**The Filter Sequence:**
1.  **Days Passed**: Checks `comp.props.minDaysPassed`. (No raids on day 1).
2.  **Target Tags**: Checks if the target is valid (e.g., don't send an "Orbital Trader" to a "Caravan").
3.  **Big Threat Safety**:
    *   Blocks `ThreatBig` if the difficulty setting forbids them.
    *   **Anomaly Protection**: If the player just finished the "Metal Hell" (Anomaly Ending), all Big Threats are blocked for **300,000 ticks (5 days)** to allow recovery.

---

## 4. Key Event Notifications

The Storyteller listens to the world via `Notify_` methods to adjust its behavior:

| Method | Purpose |
| :--- | :--- |
| `Notify_PawnEvent` | Updates **Adaptation**. Loss of colonists = easier raids. |
| `Notify_DissolutionEvent` | **Biotech DLC**: Tracks Toxic Wastepack dissolution.|
| `Notify_QuestGiverTraderSpawned` | Sets a timestamp to prevent special traders from appearing too frequently. |
| `Notify_DefChanged` | Rebuilds the Storyteller logic. Called when switching from Cassandra to Randy or loading a save. |

---

## 5. Anomaly DLC Balancing Logic

The game maintains two static lists: `anomalyIncidents` and `nonAnomalyIncidents`.

When an incident fires, `RecordIncidentFired(incident)` runs:
1.  It pushes the event type (True = Anomaly, False = Vanilla) into a 4-slot Queue.
2.  It updates `recentAnomalyIncidentFactor`.
3.  If you've had too many Anomaly events, the Storyteller will artificially boost the weight of Vanilla events for the next hour's roll.

---

## 6. Persistence (`ExposeData`)

The Storyteller is saved into the `.rws` file. It saves:
*   The current `difficulty`.
*   The `incidentQueue` (so events aren't lost on reload).
*   The `recentIncidentsAnomaly` history.
*   Timestamps for traders and the last incident.

---

### Technical Notes:
*   **`item2`** in the decompiled code is just a generic iterator name; in our source, we should use descriptive names like `pendingIncident`.
*   `MakeIncidentsForInterval` execute every game hour. Avoid adding heavy calculation logic inside `StorytellerComp.MakeIntervalIncidents`.

### Almost raw decompiled code with comments 

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

public class Storyteller : IExposable
{
	public StorytellerDef def; // links to xml

	public DifficultyDef difficultyDef; // links to xml

	public Difficulty difficulty = new Difficulty(); // difficulty implementation

	public List<StorytellerComp> storytellerComps; // storyteller components

	private static List<IncidentDef> anomalyIncidents; // dlc Anomaly related

	private static List<IncidentDef> nonAnomalyIncidents; // dlc Anomaly related

	public IncidentQueue incidentQueue = new IncidentQueue(); // queue stores incidents

	private Queue<bool> recentIncidentsAnomaly = new Queue<bool>(); // dlc anomaly related

	private float recentAnomalyIncidentFactor = 0.5f; // dlc anomaly related

	private int lastQuestGiverTraderTick; // when last trader was trigerred

	private int lastIncidentTick; // when last incident was triggerd

	private float anomalyThreatsInactiveFraction = 0.08f; // dlc Anomaly related

	private float anomalyThreatsActiveFraction = 0.3f; // dlc Anomaly related

	private float? overrideAnomalyThreatsFraction; // dlc Anomaly related

	private float studyEfficiencyFactor = 1f; // factor of research multiplyer

	private AnomalyPlaystyleDef anomalyPlaystyleDef; // dlc Anomaly related

	public static readonly Vector2 PortraitSizeTiny = new Vector2(122f, 130f); 

	public static readonly Vector2 PortraitSizeLarge = new Vector2(580f, 620f);

	public const int IntervalsPerDay = 60; 

	public const int CheckInterval = 1000; // one in game hour 

	private const int RecentAnomalyIncidentsToRecord = 4; // dlc Anomaly related

	private const float QuestGiverTraderChance = 0.15f; // chance to get trader

	private const int QuestGiverTraderMinIntervalTicks = 600000; 

	private const int QuestGiverTraderMaxIntervalTicks = 3600000;

	private static readonly List<IIncidentTarget> tmpAllIncidentTargets = new List<IIncidentTarget>();

	private string debugStringCached = "Generating data...";

	[Obsolete("Use \"difficulty\" instead.")]
	public Difficulty difficultyValues => difficulty;

	public int LastIncidentTick => lastIncidentTick;

	public List<IIncidentTarget> AllIncidentTargets
	{
		get
		{
			tmpAllIncidentTargets.Clear();
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				tmpAllIncidentTargets.Add(maps[i]);
			}
			List<Caravan> caravans = Find.WorldObjects.Caravans;
			for (int j = 0; j < caravans.Count; j++)
			{
				if (caravans[j].IsPlayerControlled)
				{
					tmpAllIncidentTargets.Add(caravans[j]);
				}
			}
			tmpAllIncidentTargets.Add(Find.World);
			return tmpAllIncidentTargets;
		}
	}

	public static List<IncidentDef> AnomalyIncidents => anomalyIncidents ?? (anomalyIncidents = DefDatabase<IncidentDef>.AllDefsListForReading.Where((IncidentDef incident) => incident.IsAnomalyIncident).ToList());

	public static List<IncidentDef> NonAnomalyIncidents => nonAnomalyIncidents ?? (nonAnomalyIncidents = DefDatabase<IncidentDef>.AllDefsListForReading.Where((IncidentDef incident) => !incident.IsAnomalyIncident).ToList());

	public float AnomalyIncidentChanceNow => Mathf.Clamp01(Find.Anomaly.AnomalyThreatFractionNow);

	public float QuestGiverTraderChanceNow
	{
		get
		{
			if (Find.TickManager.TicksGame - lastQuestGiverTraderTick < 600000)
			{
				return 0f;
			}
			if (Find.TickManager.TicksGame - lastQuestGiverTraderTick > 3600000)
			{
				return 1f;
			}
			return 0.15f;
		}
	}

	public static void StorytellerStaticUpdate()
	{
		tmpAllIncidentTargets.Clear();
	}

	public Storyteller()
	{
	}

	public Storyteller(StorytellerDef def, DifficultyDef difficultyDef)
		: this(def, difficultyDef, new Difficulty(difficultyDef))
	{
	}

	public Storyteller(StorytellerDef def, DifficultyDef difficultyDef, Difficulty difficulty)
	{
		this.def = def;
		this.difficultyDef = difficultyDef;
		this.difficulty = difficulty;
		InitializeStorytellerComps();
	}

	private void InitializeStorytellerComps()
	{
		storytellerComps = new List<StorytellerComp>();
		for (int i = 0; i < def.comps.Count; i++)
		{
			if (def.comps[i].Enabled)
			{
				StorytellerComp storytellerComp = (StorytellerComp)Activator.CreateInstance(def.comps[i].compClass);
				storytellerComp.props = def.comps[i];
				storytellerComp.Initialize();
				storytellerComps.Add(storytellerComp);
			}
		}
	}

	public void ExposeData()
	{
		Scribe_Defs.Look(ref def, "def");
		Scribe_Defs.Look(ref difficultyDef, "difficulty");
		Scribe_Deep.Look(ref incidentQueue, "incidentQueue");
		Scribe_Collections.Look(ref recentIncidentsAnomaly, "recentIncidentsAnomaly", LookMode.Value);
		Scribe_Values.Look(ref recentAnomalyIncidentFactor, "recentAnomalyIncidentFactor", 0.5f);
		Scribe_Values.Look(ref lastQuestGiverTraderTick, "lastQuestGiverTraderTick", 0);
		Scribe_Values.Look(ref lastIncidentTick, "lastIncidentTick", 0);
		if (difficultyDef == null)
		{
			Log.Error("Loaded storyteller without difficulty");
			difficultyDef = DifficultyDefOf.Rough;
		}
		if (difficultyDef.isCustom)
		{
			Scribe_Deep.Look(ref difficulty, "customDifficulty", difficultyDef);
		}
		else if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			difficulty = new Difficulty(difficultyDef);
		}
		if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
		{
			InitializeStorytellerComps();
		}
		if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.Saving)
		{
			SetupNonCustomDifficultyAnomalySettings();
		}
	}

	private void SetupNonCustomDifficultyAnomalySettings()
	{
		if (ModsConfig.AnomalyActive && !difficultyDef.isCustom)
		{
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				Scribe_Values.Look(ref difficulty.anomalyThreatsInactiveFraction, "anomalyThreatsInactiveFraction", 0.08f);
				Scribe_Values.Look(ref difficulty.anomalyThreatsActiveFraction, "anomalyThreatsActiveFraction", 0.3f);
				Scribe_Values.Look(ref difficulty.overrideAnomalyThreatsFraction, "overrideAnomalyThreatsFraction");
				Scribe_Values.Look(ref difficulty.studyEfficiencyFactor, "studyEfficiencyFactor", 1f);
				Scribe_Defs.Look(ref anomalyPlaystyleDef, "anomalyPlaystyleDef");
				difficulty.AnomalyPlaystyleDef = anomalyPlaystyleDef;
			}
			else if (Scribe.mode == LoadSaveMode.Saving)
			{
				anomalyThreatsInactiveFraction = difficulty.anomalyThreatsInactiveFraction;
				anomalyThreatsActiveFraction = difficulty.anomalyThreatsActiveFraction;
				overrideAnomalyThreatsFraction = difficulty.overrideAnomalyThreatsFraction;
				studyEfficiencyFactor = difficulty.studyEfficiencyFactor;
				anomalyPlaystyleDef = difficulty.AnomalyPlaystyleDef;
				Scribe_Values.Look(ref anomalyThreatsInactiveFraction, "anomalyThreatsInactiveFraction", 0.08f);
				Scribe_Values.Look(ref anomalyThreatsActiveFraction, "anomalyThreatsActiveFraction", 0.3f);
				Scribe_Values.Look(ref overrideAnomalyThreatsFraction, "overrideAnomalyThreatsFraction");
				Scribe_Values.Look(ref studyEfficiencyFactor, "studyEfficiencyFactor", 1f);
				Scribe_Defs.Look(ref anomalyPlaystyleDef, "anomalyPlaystyleDef");
			}
		}
	}

	public void StorytellerTick()
	{
		incidentQueue.IncidentQueueTick();
		if (Find.TickManager.TicksGame % 1000 != 0 || !DebugSettings.enableStoryteller)
		{
			return;
		}
		foreach (FiringIncident item in MakeIncidentsForInterval())
		{
			TryFire(item);
		}
	}

	public bool TryFire(FiringIncident fi, bool queued = false)
	{
		if (fi.def.Worker.CanFireNow(fi.parms) && fi.def.Worker.TryExecute(fi.parms))
		{
			fi.parms.target.StoryState.Notify_IncidentFired(fi);
			lastIncidentTick = GenTicks.TicksGame;
			return true;
		}
		return false;
	}

	public IEnumerable<FiringIncident> MakeIncidentsForInterval()
	{
		List<IIncidentTarget> targets = AllIncidentTargets;
		for (int i = 0; i < storytellerComps.Count; i++)
		{
			foreach (FiringIncident item in MakeIncidentsForInterval(storytellerComps[i], targets))
			{
				yield return item;
			}
		}
		List<Quest> quests = Find.QuestManager.QuestsListForReading;
		for (int i = 0; i < quests.Count; i++)
		{
			if (quests[i].State != QuestState.Ongoing)
			{
				continue;
			}
			List<QuestPart> parts = quests[i].PartsListForReading;
			for (int j = 0; j < parts.Count; j++)
			{
				if (!(parts[j] is IIncidentMakerQuestPart incidentMakerQuestPart) || ((QuestPartActivable)parts[j]).State != QuestPartState.Enabled)
				{
					continue;
				}
				foreach (FiringIncident item2 in incidentMakerQuestPart.MakeIntervalIncidents())
				{
					item2.sourceQuestPart = parts[j];
					item2.parms.quest = quests[i];
					yield return item2;
				}
			}
		}
	}

	public IEnumerable<FiringIncident> MakeIncidentsForInterval(StorytellerComp comp, List<IIncidentTarget> targets)
	{
		if (GenDate.DaysPassedSinceSettleFloat <= comp.props.minDaysPassed)
		{
			yield break;
		}
		for (int i = 0; i < targets.Count; i++)
		{
			IIncidentTarget incidentTarget = targets[i];
			bool flag = false;
			bool flag2 = comp.props.allowedTargetTags.NullOrEmpty();
			foreach (IncidentTargetTagDef item in incidentTarget.IncidentTargetTags())
			{
				if (!comp.props.disallowedTargetTags.NullOrEmpty() && comp.props.disallowedTargetTags.Contains(item))
				{
					flag = true;
					break;
				}
				if (!flag2 && comp.props.allowedTargetTags.Contains(item))
				{
					flag2 = true;
				}
			}
			if (flag || !flag2)
			{
				continue;
			}
			foreach (FiringIncident item2 in comp.MakeIntervalIncidents(incidentTarget))
			{
				if ((Find.Storyteller.difficulty.allowBigThreats || item2.def.category != IncidentCategoryDefOf.ThreatBig) && (!ModsConfig.AnomalyActive || Find.TickManager.TicksGame - Find.Anomaly.metalHellClosedTick >= 300000 || item2.def.category != IncidentCategoryDefOf.ThreatBig))
				{
					yield return item2;
				}
			}
		}
	}

	public void Notify_PawnEvent(Pawn pawn, AdaptationEvent ev, DamageInfo? dinfo = null)
	{
		Find.StoryWatcher.watcherAdaptation.Notify_PawnEvent(pawn, ev, dinfo);
		for (int i = 0; i < storytellerComps.Count; i++)
		{
			storytellerComps[i].Notify_PawnEvent(pawn, ev, dinfo);
		}
	}

	public void Notify_DissolutionEvent(Thing thing)
	{
		for (int i = 0; i < storytellerComps.Count; i++)
		{
			storytellerComps[i].Notify_DissolutionEvent(thing);
		}
	}

	public void Notify_QuestGiverTraderSpawned()
	{
		lastQuestGiverTraderTick = Find.TickManager.TicksGame;
	}

	public void Notify_DefChanged()
	{
		InitializeStorytellerComps();
	}

	public void RecordIncidentFired(IncidentDef incident)
	{
		if (ModsConfig.AnomalyActive && incident.category.canUseAnomalyChance)
		{
			if (recentIncidentsAnomaly == null)
			{
				recentIncidentsAnomaly = new Queue<bool>();
			}
			if (recentIncidentsAnomaly.Count >= 4)
			{
				recentIncidentsAnomaly.Dequeue();
			}
			recentIncidentsAnomaly.Enqueue(incident.IsAnomalyIncident);
			recentAnomalyIncidentFactor = recentIncidentsAnomaly.Average((bool b) => (!b) ? 0f : 1f);
		}
	}

	public static void ResetStaticData()
	{
		anomalyIncidents = null;
		nonAnomalyIncidents = null;
	}

	public string DebugString()
	{
		if (Time.frameCount % 60 == 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("GLOBAL STORYTELLER STATS");
			stringBuilder.AppendLine("------------------------");
			stringBuilder.AppendLine("Storyteller: ".PadRight(40) + def.label);
			stringBuilder.AppendLine("Adaptation days: ".PadRight(40) + Find.StoryWatcher.watcherAdaptation.AdaptDays.ToString("F1"));
			stringBuilder.AppendLine("Adapt points factor: ".PadRight(40) + Find.StoryWatcher.watcherAdaptation.TotalThreatPointsFactor.ToString("F2"));
			stringBuilder.AppendLine("Time points factor: ".PadRight(40) + Find.Storyteller.def.pointsFactorFromDaysPassed.Evaluate(GenDate.DaysPassedSinceSettle).ToString("F2"));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Ally incident fraction (neutral or ally): ".PadRight(40) + StorytellerUtility.AllyIncidentFraction(fullAlliesOnly: false).ToString("F2"));
			stringBuilder.AppendLine("Ally incident fraction (ally only): ".PadRight(40) + StorytellerUtility.AllyIncidentFraction(fullAlliesOnly: true).ToString("F2"));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(StorytellerUtilityPopulation.DebugReadout().TrimEndNewlines());
			IIncidentTarget incidentTarget = (Find.WorldSelector.SingleSelectedObject as IIncidentTarget) ?? Find.CurrentMap;
			if (incidentTarget != null)
			{
				Map map = incidentTarget as Map;
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("STATS FOR INCIDENT TARGET: " + incidentTarget);
				stringBuilder.AppendLine("------------------------");
				stringBuilder.AppendLine("Progress score: ".PadRight(40) + StorytellerUtility.GetProgressScore(incidentTarget).ToString("F2"));
				stringBuilder.AppendLine("Base points: ".PadRight(40) + StorytellerUtility.DefaultThreatPointsNow(incidentTarget).ToString("F0"));
				stringBuilder.AppendLine("Points factor random range: ".PadRight(40) + incidentTarget.IncidentPointsRandomFactorRange.ToString());
				stringBuilder.AppendLine("Wealth: ".PadRight(40) + incidentTarget.PlayerWealthForStoryteller.ToString("F0"));
				if (map != null)
				{
					if (Find.Storyteller.difficulty.fixedWealthMode)
					{
						stringBuilder.AppendLine($"- Wealth calculated using fixed model curve, time factor: {Find.Storyteller.difficulty.fixedWealthTimeFactor:F1}");
						stringBuilder.AppendLine("- Map age: ".PadRight(40) + map.AgeInDays.ToString("F1"));
					}
					stringBuilder.AppendLine("- Items: ".PadRight(40) + map.wealthWatcher.WealthItems.ToString("F0"));
					stringBuilder.AppendLine("- Buildings: ".PadRight(40) + map.wealthWatcher.WealthBuildings.ToString("F0"));
					stringBuilder.AppendLine("- Floors: ".PadRight(40) + map.wealthWatcher.WealthFloorsOnly.ToString("F0"));
					stringBuilder.AppendLine("- Pawns: ".PadRight(40) + map.wealthWatcher.WealthPawns.ToString("F0"));
				}
				stringBuilder.AppendLine("Pawn count human: ".PadRight(40) + incidentTarget.PlayerPawnsForStoryteller.Count((Pawn p) => p.def.race.Humanlike));
				stringBuilder.AppendLine("Pawn count animal: ".PadRight(40) + incidentTarget.PlayerPawnsForStoryteller.Count((Pawn p) => p.IsAnimal));
				if (map != null)
				{
					stringBuilder.AppendLine("StoryDanger: ".PadRight(40) + map.dangerWatcher.DangerRating);
					stringBuilder.AppendLine("FireDanger: ".PadRight(40) + map.fireWatcher.FireDanger.ToString("F2"));
					stringBuilder.AppendLine("LastThreatBigTick days ago: ".PadRight(40) + (Find.TickManager.TicksGame - map.storyState.LastThreatBigTick).ToStringTicksToDays());
				}
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("LIST OF ALL INCIDENT TARGETS");
			stringBuilder.AppendLine("------------------------");
			for (int num = 0; num < AllIncidentTargets.Count; num++)
			{
				stringBuilder.AppendLine(num + ". " + AllIncidentTargets[num]);
			}
			debugStringCached = stringBuilder.ToString();
		}
		return debugStringCached;
	}
}

```
