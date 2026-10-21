<!--
  Sync Impact Report
  Version change: 1.3.0 → 1.4.0
  Modified principles:
    - VI. Zero-Compile-Error Policy: build & verify procedure made concrete and
      toolchain-correct (working MSBuild command, RuntimeIdentifier requirement,
      gate pass criterion, native-copy caveat)
  Added sections: None
  Removed sections: None
  Modified sections:
    - Development Workflow / Build Gate: working MSBuild command + pass criterion
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ aligned (no changes required — generic)
    - .specify/templates/spec-template.md ✅ aligned (no changes required)
    - .specify/templates/tasks-template.md ✅ aligned (no changes required)
  Follow-up TODOs:
    - specs/001, 003, 004, 006 (tasks.md, quickstart.md, plan.md) still reference
      `dotnet build RimTelligence.csproj -p:TargetFrameworkVersion=v4.7.2`, which
      does NOT resolve package references for this old-style csproj. Update those
      spec artifacts (via /speckit.tasks, /speckit.plan, or manually) to the
      working MSBuild command in Principle VI.
-->

<!--
  Sync Impact Report
  Version change: 1.2.1 → 1.3.0
  Modified principles: None
  Added sections:
    - Core Principles: VII. Simplicity First (new principle)
  Modified sections:
    - Naming Conventions / General Rules: one-letter variable names prohibited
    - Governance: simplicity & naming compliance added as review gates
  Removed sections: None
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ updated (Complexity Tracking ↔ Principle VII)
    - .specify/templates/spec-template.md ✅ aligned (no changes required)
    - .specify/templates/tasks-template.md ✅ updated (simplicity/naming review task)
  Follow-up TODOs: None
-->

<!--
  Sync Impact Report
  Version change: 1.2.0 → 1.2.1
  Modified principles: None
  Modified sections:
    - Development Workflow: Branch Naming → single-branch workflow
  Added sections: None
  Removed sections: None
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ aligned
    - .specify/templates/spec-template.md ✅ aligned
    - .specify/templates/tasks-template.md ✅ aligned
  Follow-up TODOs: None
-->

# RimTelligence Constitution

## Core Principles

### I. Thread Safety First

All game-state mutations MUST occur on the main game thread. Async operations
(LLM API calls, ONNX inference, SQLite queries) MUST bridge results back to
the game thread via a `GameComponent` that drains a thread-safe action queue
each tick. The `LongEventHandler.ExecuteWhenFinished()` pattern is deprecated
for new code.

**Rationale**: RimWorld is single-threaded for game logic. Violations cause
race conditions, save corruption, and hard-to-reproduce crashes.

### II. Handler Extensibility via Registry

Every game effect MUST implement a common handler interface and register
itself in a central static registry. Handlers expose: a unique effect name,
narrative description, prompt structure metadata, base weight, alignment
category, and an `Execute()` method. Item-related handlers share a common
abstract base. Mood handlers share a separate abstract base. New effect types
require only a handler class and a registry entry.

**Rationale**: The handler registry is the primary extension point. Keeping
it decoupled from the LLM pipeline allows adding effects without modifying
prompt generation or validation logic.

### III. Snapshot-Driven Context

All colony state provided to the LLM MUST originate from immutable snapshot
objects captured at event-fire time. A narrator component converts raw snapshot
data to prose for the prompt. Event selection uses the same snapshot for
heuristic weighting. No direct `Map` or `Pawn` queries in prompt builders
or event selectors.

**Rationale**: Snapshots ensure consistency between what the LLM sees and
what exists in-game. They enable save/load fidelity and prevent stale
references when the game state changes during async LLM processing.

### IV. Heuristic-First Event Selection

Event selection MUST use a local weighted pipeline: draw-mode selection →
state-based weight modifiers → eligibility filtering → weighted random draw.
LLM calls are restricted to narrative generation and parameter filling
(Step 2). The heuristic pipeline must respect: recency penalties, family
diversity caps, colony-state modifiers (mood, food, weather, raid pressure),
and eligibility pre-filters.

**Rationale**: Local heuristics are deterministic, instant, and don't consume
API tokens. The LLM adds narrative flavor but should not make selection
decisions that a weighted random system handles better.

### V. Structured LLM Prompts with Concrete Examples

Every LLM system prompt MUST include: randomized narrator tone from a fixed
set of voices, explicit writing rules, colonist rules, effect-specific rules,
and at least one concrete JSON example matching the expected response format.
A validation layer MUST strip any malformed or invalid effects before dispatch
to the handler registry.

**Rationale**: LLMs produce more reliable structured output when given
concrete examples and clear constraints. Validation prevents crashes from
unexpected handler types or missing required fields.

### VI. Zero-Compile-Error Policy

Every build MUST complete with zero compilation errors before any commit,
merge, or PR submission. Warnings SHOULD be investigated and resolved where
feasible. CI/CD or local build scripts MUST enforce a zero-error gate.

The build gate for this repository is the full Visual Studio MSBuild with a
RuntimeIdentifier — `dotnet build` does NOT resolve package references for
this old-style csproj (restore produces an empty `project.assets.json`, so no
Verse/Harmony/OnnxRuntime references resolve):

```text
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" RimTelligence.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /p:RuntimeIdentifiers=win /v:m /nologo
```

- Restore MUST include the RuntimeIdentifier for the native packages:
  `dotnet restore RimTelligence.csproj -p:RuntimeIdentifiers=win`
  (required by System.Data.SQLite.Core and Microsoft.ML.OnnxRuntime).
