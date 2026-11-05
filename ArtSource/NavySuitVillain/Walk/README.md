# NavySuitVillain walking model

Walking version of the navy-suit man with black sunglasses (the static `NavySuitVillain.prefab` and
`Conversation/SuitConversation.fbx` are untouched).

* Skin: same 22-bone low-poly rig as the Conversation model (body 964 + sunglasses 108 triangles, 1.85 m).
* Clip `SuitWalk` (`SuitWalk_InPlace` in Blender): 30 fps, frames 1-34, 1.1 s loop, two steps per loop
  (about 109 steps/minute). In place; the character is moved by the scene script.
  Planted feet travel back at about 0.95 m/s, so the prefab should move at that speed to avoid foot sliding.
* Rebuild: `PROJECT_ROOT=<project> OUT_ROOT=<project> python build_suit_walk.py` (needs `bpy`), or open it in Blender.
* Validation (build-report.json): loop seam 1e-17, lowest foot 8e-5 above floor, glasses follow the head (error 9e-7),
  planted-foot slip 7e-7, legs never over-extended.
* Unity: `Assets/SecretsThatBreathe/Characters/NavySuitVillain/Walk/` (FBX, controller and prefab are created by
  `Tools > Secrets That Breathe > Set Up Suit Walk Character`).
* `Preview/`: Blender renders of 8 frames from the side and the front.
