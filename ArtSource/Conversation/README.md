# Police and suit conversation

Two complementary eight-second, 30 FPS loops. Police gestures mainly with the right hand in the first half; the suit responds mainly with the left hand in the second half. Both have small listening nods and body movement, planted feet, and no root travel. Sunglasses are skinned to Head. No speech audio or lip animation is included.

## Unity use

Drag `Assets/SecretsThatBreathe/Characters/Conversation/PoliceAndSuit_Conversation.prefab` into a scene for the two actors facing each other, 1.4 metres apart. The Animator controllers start automatically and loop together. Each actor can also be used separately.

Existing `ThaiPolice/IdleTalk/ThaiPolice_IdleTalk.prefab` and `NavySuitVillain/NavySuitVillain.prefab` now use the animated models. Original roots, transforms, components, child objects and serialized IDs are retained; original renderers and animators are disabled. The new `ConversationModel` child contains the animated model and size adjustment. Backups of the original two prefabs and their metas are in `Backups/`.

Heights at root scale 1: police 1.80 m, suit 1.85 m. The police prefab's existing root scale 1.05 is preserved. The pair uses root scale 1 for both. Visible triangles: police 964; suit 1072 including sunglasses. Texture and material assets are unchanged.

## Authoring and checks

- `build_conversation.py`: creates editable `PoliceConversation.blend` / `SuitConversation.blend`, actions and animated FBX files.
- `preview_conversation.py`: creates the paired Blender preview scene and six pose renders.
- `ConversationAssetBuild.cs`: Editor-only builder used in the isolated `Tools/CompileChecks/ConversationUnity` project; this file is outside game Assets.
- `build-report.json`: authored loop, feet and glasses attachment measurements.
- `unity-build-report.json`: Unity 6000.3.8f1 imported FBX tests, valid avatars, real Animator controller playback, world-space linear skinning, duration, loop seam, height, no root motion, triangle counts and Head attachment.
- `prefab-integrity.json`: original prefab IDs, GUIDs and root transforms retained.

Unity tests ran in batch mode without graphics; main-scene visual Play Mode lighting was not reviewed. Blender previews show source geometry and textures. Existing open game scenes were not saved or changed by the builder.
