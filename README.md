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


# V0.12
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

## Installation / Setup
1.  Clone the repository into your `RimWorld/Mods` folder.
2.  Ensure you have installed Krafs.Rimworld.Ref, Lib.Harmony, LlmTornado and its dependencies are in the `/Assemblies` folder.
3.  Build the project and enable **RimTelligence** in the RimWorld Mod Menu.