# Changelog

All notable changes to RimTelligence will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

New changes are added manually under `[Unreleased]` and moved into a dated
semver section when a version is released.

## [Unreleased]

## [0.19.0] - 2026-10-28

### Added
- Include Sqlite and ONNX runtime DLLs for linux and macOS compatibility 

## [0.18.0] - 2026-10-23

### Changed
- `Mod settings` - api keys are now hidden behind additional show button
- Unified effect and category naming across configs and code (`All_Animals`, `Diseases`, `Weapons_*`, `Food_*`, …)
- Hardened LLM parsing and validation: repairs malformed JSON and validates item effects against the drawn category and current snapshot
- Item effects spawn safely (valid `stuff`, stack limits) and support an `Extreme` severity tier
- Benchmark reports reworked into one consistent table layout with per-run results, retention, and summaries
- `LoseItems` now removes items from any storage, not only stockpile zones

### Added
- Proper graphics for storyteller and mod main mod thumbnail
- - `ChoiceLetter_Procedural.Resolve()` — letter type now follows effect alignment (positive / negative / neutral)

### Fixed

- `HeuristicEventSelector` mutated shared `EffectConfig` singletons between events
- `PawnFoundItems` no longer leaks an item when no cell is free; `ResourcePodCrash` no longer creates more pods than item stacks

### Removed

- Obsolete `HarmonyPatches.cs` storyteller-selection transpiler
- `static_chunks` table (migrated into `items` / `others`)


## [0.17.0] - 2026-10-17

### Added
- `ChoiceLetter_Procedural` — custom letter popup that hyperlinks every executed effect to the map tile or object where it happened, so players can click directly to the incident location
- Automatic RAG database population on game startup when the database is empty (`RagBootstrap`)
- `GameObjectMeta` — shared metadata wrapper used by `ColonySnapshot` for body parts, injuries, diseases, implants, chronic conditions, and miscellaneous object categories
- `CountedMeta` — typed wrapper for counted collections (animals, plants, resources) that serialises cleanly to JSON for LLM consumption
- `PawnSnapshotCapturer` — dedicated capturer that extracts per-pawn health, skills, mood, and background data from the active map
- `GameObjectsCapturer` — extracts world-level objects (animals, plants, items) for snapshot enrichment
- Automated multi-effect benchmark (`MultiEffectBenchmark`) that fires a configurable batch of effects per prompt and aggregates pass/fail results in a `BenchmarkReport`

### Changed
- All **19 effect handlers** now produce rich, narrative `GetSummary()` text:
  - Preview / default text describes what the effect *will* do in compelling prose
  - Post-execution text captures actual outcomes (pawn names, quantities, locations)
  - Human-readable def labels via `LabelCap.Resolve()` instead of raw defNames
  - All handlers safely handle `null`, empty `SelectedDefName`, and `"random"` targets
  - **Animal**: `AnimalInsanity`, `SelfTame` (fixed "Nerby" typo → "Nearby"), `SpawnAnimals`
  - **Environment**: `ChangeWeather`, `CropBlight`, `MeteoriteImpact`, `StartExtremeWeather`
  - **Item**: `LoseItems`, `PawnFoundItems`, `ResourcePodCrash`
  - **Pawn Buffs**: `AddExpPoints`, `MiracleHealChronicCondition`, `MiracleHealDisease`, `MiracleHealMissingPart`, `MoodBuff`
  - **Pawn Debuffs**: `AddDisease`, `DamagePawn`, `LoseExpPoints`, `MoodDebuff`
- `ColonySnapshot` now uses `GameObjectMeta` for **missing_body_parts**, **injuries**, **diseases**, **implants**, **chronic_conditions**, and **other** health fields
- `ColonySnapshot` animal, plant, and resource collections now use `CountedMeta` for structured JSON output
- `ColonySnapshot` added **childhood_description** and **adulthood_description** fields for richer storytelling context
- `ColonySnapshotCapturer` delegates health/pawn data extraction to `PawnSnapshotCapturer`
- `IncidentWorker_ProceduralEvent` further decomposed into smaller, focused methods
- `LlmEffectExecutor` captures `handler.GetSummary(effect)` after `Execute()` into `EffectOutcome.Summary`, then passes outcomes to `BuildSummary()` for the event letter
- `HarmonyPatches` — consolidated patch registration
- `StorytellerComp_AI` — minor tick and event flow cleanup
- `LLMCommunicationHandler` — small request/response handling improvements
- `LlmResponseParser` / `LlmMultipleEventResponse` — robustness fixes for malformed LLM JSON
- `SnapshotNarrator` — improved tone and phrasing of generated colony descriptions
- `AddDisease` effect suppresses `EffectText` when it is not set, avoiding misleading "not set" label in event letters
- `CropBlight` effect now has improved, evocative effect text describing the scale of crop loss
- `RimTelligenceDebugActions` — new dev-menu entries for testing the snapshot pipeline

### Fixed
- `StorytellerState` — `Queue<FiringIncident>` is no longer saved with `Scribe_Values` (unsupported type); serialisation refactored to avoid a `System.ArgumentException` crash on game load
- `ThoughtMemoryDynamic` — dynamically generated `ThoughtDef` instances are now registered in `DefDatabase` before save so they survive a save/load cycle without "Could not load reference" errors
- `MoodBuff` / `MoodDebuff` thoughts are now fully persistent between save and load

## [0.16.0] - 2026-10-14

### Added
- `Rimtelligence.Benchmark` project for benchmarking LLM models for custom events
- `WarmUpAsync` method for testing connection and warming up LLM models

