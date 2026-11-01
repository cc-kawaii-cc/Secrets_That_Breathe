from pathlib import Path
import re
import json
root=Path(__file__).resolve().parents[2]
path=root/'Assets/MainScenes/From_PM/MainScene_2_Evening.unity'
s=path.read_text(encoding='utf-8-sig')
backup=Path(__file__).parent/'Evening-before-F2.unity.txt'
if not backup.exists(): backup.write_text(s,encoding='utf-8')
blocks={int(m[2]):m[0] for m in re.finditer(r'^--- !u!(\d+) &(\d+)[^\n]*\n.*?(?=^---|\Z)',s,re.M|re.S)}
def named(name): return next((id,b) for id,b in blocks.items() if f'  m_Name: {name}\n' in b)
assert '  m_Name: TestSkipIntroHint\n' not in s
panel_id,panel=named('TestSkipCarExitHint')
owned=[panel_id]
owned += [id for id,b in blocks.items() if f'  m_GameObject: {{fileID: {panel_id}}}\n' in b]
rect_id=next(id for id in owned if blocks[id].startswith('--- !u!224'))
text_rect=int(re.search(r'  m_Children:\n  - \{fileID: (\d+)',blocks[rect_id])[1])
text_go=int(re.search(r'  m_GameObject: \{fileID: (\d+)',blocks[text_rect])[1])
owned += [text_go]+[id for id,b in blocks.items() if f'  m_GameObject: {{fileID: {text_go}}}\n' in b]
mapping={id:731510001+i for i,id in enumerate(owned)}
assert not set(mapping.values()) & set(blocks)
clones=''
for id in owned:
    b=blocks[id]
    if id==panel_id: b=b.replace('TestSkipCarExitHint','TestSkipIntroHint')
    if id==rect_id:
        y=float(re.search(r'm_AnchoredPosition: \{x: [^,]+, y: ([^}]+)',b)[1])-60
        b=re.sub(r'm_AnchoredPosition: \{x: ([^,]+), y: [^}]+}',lambda m:'m_AnchoredPosition: {x: '+m[1]+', y: '+str(y)+'}',b)
    if '  m_text:' in b:
        b=re.sub(r'^  m_text: .*',lambda m:'  m_text: '+json.dumps('[F2] Skip Intro',ensure_ascii=True),b,flags=re.M)
    b=re.sub(r'&(\d+)',lambda m:'&'+str(mapping.get(int(m[1]),int(m[1]))),b)
    b=re.sub(r'fileID: (\d+)',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),b)
    clones+=b
parent_id=int(re.search(r'm_Father: \{fileID: (\d+)',blocks[rect_id])[1])
parent=blocks[parent_id]
s=s.replace(parent,parent.replace('  m_Children:\n',f'  m_Children:\n  - {{fileID: {mapping[rect_id]}}}\n'))
guid=re.search(r'guid: (\w+)',(root/'Assets/SecretsThatBreathe/Scripts/EveningTestingShortcuts.cs.meta').read_text())[1]
script=next(b for b in blocks.values() if f'guid: {guid},' in b)
s=s.replace(script,script+f'  testSkipIntroHint: {{fileID: {mapping[panel_id]}}}\n')
s=s.replace('--- !u!1660057539',clones+'--- !u!1660057539')
ids=re.findall(r'^--- !u!\d+ &(\d+)',s,re.M); assert len(ids)==len(set(ids))
path.write_text(s,encoding='utf-8')
print('Saved F2 hint below F1, with existing font/style, and connected scene reference.')
