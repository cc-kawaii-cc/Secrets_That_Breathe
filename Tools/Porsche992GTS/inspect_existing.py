import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Porsche992GTS'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/Model Palm/Porsche V.2/PorscheV.2.fbx'))
report=[]
for ob in bpy.context.scene.objects:
    if ob.type=='MESH':
        coords=[ob.matrix_world@Vector(c) for c in ob.bound_box]
        lo=Vector([min(p[i] for p in coords) for i in range(3)]);hi=Vector([max(p[i] for p in coords) for i in range(3)])
        report.append({'name':ob.name,'verts':len(ob.data.vertices),'tris':sum(len(p.vertices)-2 for p in ob.data.polygons),'min':list(lo),'max':list(hi),'materials':[m.name if m else None for m in ob.data.materials]})
(OUT/'existing_inspection.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Existing_Inspection.blend'))
points=[o.matrix_world@Vector(c) for o in bpy.context.scene.objects if o.type=='MESH' for c in o.bound_box]
lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)]);center=(lo+hi)/2;size=max(hi-lo)
for mat in bpy.data.materials:
    mat.use_nodes=True
    p=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if p:
        p.inputs['Base Color'].default_value=(.45,.48,.51,1);p.inputs['Metallic'].default_value=.2;p.inputs['Roughness'].default_value=.4
def aim(ob,p):ob.rotation_euler=(p-ob.location).to_track_quat('-Z','Y').to_euler()
for pos in [(-2,-2,3),(2,1,3)]:
    bpy.ops.object.light_add(type='AREA',location=center+Vector(pos)*size);ob=bpy.context.object;ob.data.energy=180*size**2;ob.data.size=size*2;aim(ob,center)
bpy.ops.object.camera_add(location=center+Vector((1.3,-1.8,.9))*size);cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=size*1.3;aim(cam,center)
scene=bpy.context.scene;scene.camera=cam;scene.world.color=(.3,.3,.3);scene.render.engine='CYCLES';scene.cycles.samples=20;scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.render.filepath=str(OUT/'Existing_Inspection.png');bpy.ops.render.render(write_still=True)
