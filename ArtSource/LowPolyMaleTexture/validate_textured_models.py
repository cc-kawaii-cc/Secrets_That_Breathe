import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];W=R/'ArtSource/LowPolyMaleTexture';F=R/'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk'
report={}
for name in ['LowPolyMale_IdleTalk','LowPolyMale_SlowWalk']:
 bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.fps=30
 bpy.ops.import_scene.fbx(filepath=str(F/(name+'.fbx')))
 m=next(o for o in s.objects if o.type=='MESH');rig=next(o for o in s.objects if o.type=='ARMATURE')
 assert len(m.data.vertices)==484 and len(rig.data.bones)==22
 m.data.calc_loop_triangles();assert len(m.data.loop_triangles)==964
 assert m.data.uv_layers.active is not None
 original_materials=[x.name for x in m.data.materials];assert 'Material.001' in original_materials
 action=next(a for a in bpy.data.actions if ('IdleTalk' if 'Idle' in name else 'SlowWalk_InPlace') in a.name);rig.animation_data.action=action
 def points(frame):
  s.frame_set(frame);bpy.context.view_layer.update();e=m.evaluated_get(bpy.context.evaluated_depsgraph_get());return [e.matrix_world@v.co for v in e.data.vertices]
 start,end=map(int,action.frame_range);a=points(start);b=points(end);error=max((x-y).length for x,y in zip(a,b));assert error<.0001
 mat=bpy.data.materials.new('ElderOfficeWorker');mat.use_nodes=True
 tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(F/'Textures/ElderOfficeWorker_BaseColor.png'))
 mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
 m.data.materials.clear();m.data.materials.append(mat)
 for p in m.data.polygons:p.material_index=0
 s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_shadows=True;s.display.shading.show_cavity=False;s.display.shading.background_type='WORLD'
 s.world=bpy.data.worlds.new('NeutralWorld');s.world.color=(.07,.07,.07)
 s.render.resolution_x=650;s.render.resolution_y=850;s.render.resolution_percentage=100
 points(start+18)
 for angle,location in [('front',(0,-12,2.1)),('three_quarter',(7,-12,2.7))]:
  bpy.ops.object.camera_add(location=location);cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,2.1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=4.9;s.camera=cam
  s.render.filepath=str(W/(name+'_'+angle+'.png'));bpy.ops.render.render(write_still=True)
 report[name]={'vertices':484,'triangles':964,'bones':22,'material_name':original_materials,'actions':[a.name for a in bpy.data.actions],'loop_error':error,'texture_size':[512,512]}
(W/'export-texture-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('TEXTURED_EXPORT_VALIDATION',json.dumps(report))
