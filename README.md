***

# RimTelligence: AI Storyteller

I will use Readme as a file to documment progress RimTelligence (Development Name) V0.1

# Verion0.1 - proof of concept

### 1. Added selection for basic story generation on storyteller selection
*   Still buggy but it works
*   Preset System: Includes 5 built-in personality presets in mod settings way for user to type in his own preset

### 2. LLM Integration (Powered by LlmTornado)
*   Basic very basic and not universal mostly for testing and vailidating idea (BasicCom.cs class)
*   LlmTornado should be enought for our use it simplifies a lot 

### 3. Mod Settings
*   menu for mod settings 
*   paste there inside game menu your Grok api key (Only free one I've found for now)
*   textbox for user to input his custom storyteller description

### 3. Custom event Test
*   Asks llm for event text and displays it

# Version0.11

### 1. Fixed story prompt select ui element

### 2. Started on relevant documentation of decompiled source code


# Version0.12
### Procedural Event System
- `IncidentWorker_ProceduralEvent`: full async LLM event pipeline — snapshot → narrative → prompt → parse → execute
- `ColonySnapshotCapturer`: captures colony state (pawns, mood, health, traits, skills, hediffs, weather, wildlife, wealth, food, raid history, active conditions)
- `SnapshotNarrator`: converts raw snapshot into prose story seed for the prompt
- `PromptGenerator`: builds structured system prompt with randomised tone 6 styles (probably will replace after proper storyteller is implemented), effect type rules, validation constraints, and concrete examples

### Effect Execution
- `LlmEffectExecutor`: runs effects returned by LLM, collects focus target for letter camera pan
- `EffectHandlerRegistry`: handler registry with 9 active effect types
- Implemented handlers: `SpawnPawns`, `DropAtRandomPlaceItems`, `DamagePawn`, `FireCondition`, `AnimalInsanity`, `SelfTame`, `Alphabeavers`, `Disease`, `PawnFoundItem`
- Disabled (scaffolded): `StartFire`, `SpawnJoinable`

### Validation & Safety
- `LlmEffectValidator`: strips malformed effects before execution — catches blank types, missing required defNames, all-zero filler, unknown handlers

---

# Version 0.13

### Expanded Effect Handler Library
- Added 11 new effect handlers bringing the total to 20 active types
- New world effects: `ChangeWeather`, `MeteoriteImpact`, `CropBlight`, `ResourcePodCrash`, `SpawnItemsAtRandomLocation`, `LoseItems`
- New pawn effects: `MiracleHealDisease`, `MiracleHealChronicCondition`, `MiracleHealMissingPart`, `AddExpPoints`, `LoseExpPoints`, `MoodBuff`, `MoodDebuff`
- Deleted handlers that was to broken for game balance or didn't make any meaningful story effect : `StartFire`, `SpawnJoinable etc`
### Prompt System Improvements
- `PromptGenerator` now categorises effects into **World** vs **Pawn** buckets with steering logic — every 4th event is forced to be a world/environment event
- Added concrete JSON examples directly into the system prompt to reduce LLM formatting errors

### Dispatcher & Threading
- `RimTelligenceDispatcher`: new `GameComponent` that processes a thread-safe queue of LLM-triggered actions on the game thread, replacing `LongEventHandler` calls and preventing threading issues

### Debug Tooling
- `Dialog_EffectTest`: in-game debug dialog for manually firing any effect handler with custom parameters, quick-fill buttons for pawns and valid def names
- `RimTelligenceDebugActions`: dev menu integration under *RimTelligence* category

### Dynamic Mood Thoughts
- `ThoughtMemoryDynamic`: custom `Thought_Memory` subclass allowing LLM-generated mood buffs and debuffs with arbitrary labels and descriptions, fully save-game compatible

### Colony Snapshot Enchancements
- `ColonySnapshotCapturer` now includes detailed pawn health info (hediffs, missing parts), skill levels, and recent mood history for richer narrative context
- now properly captures previus events but only their names llm still have problems with repeating similar events but I tested with with short span between and in real game they shouldnt occur so often

# Version 0.14

- work in progress


## Installation / Setup
1.  Clone the repository into your `RimWorld/Mods` folder.
2.  Ensure you have installed Krafs.Rimworld.Ref, Lib.Harmony, LlmTornado and its dependencies are in the `/Assemblies` folder.
3.  Build the project and enable **RimTelligence** in the RimWorld Mod Menu.