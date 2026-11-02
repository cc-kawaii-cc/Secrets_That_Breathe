# Evening police encounter

Scene: `Assets/MainScenes/From_PM/MainScene_2_Evening.unity`.

All encounter UI is saved below `SeatingCanvas/PoliceEncounter`.

The first Khem responses are split into `03_lieReply1` / `03_lieReply2` and `04_lawyerReply1` / `04_lawyerReply2` / `04_lawyerReply3`, each with its own PromptText. `Lie Replies` and `Lawyer Replies` on the controller reference these in playback order. The original combined choice captions are unchanged. The scene's `Presentation > Completed Line Hold` is now 3 seconds after each spoken sentence finishes typing (previously 1 second). Change this value on PoliceEncounter to adjust spoken sentence holds throughout this encounter; it does not change Lawyer Room.

- `Lines`: 13 independent spoken sentence sources. Edit their TMP Text Input, font, size and colour. The controller copies that source style into the speaker's presentation when speaking.
- `NarrationLines`: two independent white parenthesized action sentences; displayed instantly at the bottom for 3 seconds.
- `Choices/ChoiceSet1` and `ChoiceSet2`: independent choice menus. Edit each `Choice1/Label` and `Choice2/Label` directly; changing a choice does not change the spoken response. `(โกหก)` uses a red rich-text tag. Menu normal/highlight colours and hover scale live on each `AuntMoneyChoiceUI` component.
- `PoliceDialogueCanvas` / `KhemDialogueCanvas`: edit the speaker name TMP, its colour and the name/body positions. Police name is brown; Khem is blue. These use the saved Lawyer Room layout and the same `FlowingDialogue` implementation.
- `NarrationCanvas`: white bottom narration presentation, without a speaker name.
- `ReturnToCarObjective`: yellow `DDDD00` objective.
- `BoardCarPrompt`: `[F] ขึ้นรถ`.
- `JumpscareAudio`: jumpscare clip and volume.
- `DialogueTypingAudio`: text typing clip and volume.

On `PoliceEncounter`, `EveningPoliceEncounter` exposes turn/zoom timing, field of view, typing speed, completed sentence hold, action narration hold, objective delay, boarding distance and push distance/time. Defaults follow Lawyer Room: turn .18s, zoom .6s, dialogue delay .5s, typing .05s/character, completed hold 1s, camera return .6s. The objective follows control restoration after 1s.

The existing `PoliceJumpscareRange` circle is consumed once after leaving the FD car. Encounter suspends walking/camera control, switches the phone flashlight off, turns both actors to the player, pauses the sunglasses man's talking Animator and restores both actors' original directions/animation afterward. Four combinations of the two choice stages are supported.

`Police Player Talk Controller` on PoliceEncounter references the exact `LowPolyMale_IdleTalk.controller` used by Uncle Pong (`Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk`). On capture, the active police Animator switches from its conversation controller to this swaying Idle Talk. Release restores its original controller, speed and animation time, including F1/F2 reset and disable cleanup. The police prefab and its original conversation controller are unchanged.

`PoliceEncounterGestures` layers two-bone arm movement onto the police's existing talking animation. It uses the active generic rig's UpperArm/Forearm/Hand .L/.R bones. Radio, gun and push targets are adjustable in metres on this component. These are procedural arm gestures, not newly imported animation clips; their final appearance needs Play Mode visual review on the model.

Radio timing: `Radio Raise Seconds` 1.1s and `Radio Lower Seconds` .9s. The narration stays for the raise duration plus Narration Hold Seconds, so it includes a full hold after reaching the radio pose. Choices wait until lowering finishes. `Radio Elbow Direction` is a direction relative to the officer, initially outward/down/forward (-.45, -.8, .15). IK now blends the actual arm rotations with SmoothStep, limits full extension, and stabilizes the wrist with bounded compensation. This replaces the old moving hand target/full-strength elbow solve that could snap the elbow into a different plane at the start of the movement.

After completion, proximity to the existing car exit marker enables F to board. The saved CarExitUI performs the fade. `PorschePassengerRide.BoardForSceneTransition` suspends controls, seats the player under black, and leaves the screen black. No destination Scene has been assigned. `On Ready To Change Scene` is the later transition hook.

Testing checkpoints are repeatable: F1 returns to the stopped car with the exit prompt; F2 skips the Intro and restarts the drive from its beginning. Both cancel the encounter/choices/boarding fade, restore actor and camera ownership, clear the objective, and re-arm the encounter for the next approach. They do not restart the Intro text.

Validation: `Tools/EveningPoliceEncounter/validate.py` checks saved references, UI ancestry, separate choice/source texts, colours/sizes and unchanged original scene objects. Runtime and editor assemblies compile using Unity's Roslyn compiler. No Play Mode or standalone build test was performed for this change.
