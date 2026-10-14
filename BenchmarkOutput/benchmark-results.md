# Model Benchmark

| Provider | Model | Result | Load (ms) | Run (ms) | Prompt | Completion | Reasoning | Effects | Error |
|---|---|---|---:|---:|---:|---:|---:|---|---|
| Ollama_Local | tobestyledintro/minicpm5-2b:latest | FAIL | 5765 | 10501 | 1445 | 1045 | 0 |  | No JSON object or array found in response. |
| Ollama_Local | qwen3.5:0.8b | FAIL | 8517 | 325587 | 26545 | 35 | 0 |  | The response contained no effects after validation. An effects array with at least one valid effect is required. |
| Ollama_Local | granite4.2:3b | PASS | 5730 | 44508 | 1392 | 3525 | 0 | ResourcePodCrash |  |
| Ollama_Local | qwen3.5:4b | FAIL | 12939 | 134775 | 8860 | 159 | 0 |  | The response contained no effects after validation. An effects array with at least one valid effect is required. |
| Ollama_Local | ministral-3:3b | FAIL | 8144 | 3819 | 2056 | 101 | 0 |  | Response was cut off mid-JSON (truncated output). The model likely ran out of output tokens. |
| Ollama_Local | gemma4:e4b | PASS | 18872 | 19018 | 2026 | 135 | 0 | ResourcePodCrash |  |
| Ollama_Local | granite4.2:8b | PASS | 12069 | 9814 | 1392 | 223 | 0 | ResourcePodCrash |  |
| Ollama_Local | qwen3.5:9b | FAIL | 17594 | 264736 | 11157 | 116 | 0 |  | The response contained no effects after validation. An effects array with at least one valid effect is required. |
| Ollama_Local | ministral-3:14b | PASS | 25600 | 15388 | 2057 | 204 | 0 | ResourcePodCrash |  |
| Ollama_Local | phi4:14b | PASS | 27290 | 13578 | 1259 | 193 | 0 | ResourcePodCrash |  |

## granite4.2:3b
**Frosty Shift**

The resource pod shattered, scattering frost‑kissed metal near the workshop. Lumi grabbed a salvaged bolt and forged it for Misha’s frozen room of hope. Misha felt the warmth easing his misery and rested easier each breath. Together they harvested the new metal, turning disaster into a quiet spark toward brighter days.


## gemma4:e4b
**Unexpected Winter Gift**

As winter tightened its grip, the heavy crash of metal spilled a fragile cargo near the supply lines. Vladimir saw the mangled pod through the drifting snow while tending his pine haul, pausing mid-effort to survey the mess. Whatever loot scattered across the frozen ground offered an immediate, if uncertain, reprieve from scarcity. We might just manage to stabilize against this cold so that we do not slide into unnecessary hardship.


## granite4.2:8b
**pod lands quietly**

The pod thudded in the snow where Lumi was hauling wood and stopped mid-step. Vladimir looked up from pine saplings and saw the hatch open, spilling crates. Misha stirred from her bed as warmth seeped through the thin fabric of her shirt. No one cursed; everyone breathed a little deeper.


## ministral-3:14b
**GiftsFromWinter**

   Misha finally lowered her pistol—the raider’s blood had gone cold, and for a long moment she just stared at her trembling hands, glad of something to think about *other* than bored walls. Then the pod came, hissing into the snow like a gift from winter itself; those jagged blocks inside were silver, and not two but fifty of them, stacked heavy in a nest of synthfoam. Vladimir, knife halfway to the chopping block above her head, swore loud enough to crack ice, but even he sounded cheerful now. Plague or not, this might be the first chance LongTestColony’s had scraped itself back from the knives.


## phi4:14b
**A Blessing in Irony**

Vladimir was out harvesting pine trees when the ResourcePodCrash (severity:MEDIUM) pierced the silence, offering resources while his companions bore injuries from recent raids. The pod's arrival meant a glimmer of hope, though not enough to forget their wounds—or Vladimir's fatigue as he carried on under bitter stars. Misha had been resting peacefully in her bedroom until the respite was shattered by the echo of incoming metal; curiosity drew her outside despite her aching heart and mind. Lumi stopped hauling wood momentarily, wondering if this crash brought salvation or simply more burdens with winter nipping at their heels.

