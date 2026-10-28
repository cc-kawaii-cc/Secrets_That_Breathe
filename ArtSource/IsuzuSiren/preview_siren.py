"""Render the actual modified lightbar and red-light spill, offline in Blender."""
import bpy,numpy as np,json,math
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;A=R/'Assets/Model Palm/Police Car1'
bpy.ops.wm.open_mainfile(filepath=str(W/'Isuzu_Inspection.blend'))
d=np.load(W/'siren-mesh.npz');p=d['positions'][:,[0,2,1]]*[-1,-1,1];normal=d['normals'][:,[0,2,1]]*[-1,-1,1];t=d['triangles'][:,[0,2,1]];uv=d['uv'];counts=d['bank_counts']
bar=bpy.data.objects['police'];me=bpy.data.meshes.new('Isuzu_Siren_Banks_Proof');me.from_pydata(p.tolist(),[],t.tolist());me.update();layer=me.uv_layers.new(name='UVMap')
for f in me.polygons:
 f.use_smooth=True
 for li,vi in zip(f.loop_indices,f.vertices):layer.data[li].uv=uv[vi]
me.normals_split_custom_set([normal[me.loops[li].vertex_index] for li in range(len(me.loops))])
bar.data=me;bar.matrix_world=Matrix.Identity(4)
atlas=bpy.data.images.load(str(A/'Texture/tripo_rgb_b9574266-7dd8-4242-9a7c-6f0907865bc9.jpg'))
mask=bpy.data.images.load(str(A/'Texture/Isuzu_Siren_EmissionMask.png'))
materials=[];emitters=[]
for bank in ['Left','Right']:
 mat=bpy.data.materials.new('Siren_Proof_'+bank);mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.6
 tex=nodes.new('ShaderNodeTexImage');tex.image=atlas;links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 em=nodes.new('ShaderNodeTexImage');em.image=mask
 mult=nodes.new('ShaderNodeMixRGB');mult.blend_type='MULTIPLY';mult.inputs[0].default_value=1;links.new(em.outputs['Color'],mult.inputs[1]);mult.inputs[2].default_value=(1,.0036,.0011,1)
 links.new(mult.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=7
 me.materials.append(mat);materials.append(mat);emitters.append(bs)
for f in me.polygons:f.material_index=0 if f.index<int(counts[0]) else 1
s=bpy.context.scene;s.render.resolution_x=1100;s.render.resolution_y=800;s.render.resolution_percentage=100;s.view_settings.view_transform='Standard';s.view_settings.look='None'
center=Vector((-.04,-.104,1.63))
def camera(eye,target,scale):
 bpy.ops.object.camera_add(location=eye);c=bpy.context.object;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=scale;s.camera=c
s.render.engine='CYCLES';s.cycles.samples=16;s.cycles.use_denoising=True
s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.35,.45,1);s.world.node_tree.nodes['Background'].inputs[1].default_value=.10
# Close view with neutral fill, no fake bloom: check the grey centre remains non-emissive.
bpy.ops.object.light_add(type='AREA',location=(1,-2,3));fill=bpy.context.object;fill.data.energy=60;fill.data.size=3
fill.rotation_euler=(center-fill.location).to_track_quat('-Z','Y').to_euler()
for o in s.objects:
 if o.type=='MESH':o.hide_render=o!=bar
camera(center+Vector((1,-1.6,.85)),center,1.2)
for bank in [0,1]:
 for i,bs in enumerate(emitters):bs.inputs['Emission Strength'].default_value=7 if i==bank else .175
 s.render.filepath=str(W/('after_bar_'+('left' if bank==0 else 'right')+'.png'));bpy.ops.render.render(write_still=True)
for o in s.objects:
 if o.type=='MESH':o.hide_render=False
# Floor/wall are proof-only objects, never exported into the game prefab.
mat=bpy.data.materials.new('Proof_Asphalt');mat.diffuse_color=(.14,.14,.14,1);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.14,.14,.14,1);bs.inputs['Roughness'].default_value=.95
bpy.ops.mesh.primitive_plane_add(size=22,location=(0,0,-.015));ground=bpy.context.object;ground.name='ProofOnly_Ground';ground.data.materials.append(mat)
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,3.5,1.4));wall=bpy.context.object;wall.name='ProofOnly_Wall';wall.scale=(8,.15,2.8);wall.data.materials.append(mat)
fill.data.energy=140;s.world.node_tree.nodes['Background'].inputs[1].default_value=.018
locations=json.loads((W/'build-report.json').read_text())['light_locations_unity'];lamps=[]
for pos in locations:
 bpy.ops.object.light_add(type='SPOT',location=(-pos[0],-pos[2],pos[1]));l=bpy.context.object;l.data.color=(1,.015,.005);l.data.energy=450;l.data.spot_size=math.radians(100);l.data.spot_blend=.45;l.data.shadow_soft_size=.04;lamps.append(l)
camera((7,-9,5),(0,0,.75),8)
for phase in [0,.25]:
 for i,l in enumerate(lamps):
  a=2*math.pi*phase+i*math.pi;direction=Vector((-math.sin(a)*math.cos(math.radians(18)),-math.cos(a)*math.cos(math.radians(18)),-math.sin(math.radians(18))))
  l.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
  pulse=max(0,math.cos((phase+i*.5)*2*math.pi))**4;emitters[i].inputs['Emission Strength'].default_value=7*(.025+.975*pulse)
 s.render.filepath=str(W/('night_spill_'+str(int(phase*100))+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(W/'Isuzu_Siren_Proof.blend'))
print('SIREN_PREVIEWS_COMPLETE')
