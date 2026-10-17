# Low-poly male slow walk

Created from Assets/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx.
The original FBX was inspected but not modified. Its actual pose has angled arms and splayed legs.

Files:
- LowPolyMale_SlowWalk.blend: editable mesh, skeleton, skinning and both actions.
- build_slow_walk.py: reproducible authoring script, run in an isolated Blender process.
- validate_export.py: reopens the FBX, compares evaluated meshes, checks both clips, renders proof.
- Preview/SlowWalk_Preview.gif and SlowWalk_ContactSheet.png: actual rendered animation.
- Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_SlowWalk.fbx: Unity export.

Mesh: 1 object (original name Cube), 484 vertices, 964 triangles before and after rigging.
Skeleton: SlowWalkRig, 22 bones, at most 4 normalised bone influences per vertex.
Clips: SlowWalk_InPlace and SlowWalk_Forward, each 2.4 seconds (frames 1-73 at 30 fps).
Geometry, UVs and imported object scale are retained. No subdivision or extra body geometry.
Forward clip travels approximately 1.563636 source units per cycle on Blender -Y.
The .blend uses the original 4.220264-unit model height; the Unity prefab scales its Model child to 1.8 metres.
Preview camera/world are not exported to FBX. No existing Unity Scene has been modified.

Unity use:
1. Open Unity and allow scripts/model to import. LowPolyMaleSlowWalkSetup creates a looped Generic animation controller and LowPolyMale_SlowWalk.prefab alongside the FBX.
2. Drag that prefab into a Scene, place its root at floor height, then Play.
3. Slow Walking Character > Walk Speed defaults to 0.28 m/s. Disable Walk Forward to preview walking in place.
4. The prefab uses the InPlace clip and CharacterController movement, without applying root motion twice.
5. For custom root-motion controllers, use the separate SlowWalk_Forward clip.

Validation performed:
- Viewed original front/side, walk contact poses, and exported front/side/back.
- All vertices weighted, max 4 influences, no added triangles.
- Evaluated in-place first/last surfaces match to numerical precision.
- Sampled foot vertices do not penetrate ground during the source loop.
- Reimported FBX: 1 mesh + 1 armature, 22 bones, both 73-frame clips retained.
- Reimported deformed surfaces differ from the .blend by less than 0.000011 source units across five samples.
- Exported forward clip moves horizontally and does not fall along the vertical axis.
- Unity source code compiled against the local Unity assemblies.

Not yet verified: playback, imported material appearance, facing direction and collision behaviour in the actual Unity Editor/Play Mode. The character is an animation/straight-walking prefab, not a navigation AI.
