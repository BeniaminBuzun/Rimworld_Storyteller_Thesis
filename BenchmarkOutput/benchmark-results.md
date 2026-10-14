# Model Benchmark

| Provider | Model | Result | Load (ms) | Run (ms) | Prompt | Completion | Reasoning | Effects | Error |
|---|---|---|---:|---:|---:|---:|---:|---|---|
| Ollama_Local | tobestyledintro/minicpm5-2b:latest | FAIL | 5938 | 27011 | 1588 | 2753 | 0 |  | No JSON object or array found in response. |
| Ollama_Local | qwen3.5:0.8b | FAIL | 27958 | 170999 | 16344 | 41 | 0 |  | The response contained no effects after validation. An effects array with at least one valid effect is required. |
| Ollama_Local | granite4.2:3b | FAIL | 5669 | 45563 | 1523 | 3555 | 0 |  | The response contained no effects after validation. An effects array with at least one valid effect is required. |
| Ollama_Local | qwen3.5:4b | FAIL | 14010 | 224393 | 13115 | 484 | 0 |  | The response contained no effects after validation. An effects array with at least one valid effect is required. |
| Ollama_Local | ministral-3:3b | FAIL | 8276 | 3702 | 2203 | 68 | 0 |  | The response contained no effects after validation. An effects array with at least one valid effect is required. |
| Ollama_Local | gemma4:e4b | FAIL | 15058 | 18898 | 2159 | 230 | 0 |  | The response contained no effects after validation. An effects array with at least one valid effect is required. |
| Ollama_Local | granite4.2:8b | PASS | 11798 | 28285 | 1523 | 941 | 0 | PawnFoundItems, MoodBuff, ResourcePodCrash |  |
| Ollama_Local | qwen3.5:9b | PASS | 26975 | 591930 | 21741 | 193 | 0 | PawnFoundItems, MoodBuff, ResourcePodCrash |  |
| Ollama_Local | ministral-3:14b | FAIL | 30992 | 24312 | 2204 | 415 | 0 |  | Response contains JSON but it could not be parsed (malformed). |
| Ollama_Local | phi4:14b | PASS | 23683 | 17855 | 1380 | 293 | 0 | ResourcePodCrash, PawnFoundItems, MoodBuff |  |

## granite4.2:8b
**Pod Crash and Item Find**

Something happened.

PawnFoundItems, MoodBuff, ResourcePodCrash


## qwen3.5:9b
**Strange Event**

Something happened.

PawnFoundItems, MoodBuff, ResourcePodCrash


## phi4:14b
**Unlikely Fortune**

Vladimir was harvesting a pine tree when his axe struck something metallic lodged in its trunk. A pod from this crash, ResourcePodCrash: metalCacheHigh, crashed nearby, revealing a bounty of rare materials and equipment. Misha, restless despite the deep cold of her bedroom, wandered out upon hearing the commotion. While sneaking through the snow to investigate, she stumbled upon assorted items scattered on the ground, PawnFoundItems: randomToolsMetalMedium, sparking a surprising curiosity within her. Her mood shifted positively, MoodBuff: newfoundHopeHigh, as if the universe had finally shown mercy on this beleaguered settlement. There was an irony in how her discovery could boost morale just when their supplies were scraping by.

ResourcePodCrash, PawnFoundItems, MoodBuff

