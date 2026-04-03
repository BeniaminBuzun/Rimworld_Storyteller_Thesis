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


---

## Installation / Setup
1.  Clone the repository into your `RimWorld/Mods` folder.
2.  Ensure you have installed Krafs.Rimworld.Ref, Lib.Harmony, LlmTornado and its dependencies are in the `/Assemblies` folder.
3.  Build the project and enable **RimTelligence** in the RimWorld Mod Menu.