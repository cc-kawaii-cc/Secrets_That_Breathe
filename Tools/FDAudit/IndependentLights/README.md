# FD V.2 independent light meshes — 2026-10-26

Superseded at the user's request by `../FullLensTexture/README.md`: the active prefab now uses full-lens texture-based emission and no additional emitter meshes. Earlier generated meshes/materials and metadata are archived under `../FullLensTexture/ArchivedIndependent/`. This file describes the historical independent-geometry implementation; do not run its builder to update the current lights.

Saved `Assets/Model Palm/FD V.2/FD.prefab` with four new selectable children:

- `FD_Headlight_Emission_Left` and `FD_Headlight_Emission_Right`: ordinary white low-beam bulbs, no DRL. Shared material: `Material/FD_Headlights_Independent_Emission.mat`.
- `FD_Taillight_Emission_Left` and `FD_Taillight_Emission_Right`: clean red upper rear light surfaces. Shared material: `Material/FD_Taillights_Independent_Emission.mat`.

Both materials use the existing URP Lit shader with a dark base color and constant HDR Emission Color. Every texture reference is empty, including Base Map and Emission Map. Adjust front and rear color/intensity independently on their materials. Source Body/Light material bindings have been restored, so the original atlas no longer drives emission.

The four meshes are under `Meshes/`. They are closed thin solids with smooth new outlines and a slightly domed emitting face. Original FBX triangle positions were sampled to measure the outer opaque glass depth; a quadratic support surface smooths those samples, then moves the new emitter at least 1.5 mm toward the outside at the sampled points. Maximum fitting offsets are 3.8–5.3 mm. The source's uneven triangles and UV islands do not shape the new light outlines. This is measured placement with sampled clearance, not a proof of zero intersections at every point.

The front discs use 64 contour samples. Rear outlines are rounded with three Chaikin passes to 48 samples. Four radial rings, closed backs and thin side walls produce 2,240 added triangles. Total: 30,624 → 32,864 triangles and 8 → 12 mesh objects. No modifiers were added. Generated UV0 is unused by the solid-color materials; UV1 is not a baked lightmap unwrap. New renderers are dynamic and cast no shadows.

Original object/component IDs, names, hierarchy, transforms, pivots, source FBX, importer, original textures and original shared materials are preserved. The original root gains four child references. The scene file is not saved or modified by this builder. Source FBX centimetres are converted to metres and reflected on X, with reversed triangle winding; this conversion was independently checked against the existing Porsche FBX/native mesh pair.

`../build_independent_lights.py` owns only the four generated meshes, two materials, four child objects and their root entries. Re-running updates them in place and keeps their GUIDs/IDs. It does not reset the root or modify source geometry. `*.npz` keeps editable generated geometry in source coordinates. Backups preserve the pre-change prefab/FBX/importer. `verify.py` checks prefab preservation, zero material texture dependencies, index validity, closed manifold topology, positive volume, normal/tangent validity and matching data serialization. See `final_validation.json`.

`../preview_independent_lights.py` renders the actual generated mesh data together with the unchanged FBX. Close and oblique PNGs show placement and clean silhouettes, without Bloom. These are CPU diagnostic renders, not Unity screenshots; source n-gons are fan-triangulated and wheel/license materials are placeholders. Not verified: live Unity import, in-scene Bloom, standalone build or Blender/FBX export. These are visible emissive model surfaces, without road-projecting lights or brake/indicator logic.

The former atlas-driven FD emission material, texture and metadata were moved out of Assets into `../Lights/LegacyAtlas/`, after confirming no scene/prefab references remained. Earlier backups and diagnostics stay under `../Lights/` for recovery.
