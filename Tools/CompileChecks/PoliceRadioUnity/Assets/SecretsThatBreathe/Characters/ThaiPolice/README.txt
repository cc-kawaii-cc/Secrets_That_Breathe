ThaiPolice_LowPoly.fbx is the new independent static character.
Drag the FBX into a scene. Its material is remapped to ThaiPolice.mat.
One mesh, 964 triangles, 1.8 m tall, 512px texture. No rig/animation.
URP Lit material responds to the scene lighting; the texture is not emissive.

Animated version: IdleTalk/ThaiPolice_IdleTalk.prefab.
Uses the ThaiPolice mesh's exact UV atlas mapping and ThaiPolice.mat on the
shared 484-vertex base geometry, with the original IdleTalk skeleton, skin weights,
bind poses, animation curves and controller. Static ThaiPolice_LowPoly.fbx is unchanged.
MainScene_2_Evening uses this animated version at the former LowPolyMale_IdleTalk position.
Validation: 964 triangles, 22 bones, 434 vertices move between sampled IdleTalk poses;
front/back previews and source in ArtSource/ThaiPolice/. Unity Play Mode not verified.
Editable Blender source and actual exported model previews:
ArtSource/ThaiPolice/
