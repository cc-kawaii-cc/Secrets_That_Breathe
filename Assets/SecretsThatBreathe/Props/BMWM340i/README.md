# BMW M340i Low Poly

Stylized exterior model built from the eight supplied reference images. Silver body, kidney grilles, dual LED signatures, L-shaped rear lamps, split-spoke wheels, red brake calipers, mirrors, sunroof, rear diffuser and paired exhaust outlets.

- 13,302 triangles in the imported Unity model (13,366 in the Blender source before Unity removes degenerate faces), including all four wheels.
- Five mesh objects: Body plus four wheels with axle-centred origins.
- Metre scale, ground-level root pivot.
- Unity prefab uses URP Lit materials, secondary lightmap UVs and two simple body colliders.
- Exterior only: opaque tinted windows, no cabin, opening panels, rig or driving controller.
- Body paint can be changed in Materials/SilverPaint.mat.

Drag BMW_M340i_LowPoly.prefab into a scene. For vehicle physics, add a Rigidbody and WheelColliders in your vehicle controller setup; these are not included in this static prop prefab. Wheel meshes contain calipers for visual simplicity, so calipers rotate together with the wheels.

Editable Blender source, portable GLB and front/rear previews are in ArtSource/BMWM340i at the project root. Rebuild model geometry with Tools/BMWM340i/build_bmw.py in Blender; rebuild Unity assets using Tools > Secrets That Breathe > Build BMW M340i.

This is a simplified artistic interpretation of the supplied images, not a manufacturer CAD model.
