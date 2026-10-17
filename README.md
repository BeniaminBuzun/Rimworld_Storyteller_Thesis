***

# RimTelligence: AI Storyteller

RimTelligence (Development Name) is an AI-driven storyteller mod for RimWorld.
Version history is tracked in [CHANGELOG.md](CHANGELOG.md).

## Things that are currently broken, missing or additional features:
- Rag is not wired into any relevant logic

## Benchmarking

Run headless model evaluations and benchmarks using `BenchmarkRunner`:

```bash
# Multi-effect benchmark (testing several effects at once per prompt)
.\BenchmarkRunner\bin\Debug\net472\RimTelligence.Benchmark.exe --benchmark multi

# Multi-effect benchmark with custom effect combinations
.\BenchmarkRunner\bin\Debug\net472\RimTelligence.Benchmark.exe --benchmark multi --combos "DamagePawn+AddDisease, ResourcePodCrash+MeteoriteImpact"

# Multi-effect benchmark testing prompts of size N (e.g., pairs or trios)
.\BenchmarkRunner\bin\Debug\net472\RimTelligence.Benchmark.exe --benchmark multi --effects-per-prompt 2 --cases 5

# Multi-effect benchmark using dynamic storyteller heuristic draws
.\BenchmarkRunner\bin\Debug\net472\RimTelligence.Benchmark.exe --benchmark multi --heuristic 5

# Single-effect benchmark (testing each effect in isolation)
.\BenchmarkRunner\bin\Debug\net472\RimTelligence.Benchmark.exe --benchmark single
```

Reports, execution metrics (latency, token usage, retention rate), and generated story samples are written to `BenchmarkOutput/multi-effect/` and `BenchmarkOutput/single-effect/`.


## Installation / Setup
1.  Clone the repository into your `RimWorld/Mods` folder.
2.  Ensure you have installed Krafs.Rimworld.Ref, Lib.Harmony, LlmTornado and its dependencies are in the `/Assemblies` folder.
3.  Build the project and enable **RimTelligence** in the RimWorld Mod Menu.


## Development
1. If you encounter weird errors on startup go to `C:\Users\User\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config` and delete `Mod_Rimworld_Storyteller_Thesis_AIStorytellerMod` 
2. Delete files in Assemblies folder and rebuild the project.