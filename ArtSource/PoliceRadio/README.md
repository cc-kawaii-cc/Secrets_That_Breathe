# Police radio: grab, hold, lower

The existing encounter now raises the officer's left hand to a visible radio attached to the Chest bone, holds contact for the narration, then lowers the hand back into the talking animation. The scene's existing Radio / Talk calls and raise 1.1 s / lower 0.9 s timings are retained. The torso, feet, controller selection, UI and scene file are unchanged.

`PoliceEncounterGestures` locates `PoliceRadioGrip` on the active police model. It restores its previous arm overlay before the next Animator evaluation, including constant bone curves that Unity may skip. This prevents the arm remaining up when the radio gesture ends. The wrist rotates to point the mitten fingers up along the radio; there are no new finger bones.

Assets:

- Updated `Assets/SecretsThatBreathe/Characters/ThaiPolice/IdleTalk/ThaiPolice_IdleTalk.prefab`: existing root, range component, collider, GUID and serialized IDs retained; new `PoliceShoulderRadio` under Chest.
- `Assets/SecretsThatBreathe/Characters/ThaiPolice/Radio/Police_Radio_GrabAndLower.anim`: 4 s, 60 FPS, non-looping Generic left-arm animation. Contains only UpperArm.L / Forearm.L / Hand.L rotation curves. Use this exported clip on the same rig, with an arm mask if layering over another animation. The encounter itself uses the live IK gesture so it can hold for varying dialogue lengths.
- `PoliceRadio.fbx`, `PoliceRadio.mat`, `RadioPalette.png`: 320 triangles, one shared material, eight-pixel color palette. Antenna, grille, display, PTT button and belt clip are geometry. No emission or scripts on the prop.
- Editable prop source: `PoliceRadio.blend`, `build_radio.py`.

Verification: `Unity-validation.txt` records both Standing Conversation and Idle Talk checks at 60 FPS, wrist contact, elbow direction, angular continuity, complete release, unchanged head and root. A separate actual Play Mode test captured the skinned model after LateUpdate at rest, raising, hold, lowering and rest, and confirmed both contact and release. `Radio_*.png` and `Radio_Side.png` are these real Unity captures. `prefab-integrity.json` verifies preservation of the original prefab root and components. The full game scripts were compiled separately without replacing Unity's live assemblies.

The isolated verifier is `Tools/PoliceRadio/BuildAndVerify.cs` plus `PoliceRadioPlaybackProbe.cs`. Its range-sensor dependencies are compile-only test substitutes and are not installed in the main game. No main scene was opened, saved or changed by the validation project.
