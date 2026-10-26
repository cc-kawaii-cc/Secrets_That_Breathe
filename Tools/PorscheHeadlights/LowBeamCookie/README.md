# Porsche low-beam cookie

Installed on the two existing `Spot Light` / `Spot Light (1)` components of the
Porsche instance in `Assets/Nemo_Upgrade/MainScene_2_Evening.unity`.

- Asset: `Assets/SecretsThatBreathe/Lighting/Cookies/Porsche_LowBeam_LHT.png`
- 1024 x 1024, linear grayscale RGB plus matching luminance alpha, uncompressed,
  bilinear, Clamp, no mipmaps. URP uses the RGB channels.
- Analytic low-beam approximation: lightly feathered upper cutoff (about seven
  texture pixels, fading below the cutoff), bounded left shoulder
  upsweep for left-hand traffic, central hotspot and softer foreground spill.
  Both lamps use the same traffic-handed pattern; do not mirror one lamp.
- Spot outer/inner angles: 70 / 62 degrees. Local downward aim: 0.8 degrees.
- Existing positions, intensity (9), range (100), colour and shadow settings
  are preserved. The source car prefab/model are not edited.
- This is a game lighting approximation, not measured Porsche photometry.

`Assets/Editor/PorscheLowBeamCookieSetup.cs` contains the deterministic generator.
To regenerate and reapply explicitly, use Unity menu:
`Tools > Secrets That Breathe > Porsche > Install Low Beam Cookie (Evening Scene)`.
Open the Evening scene and exit Play Mode first. Importing the script does not
automatically modify any scene; a one-shot request file is used only during setup.

Verification:
- `result.json`: two lights assigned, normalized texture peak 1, zero texture
  output above the cutoff. Unity compiled and executed the installer.
- `wall-preview.png`: actual URP projection onto a temporary wall at 10 m.
  Preview lamps use intensity 120 to show the profile clearly; scene lamps stay 9.
  Ambient lighting/fog from the scene may appear on the test wall.
- `road-preview.png`: actual scene render with the installed lamps at intensity 9,
  without camera post-processing. No Play Mode drive-through was performed.

Original live scene backup (includes pre-existing unsaved user changes):
`before-20261025-210345487.unity`.
Original disk scene: `on-disk-before-20261025-210345487.unity`.
Later backups were created during profile refinement. Restore the first live
backup to undo only this task while retaining the previous live scene state.

References consulted:
- Unity URP Light component: https://docs.unity.com/en-us/engine/6000.3/manual/lighting-overview/lighting/light-component
- HELLA beam patterns: https://www.hella.com/techworld/en/technical/automotive-lighting/light-distribution/
- URP package `LightCookie.hlsl` and `LightCookieManager.cs` for projection axes.
