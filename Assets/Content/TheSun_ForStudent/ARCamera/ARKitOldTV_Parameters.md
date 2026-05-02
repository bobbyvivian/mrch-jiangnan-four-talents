# ARKit Old TV Effect — Parameter Reference

Documentation for every property exposed by the modified `ARKitBackground` shader and the `ARKitOldTVEffectController`. All parameters are tunable at runtime via the controller and live-update in the inspector.

---

## Keyword

### `ARKIT_OLD_TV_EFFECT`
The shader keyword that gates the entire effect. When **off**, the shader compiles to the original ARKit background with zero overhead — no color sampling, no noise math, nothing extra. When **on**, the effect chain runs and is blended in via `_OldTVStrength`.

The controller toggles this automatically: it enables the keyword before a fade-in starts, and disables it once strength reaches `0` after a fade-out. You normally don't touch this directly.

---

## Master

### `_OldTVStrength` — Range `0..1`, default `1`
The master blend between the original camera feed and the fully processed "old TV" output. This is **the value the controller animates** during transitions; everything else stays static.

| Value | Result |
|------:|--------|
| `0.0` | Pure original camera feed (effect contributes nothing) |
| `0.5` | Half-blend — both looks visible simultaneously |
| `1.0` | Fully stylized 1930s look |

> Keep this at `1` in the material asset; let the controller drive it.

---

## Color & Tone

### `_SepiaTint` — Range `0..1`, default `0.7`
Controls how the desaturated image is tinted. The shader first converts the camera feed to luminance, then mixes between pure grayscale and a sepia color curve.

- `0.0` — Cold, neutral **black-and-white** (early TV / newsreel feel)
- `0.5` — Slightly warm gray — looks like aged silver gelatin
- `1.0` — Strong **sepia/amber** wash (classic "old photograph")

### `_Contrast` — Range `0..3`, default `1.4`
Multiplier around mid-gray (0.5). Early film stock had punchy contrast because of how silver grains responded to light, so values around `1.3–1.7` are typical.

- `< 1.0` — Faded, washed out (works for "very degraded archive" looks)
- `1.0` — Linear, no change
- `1.4–1.6` — **Sweet spot** for filmic crunch
- `> 2.0` — Crushed shadows, clipped highlights — high-key dramatic

### `_Brightness` — Range `-1..1`, default `-0.05`
Constant offset added after contrast. Useful for compensating when contrast pushes too dark or too bright.

- Negative — global darkening (pairs well with high contrast to keep highlights from blowing out)
- `0.0` — No offset
- Positive — global lift; rarely needed unless going for an overexposed feel

> Adjust **after** setting contrast, not before.

---

## Grain (film noise)

### `_GrainStrength` — Range `0..1`, default `0.45`
How visible the per-pixel film grain is. The grain is recomputed at ~24 frames per second to feel filmic rather than digital.

- `0.0` — Clean image, no grain
- `0.2` — Subtle texture, barely perceptible motion
- `0.45` — **Default**: clearly visible, period-appropriate
- `0.7+` — Heavy, almost snowy — "very degraded reel"

### `_GrainSize` — Range `50..2000`, default `600`
Controls the grain's spatial frequency (how big each noise speck is).

- **Low** (`50–200`) — Coarse, chunky grain — looks like Super 8 or low-quality video
- **Mid** (`400–800`) — Fine, classic 35mm film grain
- **High** (`1000+`) — Very fine, near-digital noise

> Grain size and viewport size interact. On smaller screens, lower the value or grain becomes invisible; on iPad, raise it or the grain looks too fine.

---

## Vignette

### `_VignetteStrength` — Range `0..2`, default `1.2`
How dark the corners of the frame become. Old projector lenses and tube cameras naturally darkened toward the edges.

- `0.0` — No vignette
- `1.0` — Standard photographic vignette
- `1.2` — **Default**: noticeable but not theatrical
- `2.0` — Heavy black corners; CRT bubble feel

### `_VignetteSoftness` — Range `0.05..2`, default `0.55`
Controls **where** the vignette starts falling off, not how dark it gets.

- **Low** (`< 0.3`) — Vignette pulled close to center; even mid-frame is darkened
- `0.55` — **Default**: corners darken, center stays clean
- **High** (`> 1.0`) — Vignette pushed all the way to corners; only the very edges darken

> Think of strength as "how black" and softness as "how far in."

---

## Scanlines (CRT lines)

### `_ScanlineStrength` — Range `0..1`, default `0.25`
Visibility of the horizontal scanline pattern. Strictly speaking, 1930s mechanical TVs used vertical disc scans, but horizontal scanlines read more universally as "old TV" to viewers.

- `0.0` — No scanlines (cleanest filmic look)
- `0.25` — **Default**: gentle CRT hint
- `0.5+` — Strongly visible bands; reads as 80s/90s CRT more than 1930s

> If you want a pure film look (no TV component), set this to `0`.

### `_ScanlineCount` — Range `50..2000`, default `600`
Number of scanlines across the screen height.

- **Low** (`100–300`) — Big chunky lines, very vintage CRT
- `600` — **Default**: dense, subtle
- **High** (`1000+`) — Almost invisible interference pattern