- The gate PASSES when the build logs **zero `error CS` compile errors** and
  `Assemblies\RimTelligence.dll` has a fresh LastWriteTime. A non-zero MSBuild
  exit caused only by the `FlattenNativeSqlite` copy into `Native\`
  (MSB3021/MSB3027 — files locked by a running RimWorld) is NOT a gate
  failure: compilation already succeeded.
- New `.cs` files MUST be added to the explicit `<Compile Include>` list in
  `RimTelligence.csproj` or the gate fails.

**Rationale**: A consistently compilable codebase prevents regression cascades,
minimizes bisection overhead, and ensures every contributor can work from a
known-good baseline. A concrete, toolchain-correct command makes the gate
reproducible instead of a black box.

### VII. Simplicity First

Code MUST be simple and straightforward. When multiple valid solutions exist,
the simplest correct one MUST be chosen. One-letter variable names MUST NOT be
used; every variable MUST have a descriptive camelCase name that conveys its
purpose. Clever, cryptic, or needlessly abstract code is a review blocker
unless the added complexity is justified in the plan's Complexity Tracking
table.

**Rationale**: Simple code is easier to read, review, debug, and maintain.
Descriptive names and direct logic reduce cognitive load, which matters in an
extension-heavy codebase where handlers and registries are frequently added.

## Naming Conventions

### C# Code

| Element | Convention | Example |
|---|---|---|
| Namespaces | `RimTelligence.Source.<Module>[.<Submodule>]` | `RimTelligence.Source.AIStoryteller` |
| Classes / Structs | PascalCase | `ColonySnapshot`, `HeuristicEventSelector` |
| Interfaces | `I` prefix + PascalCase | `IEffectHandler`, `IEventModifier` |
| Enums | PascalCase | `SeverityTypes`, `EventAlignment` |
| Enum values | PascalCase | `Weighted`, `PureRandom`, `BadStreak` |
| Methods | PascalCase | `Capture()`, `BuildSystemPrompt()` |
| Public fields | camelCase | `colonyName`, `wealthTotal` |
| Private fields | `_camelCase` | `_recentTypes`, `_countWeights` |
| Constants | PascalCase | `MaxResourceEntries`, `SpotlightCount` |
| Static readonly | `_camelCase` or PascalCase | `_random`, `OutputPath` |
| Parameters | camelCase | `incidentParms`, `map` |
| Local variables | camelCase | `selectedEvents`, `systemPrompt` |
| Type parameters | `T` or PascalCase | `THandler` |
| File names | Match class name | `HeuristicEventSelector.cs` |

### XML Defs

| Element | Convention | Example |
|---|---|---|
| Storyteller DefNames | PascalCase with prefix | `AI_AIStoryteller` |
| Incident DefNames | PascalCase | `LlmEvent` |
| Thought DefNames | PascalCase with prefix | `RimTelligence_DynamicMoodBuff` |
| Effect type strings | `PascalCase_SubType` | `ResourcePodCrash_FoodMeals` |
| Namespace attributes | Fully qualified class name | `RimTelligence.Source.CustomEvents.Workers.IncidentWorker_ProceduralEvent` |

### General Rules

- No Hungarian notation for types (e.g., no `strName`, `intCount`)
- Boolean fields/parameters use `is`, `has`, `can`, or `should` prefix
- Event handler effect types MUST match the `PascalCase_SubType` format
- All new public API surface MUST have XML doc comments
- One-letter variable names MUST NOT be used; names MUST describe their purpose
  (see Principle VII)

## Mod Architecture Constraints

- **Target Framework**: .NET Framework 4.7.2 (RimWorld 1.6 requirement)
- **Root Namespace**: `RimTelligence` with sub-namespaces under `Source/`
- **Dependencies**: Only via NuGet packages listed in `.csproj`. Runtime DLLs
  go in `Assemblies/`, native DLLs in `Native/`
- **Harmony Patches**: Use `com.rimtelligence.aistoryteller` as the Harmony
  ID. Prefer transpilers for UI injection; prefix/postfix for logic patches
- **Save Compatibility**: All `GameComponent` subclasses MUST implement
  `ExposeData()`. Custom `Thought_Memory` subclasses MUST use defName-based
  registration for save persistence
- **No Secrets in Code**: API keys MUST only be stored in mod settings via
  the in-game settings UI, never hardcoded

## Development Workflow

- **Branch**: Single `development` branch for all work. No feature branches.
- **Commit Convention**: `docs:` for constitution/spec changes, `feat:` for
  new handlers, `fix:` for bug fixes, `refactor:` for structural changes
- **Handler Implementation Order**: Define handler interface implementation
  → register in handler registry → implement `Execute()` → add XML Def if
  needed → test via debug dialog
- **Prompt Changes**: Test prompt generator output logged via the centralized
  logger. Validate JSON structure before and after effect validation
- **Debug Tooling**: Use debug menu actions and debug windows for runtime
  inspection. Export snapshots via data exporter for offline analysis
- **Decompilation Notes**: Reference `Decompilation Documentation/` for
  RimWorld API behavior. Update notes when new APIs are discovered
- **Build Gate**: Run the working MSBuild command and verify zero `error CS`
  compile errors before any commit or PR (see Principle VI). A `Native\` copy
  failure while RimWorld is running does not fail the gate.

## Governance

This constitution supersedes all other development practices for RimTelligence.
Amendments require: (1) documented rationale, (2) version bump per semantic
versioning, (3) update to all dependent templates and specs. All pull requests
and code reviews MUST verify compliance with core principles. Simplicity and
naming compliance are mandatory review gates — see Principle VII. Complexity
deviations MUST be justified in the plan's Complexity Tracking table.
Build compliance (zero-compile-error) is a mandatory PR gate — see Principle VI.

**Version**: 1.4.0 | **Ratified**: 2026-07-27 | **Last Amended**: 2026-08-09
