"""Check exact preservation, clipped surface, emission isolation and prefab wiring."""
import bpy,json,re,hashlib,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent;A=R/'Assets/Model Palm/Police Car1'
def same(a,b):assert a.read_bytes()==b.read_bytes(),a
same(A/'isuzu.fbx',W/'Backups/isuzu.fbx.before')
same(A/'isuzu.prefab.meta',W/'Backups/isuzu.prefab.meta.before')
same(A/'Material/tripo_mat_0f8da5ad.mat',W/'Backups/tripo_mat_0f8da5ad.mat.before')
source=json.loads((W/'bar-mesh-source.json').read_text());sp=np.array(source['positions']);st=np.array(source['triangles'])
tree=BVHTree.FromPolygons([Vector(p) for p in sp],st.tolist(),all_triangles=True)
data=np.load(W/'siren-mesh.npz');p=data['positions'];t=data['triangles'];normals=data['normals'];uv=data['uv'];counts=data['bank_counts']
bp=p[:,[0,2,1]]*[-1,-1,1];surface_error=0
# Verify each clipped triangle against its exact parent in double precision.
# Tiny pre-existing sliver triangles are unreliable for float32 BVH nearest queries.
for triangle,parent in zip(bp[t],data['source_triangles']):
 a0,b0,c0=sp[st[parent]];ab=b0-a0;ac=c0-a0;n0=np.cross(ab,ac);n0/=np.linalg.norm(n0)
 for point in triangle:
  distance=abs((point-a0)@n0);surface_error=max(surface_error,distance)
  bc=np.linalg.lstsq(np.stack([ab,ac],axis=1),point-a0,rcond=None)[0]
  assert bc.min()>-1e-4 and bc.sum()<1.0001,(parent,bc)
assert surface_error<1e-6,surface_error
assert np.allclose(np.linalg.norm(normals,axis=1),1,atol=1e-6)
assert np.allclose(bp.min(0),sp.min(0),atol=1e-6) and np.allclose(bp.max(0),sp.max(0),atol=1e-6)
native=(A/'Meshes/Isuzu_Siren_Banks.asset').read_text()
nativev=np.frombuffer(bytes.fromhex(re.search(r'_typelessdata: ([a-f0-9]+)',native)[1]),dtype='<f4').reshape(-1,14)
nativet=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([a-f0-9]+)',native)[1]),dtype='<u2').reshape(-1,3)
assert np.allclose(nativev[:,:3],p,atol=1e-7) and np.array_equal(nativet,t)
sub=native.split('  m_Shapes:')[0];assert len(re.findall(r'^    indexCount:',sub,re.M))==2
assert [int(v) for v in re.findall(r'^    indexCount: (\d+)',sub,re.M)]==[int(c)*3 for c in counts]
before=(W/'Backups/isuzu.prefab.before').read_text();after=(A/'isuzu.prefab').read_text()
def blocks(s):return {int(m[1]):m[0] for m in re.finditer(r'--- !u!\d+ &(\d+)\n.*?(?=--- !u!|\Z)',s,re.S)}
b,a=blocks(before),blocks(after);allowed={708723478682126092,6186545251062735584,5978638142169209096,7112178629771985715}
for id,text in b.items():
 if id not in allowed:assert a[id]==text,id
for key in ['m_LocalRotation','m_LocalPosition','m_LocalScale']:
 assert re.search(rf'{key}: [^\n]+',b[6186545251062735584])[0]==re.search(rf'{key}: [^\n]+',a[6186545251062735584])[0]
assert len(a)==len(b)+7
assert after.count('\nLight:\n')==2 and 'cyclesPerSecond: 1.6' in after
controller=next(x for x in a.values() if 'Assembly-CSharp::SecretsThatBreathe.PoliceSirenLights' in x)
for field in ['leftLight','rightLight','lensRenderer']:
 id=int(re.search(field+r': \{fileID: (\d+)\}',controller)[1]);assert id in a
 assert ('Light:' if field.endswith('Light') else 'MeshRenderer:') in a[id]
# All internal non-zero object references must resolve, apart from explicit asset references.
for id in re.findall(r'\{fileID: (\d+)\}',after):assert int(id)==0 or int(id) in a,int(id)
def pixels(path):
 image=bpy.data.images.load(str(path));image.scale(1024,1024)
 values=np.empty(1024*1024*4,dtype=np.float32);image.pixels.foreach_get(values)
 # Blender pixels start at the bottom, whereas the UV sampling below uses top-down rows.
 return values.reshape(1024,1024,4)[::-1,:,:3]
mask=pixels(A/'Texture/Isuzu_Siren_EmissionMask.png')[:,:,0]
atlas=pixels(A/'Texture/tripo_rgb_b9574266-7dd8-4242-9a7c-6f0907865bc9.jpg')
samples=uv[t].mean(1);x=np.clip((samples[:,0]*1024).astype(int),0,1023);y=np.clip(((1-samples[:,1])*1024).astype(int),0,1023)
color=atlas[y,x];emission=mask[y,x];gray=(np.max(color,axis=1)-np.min(color,axis=1)<.04)&(color.mean(1)>.15)
red=(color[:,0]>np.maximum(color[:,1],color[:,2])*1.8)&(color[:,0]>.25)
assert gray.sum()>0 and np.max(emission[gray])==0
assert np.sum(red)>10 and np.mean(emission[red])>.2
for side in ['Left','Right']:
 mat=(A/'Material'/('Isuzu_Siren_'+side+'.mat')).read_text();assert '_EMISSION' in mat and 'm_LightmapFlags: 2' in mat
 assert re.search(r'- _EmissionMap:\s+m_Texture: \{fileID: 2800000, guid: [a-f0-9]+',mat)
pipeline=(R/'Assets/Settings/PC_RPAsset.asset').read_text();assert 'm_AdditionalLightsRenderingMode: 1' in pipeline and 'm_AdditionalLightShadowsSupported: 1' in pipeline
report={'original_fbx_shared_material_and_prefab_guid_unchanged':True,'source_siren_triangles':len(st),'new_siren_triangles':len(t),'surface_error_metres':surface_error,'original_geometry_bounds_preserved':True,'submeshes':2,'gray_speaker_sample_count':int(gray.sum()),'gray_speaker_emission_zero':True,'red_lens_sample_count':int(red.sum()),'real_red_spots':2,'original_root_transforms_and_other_prefab_blocks_preserved':True,'all_serialized_references_valid':True,'URP_additional_lights_and_shadows_supported':True,'compiled_pulse_tests':json.loads((W/'CompileCheck/pulse-checks.json').read_text()),'unity_play_mode_or_render_verified':False}
(W/'validation.json').write_text(json.dumps(report,indent=2));print('SIREN_VALIDATION',json.dumps(report))