> Match this roughly to your screen's vertical pixel count for a "1:1 line per pixel row" CRT vibe; halve it for a "low-res TV" feel.

---

## Flicker (brightness instability)

### `_FlickerStrength` — Range `0..1`, default `0.18`
How much the overall image brightness wobbles over time. Comes from two combined sources: a smooth sine wave (steady pulse) and per-frame random jitter (random unsteadiness).

- `0.0` — Rock-steady image
- `0.18` — **Default**: subtle, period-appropriate breath
- `0.4+` — Heavy pulsing — cinema with a dying bulb

### `_FlickerSpeed` — Range `0..30`, default `14`
Frequency of the sine component of the flicker, in radians per second.

- `0–5` — Slow, dreamy pulsing
- `14` — **Default**: rapid, mechanical-projector feel
- `25+` — Buzzing, almost strobe-like

> The random per-frame jitter component runs at ~24fps regardless of this value, so you'll always see fine flicker even at speed `0`.

---

## Scratches (film damage)

### `_ScratchStrength` — Range `0..1`, default `0.35`
Brightness of occasional vertical white lines that flick across the image, simulating physical scratches on film stock. Lines fade out at the top and bottom of the frame so they look like real damage rather than UI elements.

- `0.0` — Pristine film, no damage
- `0.15–0.25` — Occasional faint lines (good for "well-preserved archive")
- `0.35` — **Default**: clearly visible scratches every second or so
- `0.6+` — Heavily damaged reel; lines will be very bright

> Scratches are randomized at ~6Hz, so a higher value also means more **frequent** lines, not just brighter ones. Lower this first if the effect feels too busy.

---

## Jitter (vertical instability)

### `_JitterStrength` — Range `0..0.05`, default `0.004`
Vertical UV offset applied to the video sampling, simulating an unsteady projector or weak film transport. Updates at ~12Hz (jumpy rather than smooth) to read as mechanical instability.

- `0.0` — Perfectly stable image
- `0.002` — Subtle breathing motion
- `0.004` — **Default**: clearly noticeable, projector-like
- `0.02+` — Very rough — broken transport mechanism

> Note: jitter is also scaled by `_OldTVStrength`, so it fades in/out cleanly during transitions. **Depth sampling is intentionally NOT jittered** — AR occlusion stays aligned with reality even when the visible image wobbles.

---

## Preset Suggestions

Drop these into the controller's parameter fields as starting points.

### "Pristine 1930s Sepia Photograph"
Clean, warm, no scratches — like a well-preserved archive image.
```
SepiaTint = 0.9     Contrast = 1.3    Brightness = 0
GrainStrength = 0.2 GrainSize = 800
VignetteStrength = 1.0  VignetteSoftness = 0.7
ScanlineStrength = 0    FlickerStrength = 0.05  FlickerSpeed = 8
ScratchStrength = 0.05  JitterStrength = 0.001
```

### "Damaged Silent-Film Reel" (default-ish, the one you have)
Heavy character — grain, scratches, projector wobble. Reads as 1920s–30s cinema.
```
SepiaTint = 0.7     Contrast = 1.4    Brightness = -0.05
GrainStrength = 0.45 GrainSize = 600
VignetteStrength = 1.2  VignetteSoftness = 0.55
ScanlineStrength = 0.0  FlickerStrength = 0.18  FlickerSpeed = 14
ScratchStrength = 0.35  JitterStrength = 0.004
```

### "1950s CRT Television"
Black-and-white, scanlines dominant, less film grain.
```
SepiaTint = 0.1     Contrast = 1.5    Brightness = -0.05
GrainStrength = 0.15 GrainSize = 300
VignetteStrength = 1.4  VignetteSoftness = 0.4
ScanlineStrength = 0.5  ScanlineCount = 400
FlickerStrength = 0.1   FlickerSpeed = 6
ScratchStrength = 0.0   JitterStrength = 0.002
```

### "Found Footage / Horror VHS"
Heavy degradation, fast flicker, lots of scratches.
```
SepiaTint = 0.4     Contrast = 1.7    Brightness = -0.1
GrainStrength = 0.7 GrainSize = 250
VignetteStrength = 1.6  VignetteSoftness = 0.4
ScanlineStrength = 0.4  ScanlineCount = 500
FlickerStrength = 0.35  FlickerSpeed = 20
ScratchStrength = 0.6   JitterStrength = 0.012
```

---

## Performance Notes

- The full effect path adds a handful of `sin`/`frac` ops, two `Hash21` calls, and ~3 extra `lerp`s per pixel. On A14+ devices this is well under 1ms full-screen at native res. On older devices, the cheapest knobs to drop are `_ScratchStrength` (one extra hash) and the scanline math (a `sin`).
- When `_OldTVStrength = 0` and the keyword is off, the shader is bit-identical to the original ARKit background. The controller handles this for you on `DisableImmediate()` / fade-out completion.
- All effect parameters live in their own CBUFFER (`ARKitOldTVProperties`), so updating them via `material.SetFloat` does not invalidate the YCbCr texture bindings.