### Changed
- `IncidentWorker_ProceduralEvent` broken into several smaller methods

## [0.15.3] - 2026-10-07

### Fixed
- `release.yaml` was breaking due to onnxruntime version bump
- simplified `Rimtelligence.csproj` project config
- applied linting

## [0.15.2] - 2026-10-07

### Added
- `scroll bar` to the settings window

### Fixed
- Mod settings being not persistent between game sessions

## [0.15.1] - 2026-09-14

### Added
- `release.yaml` workflow for automated releases

## [0.15.0] - 2026-08-31

### Added

- Persistent memory database backend: SQLite RAG (retrieval-augmented generation) store with per-save dynamic chunks and static content chunks (`RagBootstrap`, `RagMigration`, `RagStoreBase`, `DynamicRagStore`, `StaticRagStore`, `SaveScopeComponent`)
- Embedding providers with cosine-similarity retrieval: built-in ONNX BERT model and tokenizer (`OnnxEmbeddingProvider`, `BertTokenizer`, bundled `model.onnx`/`vocab.txt`) or any configured LLM embedding provider (`LlmEmbeddingProvider`)
- `NativeDependencyLoader` and `ModPaths` for shipping native SQLite and onnxruntime DLLs alongside the mod
- Legacy `BasicCom.cs` replaced with `LLMCommunicationHandler`, `ProviderCatalog`, `ProviderConfig`, `LlmConnectionResult`, with structured-JSON, plain-JSON, and plain-text fallback request modes
- Data-driven effect system bootstrapped from `Data/effect_config.json`: `EffectConfigRegistry`, `EffectContractProvider`, `EffectFactory`, `IEffect` (handlers renamed)

### Changed

- Mod settings reworked to per-provider configs with an active chat provider;
- Effect handlers reorganized under `Effects/<category>` and reworked around `IEffect`/`EffectFactory`; `LlmEffectExecutor`, `LlmEffectValidator`, and `LlmResponseParser` updated
- Event selector modifiers simplified and reimplemented directly under `EventSelector/Modifiers`
- Item-category registry (`ItemCategoryRegistry`, `Data/item-categories.json`)
- Dynamically generated item effects squashed into `LoseItems`, `PawnFoundItems`, `ResourcePodCrash`

### Removed

- Old handler registry and per-type handler classes (`EffectHandlerRegistry`, `IEffectHandler`, individual `*Handler` classes), replaced by the config-driven effect system
- old prompt-generator files, `EventHistory`/`EventEligibility`, and redundant selector utility classes

## [0.14.0] - 2026-05-16

### Added

- Heuristic event selector replacing the Step 1 LLM call (`DrawModeSelector`, `EventHistory`, `EventEligibility`)
- Wealth-scaled item quantities with LOW/MEDIUM/HIGH severity tiers (`WealthBasedQuantityResolver`)
- Central def list lookup for item handlers (`EventItemMapping`) and shared base class (`GenericItemHandler`)

### Changed

- Prompt system rewritten to weight-based selection: heuristics pick effects, LLM fills in the details
- Structural cleanup: miracle handlers re-namespaced, `DebugEventWindow` extracted, `LlmEffect.cs` renamed to `GameEffect.cs`, `ColonySnapshot` moved, snapshot capturer/exporter split

### Removed

- `AlphabeaversHandler`, `SpawnItemsAtRandomLocationHandler`

## [0.13.0] - 2026-05-07

### Added

- 11 new effect handlers, bringing the total to 20 active types (`ChangeWeather`, `MeteoriteImpact`, `CropBlight`, `ResourcePodCrash`, `SpawnItemsAtRandomLocation`, `LoseItems`, `MiracleHealDisease`, `MiracleHealChronicCondition`, `MiracleHealMissingPart`, `AddExpPoints`, `LoseExpPoints`, `MoodBuff`, `MoodDebuff`)
- World vs pawn effect buckets with steering so every 4th event is a world/environment event
- Concrete JSON examples added to the system prompt to reduce LLM formatting errors
- `RimTelligenceDispatcher` GameComponent processing a thread-safe action queue on the game thread
- Debug tooling: `Dialog_EffectTest` dialog and dev-menu actions under the RimTelligence category
- `ThoughtMemoryDynamic` save-compatible dynamic mood thoughts

### Changed

- Colony snapshot now includes detailed health, skill levels, and mood history for richer narrative context

### Removed

- `StartFire`, `SpawnJoinable`

## [0.12.0] - 2026-05-06

### Added

- Procedural event system: `IncidentWorker_ProceduralEvent`, `ColonySnapshotCapturer`, `SnapshotNarrator`, `PromptGenerator` (6 randomized narrator tones)
- Effect execution: `LlmEffectExecutor`, `EffectHandlerRegistry` with 9 active handlers (`SpawnPawns`, `DropAtRandomPlaceItems`, `DamagePawn`, `FireCondition`, `AnimalInsanity`, `SelfTame`, `Alphabeavers`, `Disease`, `PawnFoundItem`)
- Validation: `LlmEffectValidator` strips malformed effects before execution

## [0.11.0] - 2026-04-03

### Added

- Documentation of decompiled RimWorld source code

### Fixed

- Story prompt select UI element

## [0.1.0] - 2026-03-31

### Added

- Proof-of-concept story generation on storyteller selection
- 5 built-in personality presets plus custom preset entry in mod settings
- Basic LLM integration via LlmTornado (`BasicCom.cs`)
- Mod settings menu (API key, custom storyteller description)
- Custom event test displaying LLM-generated event text
