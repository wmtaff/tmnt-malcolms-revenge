# Residential street and Baxter prototype

User-approved direction: a suburban street in summer, starting at a light-pink house and ending at a community park. A small number of Foot Soldiers precede Baxter Stockman. Develop a simple repeating background pipeline using OpenAI image generation in the game's existing pixel-art style.

The route uses three ordered scenic sections: house, repeatable connecting street, and park with playground. Reference observations inform architecture and landscaping; private address and map imagery stay outside the repository. Summer means green lawns and deciduous trees, blue sky, and no snow. Runtime world spans are 4096–4896, 4896–5664, and 5664–6464, with a proposed player start at 4250,360. These are native game coordinates, not geographic measurements.

Generate a horizontal street tile from local street references using built-in OpenAI image generation. Store the generated asset and exact prompt in the project; keep extracted reference art local and ignored. Inspect dimensions, palette, transparency, and horizontal seam metrics, then view repeated copies at game-like scale. Diagnostics cannot certify artistic quality or engine compatibility. Preserve source generation output and version revisions instead of silently fixing or replacing it.

Prefer reusing Baxter's native Stage12 boss scene and progression because his behavior depends on arena objects. Research the scene's camera blocks, spawn groups, player start, boss trigger, and completion flow before choosing runtime changes. A cosmetic background substitution must not pretend that imported art automatically changes collision or boss dependencies. Keep actual imports and gameplay claims tied to observed runtime evidence.

Maintain isolated playtests, pinned native assembly checks, baseline/rollback, and ignored game data. Reuse the previous configuration and launcher components where applicable. Do not install binaries into Steam. Scope this as a short prototype, not a full campaign level.
