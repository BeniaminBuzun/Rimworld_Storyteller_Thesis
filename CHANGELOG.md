# Changelog

All notable changes to RimTelligence will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

New changes are added manually under `[Unreleased]` and moved into a dated
semver section when a version is released.

## [Unreleased]

## [0.17.0] - 2026-10-14

### Changed
- `LLmEvent` letter now has links to the sites where effects happened
- `Colony snapshot` now includes descriptions for animals, plants, colonist childhood and adulthood
- `SpawnAnimal` effect now spawns **all** animals at the same location 

### Added
- 

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
