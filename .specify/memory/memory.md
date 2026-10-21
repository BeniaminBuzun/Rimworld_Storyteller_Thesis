# Session Memory

## Objective
Debug RimWorld mod `RimTelligence` failing with `"Empty response from provider."` on Groq model `grok-openai/gpt-oss-20b`; add diagnostic logging; current blocker is Groq free-tier **rate limiting** (8000 TPM for gpt-oss-20b).

## Key Facts
- Mod path: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Rimworld_Storyteller_Thesis`
- `LlmTornado` 3.8.64 (`RimTelligence.csproj:147`).
- **Build procedure (see `constitution.md` Principle VI):** old-style csproj (net4.7.2) — plain `dotnet build` CANNOT resolve packages. Use:
  1. `dotnet restore RimTelligence.csproj -p:RuntimeIdentifiers=win`
  2. `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" RimTelligence.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /p:RuntimeIdentifiers=win /v:m /nologo`
- Running RimWorld locks `Native\SQLite.Interop.dll` → MSB3021/MSB3027 copy errors expected, NOT a gate failure.
- Build gate pass: 0 `error CS` (verified last build 19:52:53).
- Groq rate-limit error format: `Rate limit reached for model ... TPM: Limit 8000 ... Please try again in Ns ... code "rate_limit_exceeded"` (HTTP 429 TooManyRequests).
- ModLogger wrapper: `Source\ModLogger.cs` (use `ModLogger.Error/Message/Warning`).

## Root Cause Analysis (empty response)
- `BasicCom.cs`: empty-response check is after `AppendUserInput` + `GetResponse()`; no pipeline pruning exists between Groq and the check.
- LlmTornado parses Groq via default `JsonConvert.DeserializeObject<ChatResult>`; `choices[].message.content` → `ChatMessage.Content` (`string?`); `HandleResponseRich` only adds Message block when `Content?.Length > 0`; `ChatRichResponse.Text` collects only Message blocks → empty content ⇒ null ⇒ `"Empty response from provider."`.
- gpt-oss-20b is a reasoning model; Groq puts CoT in `message.reasoning`, final answer in `content`. Empty content = reasoning-only/refusal/filter/max-tokens case. Intermittent, not persistent.
- `RawResponse` IS populated for Groq (`ChatEndpoint.NormalizeChatResult`).

## Changes Made
1. `Source\Communication\BasicCom.cs`:
   - `GetResponse()` → `GetResponseRich()`.
   - Empty response ⇒ error gets `DescribeEmptyResponse()` (Choices, Finish, ContentLen, Reasoning, Refusal, Usage) + `LogEmptyResponseDiagnostics()` → ModLogger.Error: Blocks + RawJson (`RedactKeys`, truncated 4000 chars).
   - **NEW:** `DescribeError` now recognizes `429` OR `rate_limit` OR `toomanyrequests` (case-insensitive) → returns `"Rate limit exceeded (HTTP 429). " + RedactKeys(message)` (keeps JSON incl. retry seconds for parsing).
2. `Source\CustomEvents\Workers\IncidentWorker_ProceduralEvent.cs` (Step 2 loop, MaxAttempts=3):
   - **NEW:** Backoff between attempts: `RetryBackoffSeconds(errorMessage, attempt)` — regex `try again in\s+([\d.]+)s`; base 5s, cap 30s. Log line truncates reason to 150 chars.
   - Added usings: `System.Globalization`, `System.Text.RegularExpressions`.
3. `Source\CustomEvents\Handlers\Environment\StartExtremeWeatherEvent.cs:46`: fixed log bug (was logging ticks as days); now `Fired condition: {def.defName} for {effect.count} day(s) ({duration} ticks).`
4. Builds verified 0 error CS at 19:37:59, 19:50:10, 19:52:53.

## ROOT CAUSE (confirmed from logs, 2026-08-09)
- **Empty response mechanism:** `Finish=Length`, `Reasoning tokens: 2046/2048`, `ContentLen=0`. gpt-oss-20b on Groq spends its ENTIRE completion budget (~2048 tokens, Groq default `max_completion_tokens`) on chain-of-thought and gets cut off before emitting JSON. Prompt is too demanding (6 sentences + causal chain + voice rules + principle) → over-reasons.
- Rate limits: free tier TPM=8000; prompts up to 20k chars (20221-char prompt = ~5055 tokens, "Requested 6788"). Reasoning tokens also count against TPM.
- Concurrency: multiple ProceduralEvents run Step 2 in parallel (log lines interleave) → combined TPM spikes.

