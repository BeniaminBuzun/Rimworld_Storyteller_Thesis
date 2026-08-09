# Changelog

All notable changes to RimTelligence will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

New changes are added manually under `[Unreleased]` and moved into a dated
semver section when a version is released.

## [Unreleased]

### Added

- Crude RAG implementation feeding prompt context

### Changed

- Integrated incident generation with the storyteller flow
- Refactored and cleaned up event selection code

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
