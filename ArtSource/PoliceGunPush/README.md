# Police: grip holstered gun, chest shove, release

The existing second-choice threat branch now performs this sequence:

1. Right hand reaches the holstered gun in 0.55 s.
2. Left hand prepares an open-palm push over 0.45 s.
3. One forward shove over 0.30 s, then a 0.12 s contact hold.
4. Left arm recovers over 0.50 s; right hand remains on the grip during the narration.
5. On Talk, the right arm lowers over 0.65 s.

`EveningPoliceEncounter.PushBack` waits for `gestures.PushContactDelay` (1.30 s with these defaults), replacing the old fixed 0.25 s delay. Narration cannot end before the shove finishes, and the release is allowed to complete before returning the camera. The existing player displacement distance, collision handling, dialogue content and camera choreography remain unchanged. No close-contact repositioning or full encounter playthrough was added.

The procedural pose uses the existing Generic rig and arm-overlay restoration from the radio animation. The shove endpoint is clamped to 94% of actual arm length before target interpolation, leaving the elbow slightly bent and preventing the acceleration jump that occurred when hitting the reach limit mid-thrust. The original mitten hands are retained. The handgun stays holstered.

Saved game assets:

- `Assets/SecretsThatBreathe/Characters/ThaiPolice/GunAndPush/Police_GunGrip_ChestPush_AndLower.anim`: 4 s, 60 FPS, non-looping Generic clip, six arm bones only. For standalone use, bind to the same police rig and use an arm mask when layering. The encounter uses the live gesture to permit varying narration lengths.
- `PoliceHolsteredGun.fbx`, `.mat`, `HolsterPalette.png`: 268 triangles, one material, small native palette. Includes belt attachment, holster, retention strap and exposed handgun grip/rear slide.
- Existing `ThaiPolice/IdleTalk/ThaiPolice_IdleTalk.prefab`: holster attached to Hips, with a `PoliceGunGrip` target. Original root, collider, range sensor, radio, IDs and prefab GUID retained.

Editable source: `PoliceHolsteredGun.blend`, `build_holster.py`. Builder/test sources: `Tools/PoliceGunPush/`. The test project reuses `Tools/CompileChecks/PoliceRadioUnity`; its compile-only range dependencies and playback probes are not copied into the main game.

`Unity-validation.txt` records real Animator and IK tests on Standing Conversation and Idle Talk, right wrist contact, preparation before thrust, forward reach without stretching the arm, one shove, recovery, angular continuity and complete release. Actual Play Mode then verifies these stages on the skinned model and checks the radio grab/release still works afterward. `GunPush_*.png` are actual Play Mode frames. `prefab-integrity.json` verifies preservation; the game scripts also compile separately. The open game scene was not saved or replaced during these checks.

In-game trigger: choose the first answer at the second response menu (the branch that focuses the suited man, then returns attention to the officer).
