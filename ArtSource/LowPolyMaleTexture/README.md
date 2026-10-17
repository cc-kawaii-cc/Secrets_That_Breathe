# Elder office worker shared texture

Appearance based on the user's elderly Asian man reference: faded warm gray polo, charcoal trousers, gray hair, tan skin and sandals. Built-in ImageGen produced the appearance projection; Blender baked it into the existing mirrored UV map. Final game texture: 512 x 512, sRGB, bilinear filtering, mipmaps, opaque. Shared URP Lit material has metallic 0 and smoothness 0.08.

## Game files
- Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/Textures/ElderOfficeWorker_BaseColor.png
- Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/ElderOfficeWorker.mat

Both LowPolyMale_IdleTalk.fbx.meta and LowPolyMale_SlowWalk.fbx.meta remap their existing Material.001 slot to the shared material. Unity imports these two named models when focused; no project-wide importer was added. Existing prefabs and the story's AuntCharacter inherit the remap.

The FBX binaries, original Blender sources, vertices, UVs, weights, skeletons, clips, controllers, colliders, hierarchy and authored scale were not edited. Each model remains one mesh, 484 vertices, 964 triangles, 22 bones. Original UVs mirror left/right, so the painted breast pocket is mirrored too; clothing thickness and loose silhouettes are represented by the original mesh.

## Evidence
LowPolyMale_Textured.blend is a separate editable textured copy; original walking/idle sources retained. bake_texture.py reproduces the 512px bake from ElderWorker_Projection.png. validate_textured_models.py imports the actual game FBXs and renders their original UVs with the texture. Export-texture-validation.json records the two models, material slot, clips and zero loop seam error. Front, side, back and actual exported poses inspected. Unity Scene playback/material appearance remains to be checked after Editor import.

Image generation method and prompt: built-in image_gen, using projection_template.png as the exact geometry registration target and the user's photograph as appearance reference. See imagegen-prompt.txt.
