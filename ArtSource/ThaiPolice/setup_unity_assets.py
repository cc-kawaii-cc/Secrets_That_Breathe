"""Create only this new character's Unity material and importer metadata."""
from pathlib import Path
import uuid,re,json
R=Path(__file__).resolve().parents[2];W=Path(__file__).resolve().parent
OUT=R/'Assets/SecretsThatBreathe/Characters/ThaiPolice'
def guid(path):
 meta=Path(str(path)+'.meta')
 if meta.exists():return re.search(r'^guid: ([a-f0-9]+)',meta.read_text(),re.M)[1]
 return uuid.uuid5(uuid.NAMESPACE_URL,'SecretsThatBreathe/ThaiPolice/'+str(path.relative_to(R))).hex
def foldermeta(path):
 meta=Path(str(path)+'.meta')
 if not meta.exists():meta.write_text(f'fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
foldermeta(OUT);foldermeta(OUT/'Textures')
texture=OUT/'Textures/ThaiPolice_BaseColor.png';textureguid=guid(texture)
tm=(R/'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/Textures/ElderOfficeWorker_BaseColor.png.meta').read_text()
tm=re.sub(r'^guid: [a-f0-9]+',f'guid: {textureguid}',tm,flags=re.M)
Path(str(texture)+'.meta').write_text(tm)
material=OUT/'ThaiPolice.mat';matguid=guid(material)
mt=(R/'Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/ElderOfficeWorker.mat').read_text().replace('ElderOfficeWorker','ThaiPolice').replace('37223d374bff4b9b87e55aae5df8f054',textureguid)
mt=re.sub(r'(_Smoothness: )[^\n]+',r'\g<1>0.15',mt);mt=re.sub(r'(_Glossiness: )[^\n]+',r'\g<1>0.15',mt)
mt=re.sub(r'(- _(BaseColor|Color): )\{[^\n]+\}',r'\g<1>{r: 1, g: 1, b: 1, a: 1}',mt)
material.write_text(mt)
Path(str(material)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {matguid}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
model=OUT/'ThaiPolice_LowPoly.fbx';modelguid=guid(model)
meta=(R/'Assets/Nemo_Upgrade/low-poly-male-base-mesh/source/lowpoly_male_base_mesh-TPOSE.fbx.meta').read_text()
meta=re.sub(r'^guid: [a-f0-9]+',f'guid: {modelguid}',meta,flags=re.M)
meta=meta.replace('  externalObjects: {}',f'  externalObjects:\n  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: ThaiPolice\n    second: {{fileID: 2100000, guid: {matguid}, type: 2}}',1)
for field,value in [('importAnimation',0),('animationType',0),('importCameras',0),('importLights',0),('importPhysicalCameras',0)]:
 meta=re.sub(rf'(^  +{field}: )[^\n]+',rf'\g<1>{value}',meta,flags=re.M)
Path(str(model)+'.meta').write_text(meta)
(W/'unity-assets.json').write_text(json.dumps({'model_guid':modelguid,'material_guid':matguid,'texture_guid':textureguid,'shader':'Universal Render Pipeline/Lit','base_color_white':True,'smoothness':.15},indent=2))
print('UNITY_ASSETS_READY',model.relative_to(R))
