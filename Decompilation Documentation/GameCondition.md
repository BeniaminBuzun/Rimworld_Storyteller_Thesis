Controls state of the map temperature weather directly without displaying normal incident message 

available weather general events 

event strings:
    SolarFlare
    Eclipse
    PsychicDrone
    PsychicSoothe
    ToxicFallout
    VolcanicWinter
    HeatWave
    ColdSnap
    Flashstorm
    ClimateCycle
    Planetkiller
    Aurora
    PsychicDroner
    
can be fired using 

```csharp
GameConditionDef condDef = DefDatabase<GameConditionDef>.GetNamed("event string");
GameCondition condition = GameConditionMaker.MakeCondition(condDef, duration: 180000);
Find.CurrentMap.gameConditionManager.RegisterCondition(condition);
```