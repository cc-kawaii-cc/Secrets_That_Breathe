exec(open(__file__.replace('closeup_porsche.py','preview_porsche.py'),encoding='utf-8').read().split("shot('before_front'")[0])
for ob in scene.objects:
    if ob.type=='MESH': ob.hide_render=False
shot('headlight_closeup',(0.65,-4,1.8),(0.65,-1.67,0.69),0.62)
body=bpy.data.objects['Body']
# Wire overlay on actual polygons, retaining texture underneath.
wiremat=bpy.data.materials.new('DiagnosticWire'); wiremat.use_nodes=True
nodes=wiremat.node_tree.nodes; output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL'); em=nodes.new('ShaderNodeEmission'); em.inputs[0].default_value=(0.01,0.05,0.01,1); wiremat.node_tree.links.new(em.outputs[0],output.inputs[0])
idx=len(body.data.materials);body.data.materials.append(wiremat)
mod=body.modifiers.new('DiagnosticWire','WIREFRAME'); mod.thickness=0.0007; mod.use_replace=False; mod.material_offset=idx
body.modifiers.remove(mod)
for mat in body.data.materials:
    if not mat or not mat.use_nodes:continue
    nd=mat.node_tree.nodes;lk=mat.node_tree.links;op=next(n for n in nd if n.type=='OUTPUT_MATERIAL')
    old=op.inputs[0].links[0].from_socket
    wire=nd.new('ShaderNodeWireframe');wire.use_pixel_size=True;wire.inputs['Size'].default_value=1
    dark=nd.new('ShaderNodeEmission');dark.inputs['Color'].default_value=(0,0.16,0,1)
    mix=nd.new('ShaderNodeMixShader');lk.new(wire.outputs[0],mix.inputs[0]);lk.new(old,mix.inputs[1]);lk.new(dark.outputs[0],mix.inputs[2]);lk.new(mix.outputs[0],op.inputs[0])
shot('headlight_wire',(0.65,-4,1.8),(0.65,-1.67,0.69),0.8)
print('PORSCHE_CLOSEUP_COMPLETE')
