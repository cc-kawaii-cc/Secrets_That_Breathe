# Isuzu police lightbar: emissive red banks and real sweeping lights

Updated existing prefab: `Assets/Model Palm/Police Car1/isuzu.prefab`.
The existing instance in `Assets/MainScenes/From_PM/MainScene_2_Evening.unity`
references this prefab and has only name/placement/rotation overrides, so it inherits
the new components and bindings without saving or editing the scene.

## Behaviour and controls

The root has a new `Police Siren Lights` component. It starts enabled in Play Mode.
`Siren On` switches both material emission and the two real red spotlights.
`Pattern` defaults to `Rotating Beacon`; `Alternating Double Flash` is also available.
Rotation/flash speed is `Cycles Per Second` (1.6 default).
`Emission Color` controls HDR lens brightness. `Light Intensity` (32 default),
`Light Range` (11 m), `Spot Angle` (100 degrees) and `Downward Tilt` (18 degrees)
configure the real lights when the component enables. Set those before entering Play
or disable/re-enable the component after changing light configuration at runtime.
`SetSirenEnabled(bool)` / `SirenOn` provide code control.

The two spotlights are above the lens cover with soft shadows enabled and rotate in
opposite phases. Their shadow near plane is 0.03 m to reduce near-car light leaks.
They illuminate real scene surfaces; emission alone is not used as a light source.
There is no volumetric beam or audio. Bloom/glow appearance depends on the existing
camera/volume settings; no scene volume, lighting or exposure setting was modified.

Controller: `Assets/SecretsThatBreathe/Scripts/PoliceSirenLights.cs`.
The script changes per-material-index MaterialPropertyBlocks; shared materials are
not mutated or instantiated every frame. Lights and emission switch off when the
component disables. Updates allocate no new material/property-block objects.

## Asset changes and preservation

- `Meshes/Isuzu_Siren_Banks.asset`: copied lightbar surface, separated into two material
  slots by clipping triangles at the bank centre. Original UVs/corner normals are interpolated.
- `Material/Isuzu_Siren_Left.mat`, `Isuzu_Siren_Right.mat`: URP Lit, original atlas,
  red HDR emission and an emission mask. Opaque red lens appearance is retained.
- `Texture/Isuzu_Siren_EmissionMask.png`: 1024px mask derived from red source atlas texels.
  Grey centre/speaker and black housing/supports do not get lens emission.
- Two new children `Siren_Red_Left`, `Siren_Red_Right`, with real Spot Lights.
- New controller on the existing root; original lightbar MeshFilter/Renderer bindings updated.

Original FBX, shared car material, prefab GUID, names, root placement/rotation/scale,
all original child transforms, wheel meshes and other original prefab blocks are unchanged.
No geometry is added to the car body. The private lightbar copy has 412 triangles
instead of 328, adding only 84 triangles to permit independently lit banks.
Original complete car: 36,830 triangles; current prefab: 36,914 triangles.
Adding emission to the shared original material would have illuminated unrelated car
parts; this implementation instead binds new materials only to the lightbar copy.

`Backups/` contains the exact original prefab, source FBX, prefab metadata and shared material.
The Unity source FBX is deliberately retained: the prefab uses the private lightbar
mesh for siren operation. Drag the existing `isuzu.prefab`, not the raw FBX, to use it.

## Reproduction and checks

- `inspect_isuzu.py`: read original FBX and material bindings; geometry and before renders.
- `build_siren.py`: derive mask, clip banks, write native mesh/materials and patch only owned prefab blocks.
- `validate_siren.py`: checks exact preservation, surface, bindings, red/grey emission samples and URP flags.
- `preview_siren.py`: actual clipped geometry/atlas preview and a Blender night-light proof scene.
- `Isuzu_Inspection.blend`, `Isuzu_Siren_Proof.blend`: inspection/proof sources. Proof floor/wall are not game assets.
- `after_bar_left/right.png`: actual lens isolation, with grey centre retained.
- `night_spill_0/25.png`: offline Blender proof of rotating real-light spill and car shadows.
- `CompileCheck/PulseChecks.cs`: calls the actual compiled controller's public timing function.
- `CompileCheck/pulse-checks.json`, `build-report.json`, `validation.json`: measured results.

Validated against the original 328 triangles using exact parent planes/barycentric
coordinates: clipped surface error below 0.000001 m, same bounds. Native mesh buffers,
both submesh ranges and normalized normals checked. All original non-owned prefab
blocks compare exactly; all internal references resolve. Grey centre samples have
zero emission and red lens samples have emission. The current PC URP asset supports
additional pixel lights and additional-light shadows; its settings were not changed.

Compiled the new controller with the installed Unity 6000.3.8f1 runtime assemblies:
zero compile errors (ordinary serialized-field warnings only). Ran 2,000 timing samples
against that actual compiled code: both patterns periodic, bounded, alternating; double
flash has 210 lit samples per bank per 1,000-sample cycle.

## Native resource lifecycle fix

The first live user run exposed Unity's `CreateImpl is not allowed ... MonoBehaviour
constructor (or instance field initializer)` error. The two MaterialPropertyBlock
field initializers failed during deserialization; subsequent GetPropertyBlock(null)
exceptions aborted ApplyFrame before it reached the real-light rotation code.
Moved native block allocation to OnEnable, removed readonly field initializers,
and made the emission setter tolerate absent blocks during disable/reload.

Verified the fix using Unity 6000.3.8f1 in a separate batch validation project with
the actual updated prefab and controller. All 16 native checks passed, including
prefab deserialization, actual Renderer property blocks, actual Light transform
rotation, emission bank alternation, real-light double flashes, off/disable, and
native-cache loss/recreation. Result: `unity-isolated-regression.json`;
log: `isolated-unity-host.log`. The isolated project used -nographics and was closed.
The user's open editor and Play state were not controlled.
`Assets/Editor/PoliceSirenRegressionCheck.cs` is an opt-in one-shot preview-scene
regression; it performs no check without the exact request marker.

Not verified: rendered Play Mode appearance, actual shadow-map selection, per-object
light selection and final brightness in the user's main scene. Blender proofs use
comparable lights but are not Unity screenshots and do not establish exact brightness.
