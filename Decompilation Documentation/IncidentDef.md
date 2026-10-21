Class used for controlling incidents 

class strings:

## General environment incidents
- SolarFlare
- Eclipse
- PsychicDrone
- PsychicSoothe
- ToxicFallout
- VolcanicWinter
- HeatWave
- ColdSnap
- Flashstorm
- ClimateCycle
- Planetkiller
- Aurora
- PsychicDroner

## Caravean incidents
- Ambush
- ManhunterAmbush
- CaravanMeeting
- CaravanDemand

## Disease incidents
- Disease_Flu
- Disease_Plague
- Disease_Malaria
- Disease_SleepingSickness
- Disease_FibrousMechanites
- Disease_SensoryMechanites
- Disease_GutWorms
- Disease_MuscleParasites
- Disease_AnimalFlu
- Disease_AnimalPlague
- Disease_OrganDecay

## Animal incidents
- SelfTame
- FarmAnimalsWanderIn
- ThrumboPasses
- HerdMigration
- Alphabeavers


## Combat incidents
- RaidEnemy
- Infestation
- DeepDrillInfestation
- ManhunterPack
- DefoliatorShipPartCrash
- PsychicEmanatorShipPartCrash
- AnimalInsanityMass
- AnimalInsanitySingle

## Trader/friendly fraction incidents
- OrbitalTraderArrival
- TraderCaravanArrival
- VisitorGroup
- TravelerGroup
- RaidFriendly

## Plants incidents
- AmbrosiaSprout
- CropBlight

## Pawn incidents
- WandererJoin
- RefugeePodCrash
- RansomDemand
- WildManWandersIn
- StrangerInBlackJoin
- GameEndedWanderersJoin


## Random/missalainous incidents
- ResourcePodCrash
- MeteoriteImpact
- ShortCircuit
- ShipChunkDrop
- GiveQuest_Random
- GiveQuest_EndGame_ShipEscape
- VSE_ManInTheCoatJoin


```csharp
IncidentDef def = DefDatabase<IncidentDef>.GetNamed("HerdMigration");
IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, Find.CurrentMap);
parms.target = Find.CurrentMap;
def.Worker.TryExecute(parms);
```
