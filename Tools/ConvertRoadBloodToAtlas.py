"""Bake existing native blood geometry/material into an atlas and 2-triangle Prefabs.

This is a deterministic mesh/material bake, not AI generation or a photo edit.
Original assets are backed up outside Assets; existing Prefab/mesh GUIDs survive.
"""
from pathlib import Path
import datetime
import json
import re
import shutil
import struct
import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parent.parent
ROOT = PROJECT / "Assets/SecretsThatBreathe/Props/RoadBlood"
WORK = PROJECT / "Tools/RoadBlood"
SIZE = 1024
ATLAS_GUID = "f1c3b947bfa2417aa42ba357e9668db6"
MATERIAL_GUID = "42b9e7dc8f3d4e42a683c3d18fb7aec8"
OLD_MATERIAL_GUID = "d671e43c82a2497ea9c52e03a9bde144"
TILES = {"RoadBlood_Pool": (0, 0, 512, 512),
         "RoadBlood_Drops": (512, 0, 512, 512),
         "RoadBlood_Smear": (0, 512, 1024, 256)}

def write(path, content):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")

def meta(path, guid, importer, file_id=None):
    extra = f"  mainObjectFileID: {file_id}\n" if file_id else ""
    write(Path(str(path)+".meta"), f"fileFormatVersion: 2\nguid: {guid}\n{importer}:\n  externalObjects: {{}}\n{extra}  userData:\n  assetBundleName:\n  assetBundleVariant:\n")

def read_mesh(path):
    text = path.read_text(encoding="utf-8")
    count = int(re.search(r"m_VertexCount: (\d+)", text)[1])
    data = bytes.fromhex(re.search(r"_typelessdata: ([0-9a-f]+)", text)[1])
    dtype = np.dtype([("p", "<f4", 3), ("n", "<f4", 3), ("c", "u1", 4), ("uv", "<f4", 2)])
    assert len(data) == count*dtype.itemsize and dtype.itemsize == 36
    vertices = np.frombuffer(data, dtype=dtype)
    indices = np.frombuffer(bytes.fromhex(re.search(r"m_IndexBuffer: ([0-9a-f]+)", text)[1]), dtype="<u2").reshape(-1, 3)
    assert count > 4, "Already converted. Use the archived source meshes to rebuild."
    return text, vertices, indices

def fract(x): return x-np.floor(x)
def hash2(x, z): return fract(np.sin(x*127.1+z*311.7)*43758.5453)
def noise(x, z):
    ix, iz = np.floor(x), np.floor(z)
    fx, fz = fract(x), fract(z)
    fx, fz = fx*fx*(3-2*fx), fz*fz*(3-2*fz)
    low = hash2(ix, iz)*(1-fx)+hash2(ix+1, iz)*fx
    high = hash2(ix, iz+1)*(1-fx)+hash2(ix+1, iz+1)*fx
    return low*(1-fz)+high*fz

def srgb(rgb):
    return np.where(rgb <= .0031308, rgb*12.92, 1.055*np.power(np.maximum(rgb, 0), 1/2.4)-.055)

source_material = (ROOT/"Materials/RoadBlood_DarkRed.mat").read_text(encoding="utf-8")
def color(name):
    match = re.search(re.escape(name)+r": \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}", source_material)
    assert match, name
    return np.array([float(match[i]) for i in range(1, 4)])
def scalar(name): return float(re.search(re.escape(name)+r": ([\d.]+)", source_material)[1])
thin, dense = color("_BloodColor"), color("_DenseColor")
opacity, patchiness, detail_scale = scalar("_Opacity"), scalar("_Patchiness"), scalar("_NoiseScale")

