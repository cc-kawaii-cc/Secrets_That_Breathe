# Phi Tani (ผีตานี) low-poly character

Static model of the Thai banana-tree ghost, delivered at
`Assets/SecretsThatBreathe/Characters/PhiTani/PhiTani.fbx`. It is **not placed in any scene and has no
scripts, rig or animation** (that was requested for later).

Reference: the user's picture of a green-skinned woman in a green and gold Thai dress among banana trees.

## What it is

- 7 mesh parts, 2,332 triangles, about 1.66 m tall, origin at the soles of the feet, one material, one 512 x 512 atlas.
- Faces -Y in Blender, same export settings as `ThaiPolice` (so it faces the same way in Unity).
- She floats: the hem is 0.17 m above the ground and the bare feet dangle below it.
- A-pose, arms slightly away from the body.

| Object | Contents |
|---|---|
| `PhiTani_Body` | head with painted face, nose, neck, torso, arms, mitten hands with thumbs, dangling legs and feet |
| `PhiTani_Hair` | skull cap with a raised hairline, long back hair, two locks in front of the shoulders |
| `PhiTani_Skirt` | long phaasin skirt, emerald silk with gold diamond lattice and a gold flame-pattern hem |
| `PhiTani_Top` | tube top with gold halter cords |
| `PhiTani_Sabai` | diagonal sash over the left shoulder with a hanging end |
| `PhiTani_Jewelry` | necklace, pendant, two bracelets per wrist, gold waist band |
| `PhiTani_Flower` | red-orange flower above the right ear (the viewer's left) |

The parts are separate so hair, sash or flower can be animated, swapped or hidden later.

## Files

- `PhiTani.fbx`, `PhiTani.mat` (URP Lit, smoothness 0.15, same setup as `ThaiPolice.mat`), `Textures/PhiTani_BaseColor.png`.
  The `.meta` files remap the FBX material to `PhiTani.mat`, with new GUIDs.
- `PhiTani.blend`: editable source.
- `build_phitani.py`: builds the geometry and paints the atlas (needs `bpy` and `Pillow`). Re-running replaces `PhiTani.blend` and the texture.
- `export_fbx.py`, `validate_fbx.py`, `render_preview.py`: export, reimport check, preview renders.
- `build-report.json`, `validation.json`: measurements.
- `preview_*.png`, `PhiTani_overview.png`: renders of the actual model (Cycles, soft studio light).

## Checked

Reimported the delivered FBX in Blender: 7 objects, 2,332 triangles, height 1.6634 m (matches the source), all UVs inside 0-1,
no degenerate faces, one material. Open edges exist where tubes end (hem, wrists, neck, sash edges) because those are surfaces, not solids.

## Not checked

- Not opened in Unity: import, material remap and look under the scene's dark lighting have not been seen.
- No collision, LOD, rig, blend shapes or animation.
- The face is painted on the atlas (eyes, brows, lips), so it has no expressions.
