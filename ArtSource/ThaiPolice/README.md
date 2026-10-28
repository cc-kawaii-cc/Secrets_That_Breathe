# Thai police low-poly character

New independent model at `Assets/SecretsThatBreathe/Characters/ThaiPolice/ThaiPolice_LowPoly.fbx`.
Drag this FBX from the Unity Project browser into a scene after import.
Its importer remaps `ThaiPolice` to the supplied URP Lit material automatically.
No original character, material, texture, or scene was replaced.

Appearance: brown/taupe khaki long-sleeved uniform, chest pockets, epaulettes,
simplified badges/ribbons/nameplate, black belt and silver buckle, black closed shoes,
short dark cropped police haircut and a fictional adult Thai male face. No hat.
Details and hair are painted into the atlas; this is a low detail game character.
The original mitten hands are retained, as requested when the finger edits were reverted.

## Game assets

- `ThaiPolice_LowPoly.fbx`: one mesh, 484 vertices / 964 triangles, about 1.8 metres tall, origin at feet.
- `ThaiPolice.mat`: opaque URP Lit, white base tint, smoothness 0.15, metallic 0, no emission.
- `Textures/ThaiPolice_BaseColor.png`: 512 x 512 sRGB, bilinear, mipmaps, compressed.
- Importer/material/folder metadata are included, with independent GUIDs.

Base geometry comes from the restored `lowpoly_male_base_mesh-TPOSE.fbx`.
The new copy is normalized to 1.8 m; UV halves are separated to preserve asymmetric badges.
The original mesh is unchanged. The asset is a static A-pose model; no armature or animation was requested or added.

## Source and reproducibility

- `ThaiPolice_LowPoly.blend`: editable model and final material.
- `ThaiPolice_Projection.png`: front/back registration artwork made with built-in ImageGen.
- `imagegen-prompt.txt`: exact image generation prompt. The user's five images were reviewed;
  four were supplied to ImageGen along with the registration template, within its five-image input limit.
- `build_police.py`: bake full front/back appearance to the unique atlas, complete head sides with a simple tapered crew cut, export and render.
- `setup_unity_assets.py`: create only this character's Unity material/importer metadata.
- `validate_police.py`: reimport the delivered FBX, check geometry/height/atlas and render it.
- `build-report.json`, `validation.json`, `unity-assets.json`: measurements and asset IDs.
- `export_*.png`, `albedo_front.png`: actual model with flat albedo illumination, for assessing texture colors.
- `preview_*.png`: studio-lit model, showing how shading can darken the same texture.

## Validation and lighting

Reimported the delivered FBX: one closed manifold mesh, no degenerate faces, 964 triangles,
one material, 512px atlas, height 1.8000001 m, maximum surface error below 0.000001 m.
Viewed front, back, side and three-quarter actual geometry, including exported textured views.
Original source hash is unchanged.

The generated registration artwork is not proof of final in-game appearance.
URP Lit responds to scene lights, shadows and exposure, so a dim scene can darken it.
Flat albedo images show the atlas colors without that additional shading; they are labeled accordingly.
The generated artwork already contains subtle painted creases/shading, appropriate to the low-detail style.
Live Unity import/material/scene appearance has not been visually checked. No scene lighting or exposure was changed.
