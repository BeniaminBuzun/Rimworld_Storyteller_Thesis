***

# RimTelligence: AI Storyteller

RimTelligence (Development Name) is an AI-driven storyteller mod for RimWorld.
Version history is tracked in [CHANGELOG.md](CHANGELOG.md).

## Things that are currently broken, missing or additional features:
- LLm has problems with generating proper json structure for DamagePawn effects
- Mood buff/debuff breaks between save and load
- LlmTornado has outdated groq/OpenRouter provider models list
- Rag is not wired into any relevant logic
- CustomEvent prompt is catchy and needs to be fine tuned for more `natural` results
- Settings needs scrollbar
- Colony snapshot is not fully complete (wildlifeAnimals should should include metaData)
- If possible add into event popup window link that jumps into where effect occured
- 



## Installation / Setup
1.  Clone the repository into your `RimWorld/Mods` folder.
2.  Ensure you have installed Krafs.Rimworld.Ref, Lib.Harmony, LlmTornado and its dependencies are in the `/Assemblies` folder.
3.  Build the project and enable **RimTelligence** in the RimWorld Mod Menu.


## Development
1. If you encounter weird errors on startup go to `C:\Users\User\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config` and delete `Mod_Rimworld_Storyteller_Thesis_AIStorytellerMod` 
2. Delete files in Assemblies folder and rebuild the project.