## Changes Made (2026-08-09, build DLL 20:05:29 verified 0 error CS)
1. `IncidentWorker_ProceduralEvent.cs`: `RetryBackoffSeconds` now returns fixed **60** (always 1 min). Removed unused usings (`System.Globalization`, `System.Text.RegularExpressions`). Step 2 success log now appends `P={promptTokens} C={completionTokens} R={reasoningTokens}`.
2. `LlmConnectionResult.cs`: added `promptTokens`, `completionTokens`, `reasoningTokens` (int); `Success(...)` factory extended with optional params.
3. `BasicCom.cs`: new `LogResponseSummary()` logs on EVERY successful LLM response: chars + `Finish=` + `tokens P= C= R=`. Populates the new usage fields from `richResponse.Usage` / `Usage.CompletionTokensDetails.ReasoningTokens`.

## Findings / Facts
- LlmTornado 3.8.64: `ChatUsage` has `PromptTokens`, `CompletionTokens`, `TotalTokens` (Int32); reasoning = `Usage.CompletionTokensDetails.ReasoningTokens` (nullable Int32). `ChatRequest.MaxTokens` (Nullable<int>) settable via `chat.RequestParameters.MaxTokens` — NOT currently set anywhere; Groq defaults to 2048.
- Catalog `grok-openai/gpt-oss-20b`: Context=131072, ReasoningTokensMin/Max unset.
- `ChatMessageFinishReasons` includes `Length` — the observed empty-response reason.

## Remaining / Next Steps
- USER MUST RESTART RimWorld to load 20:05:29 DLL to verify fixed 60s backoff + token logging.
- **Proposed fix for empty responses: set `chat.RequestParameters.MaxTokens` higher (e.g. 4096-8192) in `BasicCom.RunChatRequestAsync`** so reasoning can finish AND content is emitted. NOT implemented — ask user (interacts with TPM "Requested" accounting).
- **Proposed fix for rate limits: reduce Step 2 prompt size** (20221-char prompts) — design decision, ask user.
- Consider serializing Step 2 LLM calls to avoid parallel TPM spikes (bigger change, not started).

## Changes Made (2026-10-07, build DLL 13:52:22 verified 0 error CS) — ProviderConfig MissingMethodException
- **Root cause:** `Settings.ExposeData` called
  `Scribe_Collections.Look(ref providerConfigs, "providerConfigs", LookMode.Deep, "providerId")`.
  The 4th arg `"providerId"` binds to the `params object[] ctorArgs` overload →
  Scribe ran `Activator.CreateInstance(typeof(ProviderConfig), new object[]{"providerId"})`
  (looks for a `ProviderConfig(string)` ctor) → `MissingMethodException: Constructor on type
  'RimTelligence.Llm.ProviderConfig' not found.` Proven by stack trace
  `Look<ProviderConfig>(List&, bool, string, LookMode, object[])`. A parameterless ctor alone
  does NOT fix this — Scribe was never requesting it.
- `Source\Settings.cs:60` → `Scribe_Collections.Look(ref providerConfigs, "providerConfigs", LookMode.Deep);`
  (default inverse element name `"li"` matches the saved XML). **Do NOT re-add the extra string.**
- Hardened `Settings.cs`: `GetProviderConfig` Find lambda now null-guards
  (`config != null &&`); `ExposeData` removes null entries from `providerConfigs` on LoadingVars.
  (Failed `SaveableFromNode` puts NULLs in the list → previously crashed the Mod Settings window
  with NRE in `GetProviderConfig`.)
- `ProviderConfig.cs` keeps public parameterless ctor (still valid/needed for LookMode.Deep).
- **User must fully restart RimWorld** (no running process 13:xx) so the 13:52:22 DLL loads.

## Relevant Files
- `Source\Communication\BasicCom.cs`, `Source\Communication\LlmConnectionResult.cs`
- `Source\CustomEvents\Workers\IncidentWorker_ProceduralEvent.cs`
- `Source\CustomEvents\Handlers\Environment\StartExtremeWeatherEvent.cs`
- `Source\AIStoryteller\AiStorytellerMod.cs:262-285` (connection test UI)
- `.specify\memory\constitution.md` (Principle VI build gate)
- Player.log: `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
