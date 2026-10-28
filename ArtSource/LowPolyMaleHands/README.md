# Five-finger hands for the original TPOSE base mesh

Updated asset: `Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx`.
The original hand had a thumb and fused finger regions. Both hands now have a thumb,
index, middle, ring and little finger, with relaxed separation and tapered eight-sided digits.
The wrists share vertices with the original arms; no floating hand objects or overlapping shells were added.

## Files

- `LowPolyMale_FiveFingers.blend`: editable source, original object `Cube`, original transform and material.
- `lowpoly_male_base_mesh-TPOSE.before.fbx`: exact original FBX backup.
- `original.fbx.meta.txt`: exact original importer metadata backup, including GUID.
- `build_hands.py`: reproducible hand generation, preserved body UV/corner normals and geometry-only FBX replacement.
- `validate_hands.py`: reimports the actual export and checks surface, topology, source metadata and FBX structure.
- `candidate.fbx`: validated delivery copy, identical to the installed asset.
- `export_hand_right.png`, `export_hand_left.png`: renders of the reimported delivery FBX.
- `before_*.png`, `after_*.png`: actual geometry inspection views.
- `build-report.json`, `validation.json`: measured validation results.

## Preserved / changed

One mesh before and after: 484 -> 716 vertices, 964 -> 1,428 triangles.
356 original vertices and 376 body polygons outside the replaced hand regions retained.
Body UVs and imported corner normals preserved; new hand UVs projected onto the original hand surface UVs.
Original FBX object/geometry IDs, names, parentage, transforms, materials, connections,
global units/axes and importer `.meta` preserved. Body proportions and original pose retained.
Only the named static base mesh was edited; existing separately rigged walking/talking variants were not rebuilt.
The original static base mesh has no armature or animation to preserve or test.

Export reimport: one `Cube` mesh, zero surface position error from authored Blender source,
closed manifold, positive volume, no degenerate faces, original height retained.
Both exported hands viewed, plus palm/back/edge and complete-body previews.
The temporary hand-only preview cuts at the lower forearm for framing; the delivered mesh is closed and attached to the body.
No Unity scenes were saved or screen controlled. Appearance after the live Unity Editor reimport has not been visually checked.
