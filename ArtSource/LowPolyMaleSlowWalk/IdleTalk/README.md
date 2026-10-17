# IdleTalk — standing conversation idle

- Source: LowPolyMale_IdleTalk.blend (separate from the walking source).
- Export: Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_IdleTalk.fbx
- 4.8-second loop, 30 fps, subtle sideways weight shift, head and shoulder movement. Feet remain planted; no forward travel.
- Original mesh preserved: 484 vertices, 964 triangles, 22 bones. Existing walking assets and Scene are not changed.

## Unity usage
Return to Unity and let scripts compile. The exact-file setup creates LowPolyMale_IdleTalk.controller and LowPolyMale_IdleTalk.prefab beside the new FBX. Drag the new prefab into the Scene and press Play. It has no walking component.
If needed, run Tools > Secrets That Breathe > Set Up Idle Talking Character.
Animator Speed controls playback speed (1 = 4.8 seconds per cycle, 0.5 = 9.6 seconds).

## Validation
Blender source and exported FBX checked at five poses. Export has only one IdleTalk take, one mesh and one armature. Maximum surface export error: 0.00002656 source units; first/last exported vertex difference: 0. Actual exported front, side and back renders checked. Preview/IdleTalk_Preview.gif shows the full motion.
C# compile passed. Actual Unity playback remains to be verified after the Editor imports the new files. The setup checks looping and moving bones beyond the first cycle when it creates the prefab, and logs [IdleTalk].