sources = {name: read_mesh(ROOT/f"Meshes/{name}.asset") for name in TILES}
backup = WORK/("BeforeAtlas-"+datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
backup.mkdir(parents=True, exist_ok=False)
for sub in ("Meshes", "Prefabs", "Materials"):
    shutil.copytree(ROOT/sub, backup/sub)
shutil.copytree(ROOT/"Shaders", backup/"Shaders")

premult = np.zeros((SIZE, SIZE, 3), dtype=np.float32)
alpha = np.zeros((SIZE, SIZE), dtype=np.float32)
mesh_records = []
converted_meshes = {}
for name, (text, vertices, triangles) in sources.items():
    x0, y0, w, h = TILES[name]
    positions = vertices["p"].astype(np.float64)
    lo, hi = positions.min(axis=0), positions.max(axis=0)
    # Transparent gutters around every image protect the edge when mipmapping.
    margin = (hi-lo)*.035
    lo[[0, 2]] -= margin[[0, 2]]
    hi[[0, 2]] += margin[[0, 2]]
    projected = np.column_stack((x0+(positions[:, 0]-lo[0])/(hi[0]-lo[0])*(w-1),
                                 y0+(hi[2]-positions[:, 2])/(hi[2]-lo[2])*(h-1)))
    for triangle in triangles:
        points = projected[triangle]
        xmin, ymin = np.maximum(np.floor(points.min(axis=0)).astype(int), (x0, y0))
        xmax, ymax = np.minimum(np.ceil(points.max(axis=0)).astype(int), (x0+w-1, y0+h-1))
        if xmax < xmin or ymax < ymin: continue
        xx, yy = np.meshgrid(np.arange(xmin, xmax+1)+.5, np.arange(ymin, ymax+1)+.5)
        a, b, c = points
        denominator = (b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(denominator) < 1e-12: continue
        wa = ((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/denominator
        wb = ((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/denominator
        wc = 1-wa-wb
        inside = (wa >= 0) & (wb >= 0) & (wc >= 0)
        if not inside.any(): continue
        weights = np.stack((wa, wb, wc), axis=-1)
        values = np.einsum("...i,ij->...j", weights, vertices["c"][triangle].astype(np.float32)/255)
        local = np.einsum("...i,ij->...j", weights, positions[triangle])
        detail = .65*noise(local[..., 0]*detail_scale, local[..., 2]*detail_scale)
        detail += .35*noise(local[..., 0]*detail_scale*2.17, local[..., 2]*detail_scale*2.17)
        coverage = np.clip(values[..., 3]*opacity, 0, 1)*((1-patchiness)+patchiness*(.55+detail*.45))
        coverage *= inside
        density = np.clip(values[..., 0]*.75+detail*.25, 0, 1)
        tint = thin[None, None, :]*(1-density[..., None])+dense[None, None, :]*density[..., None]
        region = np.s_[ymin:ymax+1, xmin:xmax+1]
        premult[region] = tint*coverage[..., None]+premult[region]*(1-coverage[..., None])
        alpha[region] = coverage+alpha[region]*(1-coverage)

    # Native four-vertex horizontal Quad. Existing mesh GUID and footprint survive.
    u0, u1 = (x0+.5)/SIZE, (x0+w-.5)/SIZE
    v0, v1 = (SIZE-y0-h+.5)/SIZE, (SIZE-y0-.5)/SIZE
    y = float(positions[0, 1])
    quad_positions = ((lo[0], y, lo[2]), (lo[0], y, hi[2]), (hi[0], y, hi[2]), (hi[0], y, lo[2]))
    quad_uvs = ((u0, v0), (u0, v1), (u1, v1), (u1, v0))
    packed = b"".join(struct.pack("<3f3f4B2f", *pos, 0, 1, 0, 255, 255, 255, 255, *uv) for pos, uv in zip(quad_positions, quad_uvs))
    indices = struct.pack("<6H", 0, 1, 2, 0, 2, 3)
    quad = re.sub(r"(?m)^    indexCount:.*$", "    indexCount: 6", text)
    quad = re.sub(r"(?m)^    vertexCount:.*$", "    vertexCount: 4", quad)
    quad = re.sub(r"m_VertexCount: \d+", "m_VertexCount: 4", quad)
    quad = re.sub(r"m_DataSize: \d+", f"m_DataSize: {len(packed)}", quad)
    quad = re.sub(r"(?m)^  m_IndexBuffer:.*$", "  m_IndexBuffer: "+indices.hex(), quad)
    quad = re.sub(r"(?m)^    _typelessdata:.*$", "    _typelessdata: "+packed.hex(), quad)
    center, extent = (lo+hi)/2, np.maximum((hi-lo)/2, .003)
    for key, value in (("m_Center", center), ("m_Extent", extent)):
        vector = "{x: %.8f, y: %.8f, z: %.8f}" % tuple(value)
        quad = re.sub(r"(?m)^(\s*"+key+r":) [^\n]+", r"\1 "+vector, quad)
    converted_meshes[name] = quad
    mesh_records.append({"name": name, "beforeTriangles": len(triangles), "afterTriangles": 2,
                         "vertices": 4, "atlasTilePixels": TILES[name]})

rgb = np.broadcast_to((thin+dense)/2, premult.shape).copy()
np.divide(premult, alpha[..., None], out=rgb, where=alpha[..., None] > 1e-6)
encoded = np.round(np.clip(srgb(rgb), 0, 1)*255).astype(np.uint8)
rgba = np.dstack((encoded, np.round(np.clip(alpha, 0, 1)*255).astype(np.uint8)))
atlas_path = ROOT/"Textures/RoadBlood_Atlas.png"
atlas_path.parent.mkdir(parents=True, exist_ok=True)
Image.fromarray(rgba).save(atlas_path, optimize=True)

# Start from the project's native TextureImporter schema; change relevant options.
texture_meta = (PROJECT/"Assets/SecretsThatBreathe/Lighting/Cookies/Porsche_LowBeam_LHT.png.meta").read_text(encoding="utf-8")
texture_meta = re.sub(r"(?m)^guid:.*$", "guid: "+ATLAS_GUID, texture_meta)
for key, value in {"enableMipMap": 1, "sRGBTexture": 1, "alphaIsTransparency": 1, "aniso": 2,
                   "maxTextureSize": SIZE, "textureCompression": 1}.items():
    texture_meta = re.sub(r"(?m)^(\s*"+key+r":) [^\n]+", r"\g<1> "+str(value), texture_meta)
pattern = r"(?s)(  - serializedVersion: 4\n    buildTarget: Standalone\n).*?(?=  spriteSheet:|  - serializedVersion:|\Z)"
texture_meta = re.sub(pattern, r"\1"+f"    maxTextureSize: {SIZE}\n    resizeAlgorithm: 0\n    textureFormat: 25\n    textureCompression: 1\n    compressionQuality: 50\n    crunchedCompression: 0\n    allowsAlphaSplitting: 0\n    overridden: 1\n    ignorePlatformSupport: 0\n    androidETC2FallbackOverride: 0\n    forceMaximumCompressionQuality_BC6H_BC7: 0\n", texture_meta)
write(Path(str(atlas_path)+".meta"), texture_meta)

atlas_material = re.sub(r"(?m)^  m_Name:.*$", "  m_Name: RoadBlood_Atlas", source_material)
atlas_material = atlas_material.replace("guid: 8d621fd11da24b94b8c3173de45a2abc", "guid: 644c540e98c94a22846590c4fa3d45df")
atlas_material = re.sub(r"(?s)    m_TexEnvs:.*?(?=    m_Ints:)", f"    m_TexEnvs:\n    - _BaseMap:\n        m_Texture: {{fileID: 2800000, guid: {ATLAS_GUID}, type: 3}}\n        m_Scale: {{x: 1, y: 1}}\n        m_Offset: {{x: 0, y: 0}}\n", atlas_material)
atlas_material = re.sub(r"(?s)    m_Floats:.*?(?=  m_BuildTextureStacks:)", "    m_Floats:\n    - _Opacity: 1\n    m_Colors:\n    - _BaseColor: {r: 1, g: 1, b: 1, a: 1}\n", atlas_material)
material_path = ROOT/"Materials/RoadBlood_Atlas.mat"
write(material_path, atlas_material)
meta(material_path, MATERIAL_GUID, "NativeFormatImporter", 2100000)
for name, quad in converted_meshes.items():
    write(ROOT/f"Meshes/{name}.asset", quad)
    prefab_path = ROOT/f"Prefabs/{name}.prefab"
    prefab = prefab_path.read_text(encoding="utf-8")
    assert prefab.count(OLD_MATERIAL_GUID) == 1
    write(prefab_path, prefab.replace(OLD_MATERIAL_GUID, MATERIAL_GUID))

# QA composite is a render of the baked assets over neutral asphalt, not a game screenshot.
background = np.full_like(rgb, .09)
composite = premult+background*(1-alpha[..., None])
Image.fromarray(np.round(np.clip(srgb(composite), 0, 1)*255).astype(np.uint8)).save(WORK/"Atlas-on-gray-preview.png")
for name, quad in converted_meshes.items():
    assert int(re.search(r"m_VertexCount: (\d+)", quad)[1]) == 4
    assert int(re.search(r"(?m)^    indexCount: (\d+)", quad)[1]) == 6
    x0, y0, w, h = TILES[name]
    tile_alpha = alpha[y0:y0+h, x0:x0+w]
    assert tile_alpha.max() > .5 and np.count_nonzero(tile_alpha > .01) > 100
    assert not (tile_alpha[0].any() or tile_alpha[-1].any() or tile_alpha[:, 0].any() or tile_alpha[:, -1].any())
report = {"success": True, "backup": str(backup), "atlas": str(atlas_path), "resolution": [SIZE, SIZE],
          "pngBytes": atlas_path.stat().st_size, "windowsFormat": "BC7 RGBA + mipmaps",
          "estimatedAtlasGPUMemoryBytes": round(SIZE*SIZE*4/3), "meshes": mesh_records,
          "totalBeforeTriangles": sum(x["beforeTriangles"] for x in mesh_records), "totalAfterTriangles": 6,
          "preservedPrefabAndMeshGuids": True, "preservedSourceMaterialColors": {"thinLinear": thin.tolist(), "denseLinear": dense.tolist()},
          "shaderTextureSamples": 1, "playModeTested": False}
write(WORK/"atlas-validation.json", json.dumps(report, ensure_ascii=False, indent=2))
print(json.dumps(report, ensure_ascii=True, indent=2))

# Keep future conversions on the lighting-aware material and add lightmap UVs.
from LightRoadBloodAtlas import upgrade
upgrade()
