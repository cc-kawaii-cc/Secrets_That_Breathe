from pathlib import Path
import re,json
exec(Path('Tools/DialogueAudit/verify_authored_canvases.py').read_text(encoding='utf-8-sig').split('report=[]')[0])
guid=re.search(r'guid: (\w+)',Path('Assets/SecretsThatBreathe/Scripts/PhoneCallDialogueUI.cs.meta').read_text()).group(1)
ui=next(b for _,(k,b) in blocks.items() if k==114 and 'guid: '+guid+',' in b)
arr=re.search(r'^  sentences:\n((?:  - \{fileID: \d+\}\n)+)',ui,re.M)
ids=[int(i) for i in re.findall(r'fileID: (\d+)',arr.group(1))]
def txt(b):
 raw=re.search(r'^  m_text: (.*?)(?=\n  m_\w+:)',b,re.M|re.S).group(1)
 return json.loads(re.sub(r'\n +',' ',raw)) if raw.startswith('"') else raw.strip()
texts=[txt(blocks[i][1]) for i in ids]
assert len(ids)==12
phrase='ฝากหาให้พี่ด้วยนะเข้ม'
assert texts[10]==phrase
assert phrase not in texts[9]
assert sum(phrase in t for t in texts)==1
requestid=fid(ui,'tonRequestSentence')
assert ids[10]==requestid
requestview=re.search(r'^  tonRequestDialogue:\n((?:    .*\n)+)',ui,re.M).group(1)
assert fid(requestview,'sentence')==requestid
canvasgo=fid(blocks[fid(requestview,'canvas')][1],'m_GameObject')
assert gos[canvasgo]=='TonRequestDialogueCanvas'
assert '/TonRequestDialogueCanvas/FlowingDialogue/Sentence' in path(fid(blocks[requestid][1],'m_GameObject'))
reference=re.search(r'^  tonDialogue:\n((?:    .*\n)+)',ui,re.M).group(1)
referencebody=blocks[fid(reference,'sentence')][1];body=blocks[requestid][1]
for key in ['m_fontSize','m_fontAsset','m_fontColor']:
 assert prop(body,key)==prop(referencebody,key),key
report={'result':'PASS','incomingLineCount':len(ids),'lastThreeLines':texts[-3:],'requestCanvasPath':'SeatingCanvas/PhoneCallUI/TonRequestDialogueCanvas/FlowingDialogue/Sentence','styleMatchesTon':True}
Path('Tools/DialogueAudit/ton-request-canvas-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
