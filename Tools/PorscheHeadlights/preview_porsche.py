import bpy, os, json, math
from mathutils import Vector
root=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
out=os.path.join(root,'Tools','PorscheHeadlights')
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'PorscheV2_Inspection.blend'))
texdir=os.path.join(root,'Assets','Model Palm','Porsche V.2','Texture')
for im in bpy.data.images:
    nm=os.path.basename(bpy.path.abspath(im.filepath)).replace('\\','/').split('/')[-1]
    print('IMAGE', im.name, im.filepath, nm)
    candidates=[os.path.join(texdir,nm), os.path.join(texdir,im.name)]
    for p in candidates:
        if os.path.exists(p): im.filepath=p; im.reload(); break
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.render.resolution_x=1000; scene.render.resolution_y=800; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False
if scene.world is None: scene.world=bpy.data.worlds.new('DiagnosticWorld')
scene.world.color=(0.08,0.08,0.08)
scene.view_settings.view_transform='Standard'
# Unlit textured diagnostic keeps original UV/atlas easy to inspect.
for mat in bpy.data.materials:
    if not mat.use_nodes: continue
    nodes=mat.node_tree.nodes
    bs=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None)
    output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL')
    em=nodes.new('ShaderNodeEmission')
    links=mat.node_tree.links
    if bs and bs.inputs['Base Color'].is_linked:
        links.new(bs.inputs['Base Color'].links[0].from_socket, em.inputs['Color'])
    elif bs: em.inputs['Color'].default_value=bs.inputs['Base Color'].default_value
    links.new(em.outputs[0],output.inputs['Surface'])
camdata=bpy.data.cameras.new('DiagnosticCamera'); cam=bpy.data.objects.new('DiagnosticCamera',camdata); scene.collection.objects.link(cam); scene.camera=cam
camdata.type='ORTHO'; camdata.ortho_scale=2.8
def shot(name,eye,target,scale):
    cam.location=eye; cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler(); camdata.ortho_scale=scale
    scene.render.filepath=os.path.join(out,name+'.png'); bpy.ops.render.render(write_still=True)
shot('before_front',(0,-6,2.6),(0,-0.55,0.55),2.75)
shot('before_threequarter',(4,-6,3),(0,0,0.5),5.7)
for ob in scene.objects:
    if ob.type=='MESH': ob.hide_render=ob.name!='Light'
shot('light_only_front',(0,-6,2.6),(0,-0.55,0.55),2.75)
print('PORSCHE_PREVIEW_COMPLETE')
