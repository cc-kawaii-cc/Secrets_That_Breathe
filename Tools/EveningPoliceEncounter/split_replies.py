"""Split only the authored spoken replies; preserve both choice menus and scene edits."""
from pathlib import Path
import re, json, datetime

p = Path('Assets/MainScenes/From_PM/MainScene_2_Evening.unity')
raw = p.read_bytes()
s = raw.decode('utf-8-sig')
pat = r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)'
def parse(text):
    return {int(m[2]): (int(m[1]),m[3]) for m in re.finditer(pat,text,re.M|re.S)}
blocks = parse(s)
def ref(body,key):
    return int(re.search(r'^  '+key+r': \{fileID: (-?\d+)',body,re.M)[1])
def field(body,key,value):
    body,n = re.subn(r'^  '+key+r':[^\n]*',lambda m:'  '+key+': '+value,body,flags=re.M)
    assert n == 1,key
    return body
def text(body,value):
    body,n = re.subn(r'^  m_text:.*?(?=^  m_isRightToLeft:)',lambda m:'  m_text: '+json.dumps(value)+'\n',body,flags=re.M|re.S)
    assert n == 1
    return body
changed = {}; added = {}; nextid = 842001000
report_path = Path('Tools/EveningPoliceEncounter/ui-report.json')
report = json.loads(report_path.read_text())
assert 'lieReply2' not in report['lines'], 'Already split'
for key,root_name,sentences in [
    ('lieReply','03_lieReply', ['พอดีผมได้รับอนุญาติจากตำรวจที่สถานีมาแล้วน่ะครับ','เพราะฉะนั้นขอเข้าไปดูหน่อยนะครับ']),
    ('lawyerReply','04_lawyerReply',['ผมเป็นทนายของฝั่งเหยื่อในคดีนี้','พอดีเห็นว่ายังพึ่งเกิดเหตุได้ไม่นาน','เลยอยากจะมาดูด้วยตาตนเองหน่อยน่ะครับ'])]:
    label = report['lines'][key]
    label_go = ref(blocks[label][1],'m_GameObject')
    rect = next(i for i,(k,b) in blocks.items() if k == 224 and ref(b,'m_GameObject') == label_go)
    panel_rect = ref(blocks[rect][1],'m_Father')
    panel_go = ref(blocks[panel_rect][1],'m_GameObject')
    ids = [i for i,(k,b) in blocks.items() if (k==1 and i in [panel_go,label_go]) or (k!=1 and re.search(r'^  m_GameObject:',b,re.M) and ref(b,'m_GameObject') in [panel_go,label_go])]
    assert len([i for i in ids if blocks[i][0]==224]) == 2
    parent = ref(blocks[panel_rect][1],'m_Father')
    changed[label] = text(blocks[label][1],sentences[0])
    changed[panel_go] = field(blocks[panel_go][1],'m_Name',root_name+'1')
    refs = [label]; sibling_rects = []
    for index,value in enumerate(sentences[1:],2):
        mapping = {}
        for i in ids:
            nextid += 1
            assert nextid not in blocks
            mapping[i] = nextid
        for i in ids:
            kind,body=blocks[i]
            body = re.sub(r'\{fileID: (-?\d+)\}',lambda m:'{fileID: '+str(mapping.get(int(m[1]),int(m[1])))+'}',body)
            if i == label: body=text(body,value)
            if i == panel_go: body=field(body,'m_Name',root_name+str(index))
            added[mapping[i]]=(kind,body)
        refs.append(mapping[label]); sibling_rects.append(mapping[panel_rect])
        report['lines'][key+str(index)] = mapping[label]
    body=changed.get(parent,blocks[parent][1])
    needle=f'  - {{fileID: {panel_rect}}}\n'
    assert needle in body
    changed[parent]=body.replace(needle,needle+''.join(f'  - {{fileID: {i}}}\n' for i in sibling_rects),1)
    controller=report['controller']
    body=changed.get(controller,blocks[controller][1])
    singular=key; plural='lieReplies' if key=='lieReply' else 'lawyerReplies'
    body=body.replace(f'  {singular}: {{fileID: {label}}}\n','  '+plural+':\n'+''.join(f'  - {{fileID: {i}}}\n' for i in refs))
    changed[controller]=body
changed[report['controller']]=field(changed[report['controller']],r'  completedLineHold','3')
for i,body in changed.items():
    assert s.count(blocks[i][1]) == 1
    s=s.replace(blocks[i][1],body)
s+=''.join(f'--- !u!{kind} &{i}\n{body}' for i,(kind,body) in added.items())
after=parse(s)
for i,value in blocks.items():
    if i not in changed: assert after[i]==value,('Unexpected scene edit',i)
for i,(kind,body) in added.items():
    for line in body.splitlines():
        if 'guid:' not in line:
            for ident in re.findall(r'\{fileID: (-?\d+)\}',line): assert int(ident)==0 or int(ident) in after
backup=Path('Tools/EveningPoliceEncounter')/('before-split-replies-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S')+'.unity.txt')
backup.write_bytes(raw)
assert p.read_bytes()==raw
p.write_text(s,encoding='utf-8',newline='\n')
report['newBlocks']=len(after)-722
report_path.write_text(json.dumps(report,indent=2))
print('PASS: lie 2 sentences, lawyer 3 sentences; hold 3s. Choice menus and unrelated scene blocks unchanged.')
