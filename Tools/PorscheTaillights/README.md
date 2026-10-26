# Porsche V.2 tail light emission — 2026-10-26

Saved the existing `Assets/Model Palm/Porsche V.2/PorscheV.2.prefab` with one new child, `Taillights_Emission`. Its material is `Material/Taillights_Emission.mat`; adjust **Emission Color** (HDR) to change color and strength. The full-width upper guide and two lower short segments on each side follow the original rear surfaces. The reference car differs from this existing winged model; the car body was preserved.

Revision: the dedicated mesh now builds five closed light guides from a smooth analytic plan curve, independent of the car's original triangulation. The main guide has a constant height and thickness across its full width. The four short lower guides form two segments on each side. The cross section is elliptical; normals and UV0 are generated, with vertex colors distinguishing the lower guide intensity. The dedicated URP shader emits uniformly over the new geometry and uses ordinary opaque depth testing. It does not cast shadows. There are 2,384 new triangles and one renderer (26,436 → 28,820 triangles; 9 → 10 mesh objects). Original transforms and pivot are preserved.

The original meshes, normals, UVs, textures, materials, object/component IDs and transforms remain unchanged. The prefab diff only adds one child and its renderer/filter. No new texture is required. No runtime script or light was added. The shader supplies visible emission; the scene's existing Bloom supplies the halo. This is not a brake/turn-signal controller or a baked light source.

Verified in Unity 6000.3.8f1: shader compiles, emission persists after material reimport, rear/oblique/close diagnostic renders, and original prefab blocks remain identical except for the added root child. See `final_validation.json` and `final_unity_inspection.json`. The user's open scene is not saved by the tool. The preview PNGs use matching neutral setup with post-processing disabled, so they exclude scene Bloom.

The editor-only builder `Assets/Editor/PorscheTaillightTools.cs` responds only to explicit one-use request files in this folder, with consumed IDs retained. It owns only the new overlay mesh/material/child. `PorscheV.2_before.prefab` preserves the pre-task prefab. The atlas baking script/image are diagnostic work files; the final material uses no atlas emission mask because the source UV islands produced visible seams.

Not tested: standalone player build, GI baking or Blender/FBX export. This change is saved in the Unity prefab and Unity mesh/material assets; original Blender/FBX sources were not modified.
