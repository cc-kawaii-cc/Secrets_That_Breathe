# Worn Thai police traffic cone

Game folder: `Assets/SecretsThatBreathe/Props/ThaiTrafficCone/`.
Drag `ThaiTrafficCone_Worn.prefab` into a scene. It includes one MeshRenderer,
one material and a convex MeshCollider. No Rigidbody or runtime script is added.
The FBX is also supplied as an editable/importable alternative.

## Appearance and proportions

Orange PVC, two worn off-white tape bands, charcoal rubber square base with rounded
corners, scuffs and road dirt especially around the lower skirt/base.
Broad, restrained wear texture suits the existing low quality game style.
Nominal height 0.70 m, base 0.36 x 0.36 m, base thickness 0.045 m.
Top opening diameter 0.030 m; material wall thickness is modeled, with a through-open
interior and an open underside, not a cone surface with missing cap faces.
One rigid mesh with its pivot at base centre / ground level, identity transform.

Shape reference: Thai Provincial Police Region 8 equipment specification
https://p8.police.go.th/download/070866_Trafic.pdf
It describes an orange cone at least 70 cm high, a black square base at least 35 cm
wide and two white reflective strips. This asset uses those broad proportions and
appearance as a reference, not as a claim of certified real-world equipment.

## Files and cost

- `ThaiTrafficCone_Worn.prefab`: placement asset with convex collision.
- `Meshes/ThaiTrafficCone_Worn.asset`: native Unity mesh, 282 split render vertices, 288 triangles.
- `ThaiTrafficCone_Worn.fbx`: one authored mesh, 144 vertices / 144 quads / 288 triangles, material remapped.
- `ThaiTrafficCone_Worn.mat`: opaque URP Lit, metallic 0, smoothness 0.12, no emission.
- `Textures/ThaiTrafficCone_Worn_BaseColor.png`: 512 x 512, sRGB, bilinear, mipmaps.

White bands are painted tape in the albedo, not a retroreflective shader or emissive
surface. Lighting and shadows are controlled by the user's scene.
The prefab is dynamic/non-static; UV0 is an atlas, not a unique lightmap layout.
UV1 in the native mesh is unused; generate a proper UV2 before static light baking.
Convex collision approximates the envelope and intentionally closes the openings.

## Source and validation

- `ThaiTrafficCone_Worn.blend`: editable source.
- `build_cone.py`: deterministic geometry, UV placement, FBX export, studio preview rendering.
- `setup_unity.py`: writes only this asset's native mesh, material, metadata and prefab.
- `validate_cone.py`: actual FBX reimport and native buffer comparison.
- `Cone_Texture_Atlas.png`: original 1024px atlas from built-in ImageGen.
- `imagegen-prompt.txt`: exact generation prompt; final game atlas downsampled in Blender to 512px.
- `preview_front/back/top/underside.png`: renders of actual authored geometry and final texture.
- `export_front/back.png`: renders of reimported delivered FBX geometry and final texture.
- `build-report.json`, `unity-assets.json`, `validation.json`: measured results.

Validated: closed manifold material shell, no degenerate faces, positive volume,
through-open centre ray, single material, dimensions 0.36 x 0.36 x 0.70 m,
native mesh winding/normals/positions/UV buffer checks and FBX surface error below
0.000001 m. Viewed front/back, top opening, underside and updated base side UVs.
No existing project scenes or assets were modified.
Live Unity import/material appearance and PhysX collider cooking have not been tested.

## Polygon reduction

At the user's request, reduced the original 720 triangles to 288 (60% fewer).
Radial segments 24 -> 16; removed redundant longitudinal rings on the straight cone
walls and one base bevel ring. Kept the top hole, underside opening, shell thickness,
dimensions, texture and original asset GUIDs. Updated FBX, native mesh, prefab and
editable Blender source together. Original version archived outside Assets in `Archive_720tri/`.
