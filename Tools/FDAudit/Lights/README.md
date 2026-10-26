# FD V.2 conventional headlamps and rear emission — 2026-10-26

Historical bulb-only atlas implementation. The current full marked-lens texture method is documented in `../FullLensTexture/README.md`; it replaced the later independent-geometry experiment at the user's request. `LegacyAtlas/` keeps the earlier material/texture versions. Do not run the bulb-only builder to update the current full-lens lights. Preview PNGs in this folder are now refreshed by `../preview_fd_lights.py` using the active texture method.

Saved `Assets/Model Palm/FD V.2/FD.prefab` with one new material, `Material/FD_Lights_Emission.mat`, and one shared 4096-square RGB emission atlas, `Texture/FD_Lights_Emission.png`.

The front lights emit from the large conventional low-beam aperture on each side. There are no DRL strips or rings. The smaller high-beam/indicator reflectors and side repeaters remain unlit. The rear running-light emission covers the continuous upper red lens area on each side, while lower clear reverse/indicator areas remain dark. Headlamps are warm white; rear lights are red.

The original separate Light mesh is behind an opaque glass surface included in Body. The same selective mask therefore also applies to Body's atlas material slot so emission is visible at the outer glass. Both bindings use a clone of the original URP Lit material, preserving base texture, lighting properties and all other material values. The mask is black everywhere outside the lamp apertures. The original shared material is untouched. HDR Emission Color is (4,4,4); the red channel's mask value is lower to keep rear emission below the front intensity.

No mesh or geometry changes were necessary. There are still 8 mesh objects and 30,624 source-FBX triangles. Object/component IDs, transforms, pivots, hierarchy, FBX, importer, original textures and scene files remain unchanged. The prefab diff consists of two material GUID references. This adds visible emission, not road-projecting spotlights or brake/indicator logic.

Source: `../build_fd_lights.py`. Backups preserve the pre-edit prefab, FBX and original material in this folder. `verify.py` checks preservation, references, colors and counts. `../preview_fd_lights.py` generates close and oblique CPU diagnostic projections from the actual FBX geometry/UVs and saved emission map. They approximate diffuse plus emission, without Unity lighting, tone mapping or Bloom. N-gons are fan-triangulated for the diagnostic, and wheel/license materials are placeholders. Use them to inspect emitter placement, not final in-game appearance.

Not verified in this pass: live Unity reimport, in-scene Bloom, standalone build, or Blender/FBX export. The existing scene was not saved or controlled through the screen.
