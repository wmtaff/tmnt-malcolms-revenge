# Review a repeating street background

Use the saved art prompt with OpenAI image generation, save each PNG candidate under an ignored `artifacts/` or `local/` directory, inspect it, then review four horizontal repeats. Preserve the prompt and generation settings alongside each candidate so another generation can be compared. These tools do not call an image-generation API or require a key; generation is a separate workflow step.

The Python interface is:

```python
import json
from tmnt_mod.backgrounds import inspect_background, write_repeat_preview

report = inspect_background('artifacts/street-candidate.png')
print(json.dumps(report, indent=2))
result = write_repeat_preview('artifacts/street-candidate.png', 'artifacts/street-repeat.html')
print(result['output'])
```

The preview output directory must already exist. The preview refuses existing outputs and source/output aliases; use a new filename for each revision. It embeds the original PNG unchanged in a self-contained HTML file, with no external image requests, scripts, or absolute local paths. Open that HTML in a browser. The native-size and 2× views both show four horizontally repeated copies with pixelated rendering and scrollbars. A filename is displayed only as escaped text.

## Input bounds and report

Input must be a single-frame PNG of at most 32 MiB, at most 4096 pixels along each axis, and at most 8,000,000 pixels total. Bounds are checked before full pixel conversion. Invalid formats, corrupt data, animation, and exceeded bounds raise an error rather than producing a preview. Inspection never changes the source PNG.

`inspect_background(path)` returns a JSON-compatible dictionary. `write_repeat_preview(path, output)` returns `{"output": ..., "report": ...}` with the same report. The report includes dimensions, original mode, source-byte SHA256, alpha pixel counts, palette information, and these diagnostics:

| Field | Meaning |
| --- | --- |
| `seam.rgb_mean_absolute_difference` | Mean absolute RGB channel difference between the leftmost and rightmost columns, on a 0–255 scale. |
| `seam.rgb_max_absolute_difference` | Largest RGB channel difference across those paired edge pixels. |
| `seam.alpha_mean_absolute_difference` / `alpha_max_absolute_difference` | Corresponding differences in alpha, separate from RGB. |
| `edge_strips.rgb_mean_absolute_difference` | Paired RGB differences between the first and last up-to-16-column strips, in their original order. |
| `edge_strips.mean_color_absolute_difference` | Difference between the strips' mean RGB colors; a broad lighting/color diagnostic. |
| `palette.visible_colors` | Exact visible RGBA count when the full RGBA palette fits within 65,536 entries; otherwise null with `exact: false`. Fully transparent entries do not count as visible. |

RGB diagnostics use converted 8-bit raw channels, including hidden RGB under transparent pixels. Review alpha separately. The preview retains the original PNG, including any color-profile information; browser color management can differ from numerical channel comparisons.

The warning thresholds—12/255 mean seam difference and 24/255 mean strip difference—are initial review heuristics, not calibrated acceptance criteria. High values can occur in intentional texture; matching edge pixels can still conceal a visibly repeating landmark or a bad transition one column away. `structurally_valid` means only that a bounded single-frame PNG was decoded. It does not mean seamless, stylistically correct, game-ready, or compatible with a particular layer layout.

## Visual review and iteration

Check each join for road/sidewalk continuity, aligned rooflines and horizon, consistent lighting, abrupt silhouettes, repeated text, obvious landmarks, and transparency artifacts. Review at native size before using the enlarged view. Compare several consecutive repeats: attractive individual artwork can still repeat poorly.

Revise the saved generation prompt and generate a new candidate when the join or style needs work. Keep the prior PNG, report, and preview for comparison. These tools never synthesize pixels, resize the source, perform automatic seam repair, infer game collision data, or install the artwork into the game.
