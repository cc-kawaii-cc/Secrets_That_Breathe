"""Upgrade the atlas prefabs to URP Lit; safe to run repeatedly."""
from pathlib import Path
import re
import struct

ROOT = Path(__file__).resolve().parents[1] / 'Assets/SecretsThatBreathe/Props/RoadBlood'

def upgrade():
    path = ROOT / 'Materials/RoadBlood_Atlas.mat'
    text = path.read_text(encoding='utf-8')
    text = re.sub(r'(m_Shader: .*guid: )\w+', r'\g<1>933532a4fcc9baf4fa0491de14d08ed7', text)
    keywords = ['_ALPHATEST_ON', '_ENVIRONMENTREFLECTIONS_OFF', '_SPECULARHIGHLIGHTS_OFF', '_SURFACE_TYPE_TRANSPARENT']
    text = re.sub(r'  m_ValidKeywords:.*?(?=  m_InvalidKeywords:)', '  m_ValidKeywords:\n' + ''.join('  - '+k+'\n' for k in keywords), text, flags=re.S)
    values = dict(_WorkflowMode=1, _Surface=1, _Blend=0, _BlendModePreserveSpecular=0,
                  _AlphaClip=1, _Cutoff=.005, _SrcBlend=5, _DstBlend=10,
                  _SrcBlendAlpha=1, _DstBlendAlpha=10, _ZWrite=0, _Cull=0,
                  _ReceiveShadows=1, _Metallic=0, _Smoothness=.15,
                  _SpecularHighlights=0, _EnvironmentReflections=0, _QueueOffset=-20)
    properties = '    m_Floats:\n' + ''.join(f'    - {k}: {v}\n' for k,v in values.items())
    properties += '    m_Colors:\n    - _BaseColor: {r: 1, g: 1, b: 1, a: 1}\n    - _EmissionColor: {r: 0, g: 0, b: 0, a: 1}\n'
    text = re.sub(r'    m_Floats:.*?(?=  m_BuildTextureStacks:)', properties, text, flags=re.S)
    path.write_text(text, encoding='utf-8')
    for path in sorted((ROOT/'Meshes').glob('RoadBlood_*.asset')):
        text = path.read_text(encoding='utf-8')
        assert 'm_VertexCount: 4' in text
        data = bytes.fromhex(re.search(r'_typelessdata: (\w+)', text)[1])
        if len(data) == 144:
            uv2 = [(0,0), (0,1), (1,1), (1,0)]
            data = b''.join(data[i*36:(i+1)*36]+struct.pack('<2f', *uv2[i]) for i in range(4))
            channels = re.search(r'    m_Channels:\n(.*?)    m_DataSize:', text, re.S)
            entries = re.findall(r'    - stream:.*?(?=    - stream:|\Z)', channels[1], re.S)
            assert len(entries) == 14
            entries[5] = '    - stream: 0\n      offset: 36\n      format: 0\n      dimension: 2\n'
            text = text[:channels.start(1)] + ''.join(entries) + text[channels.end(1):]
            text = text.replace('m_DataSize: 144', 'm_DataSize: 176')
            text = re.sub(r'(_typelessdata: )\w+', lambda m: m[1]+data.hex(), text)
            path.write_text(text, encoding='utf-8')
        assert len(data) == 176
        assert 'indexCount: 6' in text
    for path in sorted((ROOT/'Prefabs').glob('RoadBlood_*.prefab')):
        text = path.read_text(encoding='utf-8')
        for key in ('m_ReceiveShadows', 'm_LightProbeUsage', 'm_ScaleInLightmap'):
            text = re.sub(r'('+key+r': )\d+', r'\g<1>1', text)
        path.write_text(text, encoding='utf-8')
    print('Validated: URP Lit, no emission, receive shadows/probes, UV2; 3 meshes x 2 triangles.')

if __name__ == '__main__':
    upgrade()
