OfficeGlassDoor - low polygon aluminium double office door

Drag OfficeGlassDoor.prefab into a Unity scene.
Nominal size: 1.84 m wide, 2.20 m tall; outer jamb depth 0.09 m.
Door leaf depth: 0.045 m; glass thickness: 0.006 m; centre clearance: 0.006 m.
Size is an estimate from references, not a measured product specification.

After modifiers / triangulation: 1,004 triangles, three meshes/renderers:
Frame 300, DoorLeft 324, DoorRight 380.
Four shared URP materials: silver aluminium, dark seal, chrome, photo glass.
Glass uses the supplied office image directly with a continuous central crop.
The image is intentionally opaque, not transparent glass or real reflections.
The back shows the reverse side of the same image.

Root pivot is at floor centre. DoorLeft and DoorRight are separate children
with hinge pivots at X -0.86/+0.86, Y 0, Z +0.035 m, identity rotation/scale.
Open toward +Z with DoorLeft local Y -90, DoorRight local Y +90 degrees.
Frame: 3 simple BoxColliders; each moving leaf: 1 BoxCollider.
No automatic interaction/animation controller is included.

UV2 generated for all three Unity meshes. Imported texture top direction,
metre scale, leaf pivots and 0/30/60/90-degree transforms were checked.
Blender mesh manifold/face area checks passed; rendered front, back and open
views checked in Blender and Unity. Source-jamb clearance sampled every 15
degrees, not a full triangle-intersection or physics simulation.
Scene-specific lighting bake and integration with gameplay were not tested.

FBX: Models/OfficeGlassDoor.fbx
Editable source: ArtSource/OfficeGlassDoor/OfficeGlassDoor.blend
Render previews and reports: ArtSource/OfficeGlassDoor
Build sources: Tools/OfficeGlassDoor
