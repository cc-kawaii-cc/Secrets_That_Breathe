# Navy-suit antagonist

New independent character: `Assets/SecretsThatBreathe/Characters/NavySuitVillain/NavySuitVillain.prefab`.
Drag this prefab into the scene. It contains a 1.85 m tall relaxed standing character,
separate `Body` and `Sunglasses` children, two materials and a root CapsuleCollider.
The existing story placeholder `Assets/MainScenes/Prefeb/Villain.prefab` was read as
a serialization template but was not replaced. No scene was edited.

Appearance: deep navy two-button suit, notched lapels, ivory shirt, charcoal tie,
black dress shoes, neat short side-parted hair, serious closed-mouth expression,
opaque black rectangular sunglasses. Body details are intentionally restrained
for the game's low quality style.

## Game files and cost

- `NavySuitVillain.prefab`: standing character and capsule collision; no behaviour script.
- `NavySuitVillain_LowPoly.fbx`: editable/importable alternative, two meshes with material remaps.
- `Meshes/NavySuitVillain_Body.asset`: 964 triangles, 694 split render vertices.
- `Meshes/NavySuitVillain_Sunglasses.asset`: 108 triangles, 200 split render vertices.
- `NavySuitVillain.mat`: URP Lit, original atlas colors, white tint, smoothness 0.15.
- `SunglassesBlack.mat`: opaque black URP Lit, smoothness 0.68, no emission.
- `Textures/NavySuitVillain_BaseColor.png`: 512 x 512 sRGB, bilinear, mipmaps.

Total 1,072 triangles, two renderers / two material slots. The glasses have real
thickness, clipped corners, a bridge and side arms that reach the ears. Lens height
was fitted to the texture's eye line after close-up inspection. No transparency shader.
Hands retain the original low-poly shape, consistent with the user's earlier revert.
This game export is a baked static standing pose; it contains no animation or game rig.

## Editable source and generation

- `NavySuitVillain_LowPoly.blend`: editable body, glasses and a hidden private authoring rig.
  The helper rig/body use the existing authored IdleTalk frame-one standing posture;
  they are hidden and excluded from export. The original rig files are unchanged.
- `NavySuitVillain_Projection.png`: registered front/back artwork created with built-in ImageGen.
- `imagegen-prompt.txt`: exact generation prompt. The existing mannequin projection
  template was the registration input; no real person's identity was referenced.
- `build_villain.py`: bake the unique left/right atlas, build glasses, pose/bake the
  new character, normalize height, export FBX and native geometry data, render previews.
- `setup_unity.py`: write only this new character's meshes/materials/metadata/prefab.
- `validate_villain.py`: reopen actual FBX and compare the export and native buffers.

The original `lowpoly_male_base_mesh-TPOSE.fbx` is unchanged. Its 484-vertex body
is reused privately; no new body polygons were added. Only the new character's UVs,
materials, baked pose and glasses were created. Existing male/police characters and
scene/story references were not changed.

## Validation

Reopened delivered FBX: exactly `Body` and `Sunglasses`, 1,072 triangles, one material
per mesh, valid UVs, closed solid parts, no degenerate polygons, positive volumes.
Compared both native mesh position/index/normal buffers to the Blender export data.
Body height 1.85000002 m; maximum export position error below 0.000001 m.
All internal prefab object references resolve, and the original source hash is retained.
See `build-report.json`, `unity-assets.json`, `validation.json`.

Viewed front, back, side, three-quarter and face close-up renders from the actual
authored/exported model. `export_*.png` and `albedo_front.png` use flat albedo lighting
to show texture colors; `preview_*.png` use studio shading. They are not concept images.
Live Unity import/scene appearance and collision have not been visually tested.
The atlas is UV0, not a unique lightmap layout. Prefab is non-static; generate a valid
lightmap UV2 if a later workflow requires static light baking.
