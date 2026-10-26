from pathlib import Path
import re,json
exec(Path('Tools/DialogueAudit/verify_authored_canvases.py').read_text(encoding='utf-8-sig').split('report=[]')[0])
clipguid=re.search(r'guid: (\w+)',Path('Assets/video-game-text-330163.mp3.meta').read_text()).group(1)
controllers=[]
for script in ['PhoneCallDialogueUI','AuntDialogueUI']:
 guid=re.search(r'guid: (\w+)',Path('Assets/SecretsThatBreathe/Scripts/'+script+'.cs.meta').read_text()).group(1)
 b=next(b for _,(k,b) in blocks.items() if k==114 and 'guid: '+guid+',' in b)
 aid=fid(b,'typingAudio')
 kind,audio=blocks[aid]
 assert kind==82,kind
 assert 'guid: '+clipguid+',' in audio
 assert prop(audio,'m_PlayOnAwake')=='0'
 assert prop(audio,'Loop')=='1' or prop(audio,'m_Loop')=='1'
 controllers.append({'controller':script,'audioSource':aid,'volume':prop(audio,'m_Volume')})
 if script=='AuntDialogueUI':
  view=re.search(r'  visitorDialogue:\n.*?    speaker: \{fileID: (\d+)',b,re.S)
  name=blocks[int(view.group(1))][1]
  raw=re.search(r'^  m_text: (.*)',name,re.M).group(1)
  decoded=re.sub(r'\\u([0-9a-fA-F]{4})',lambda m:chr(int(m.group(1),16)),raw)
  assert decoded.strip('"')=='ลุงป้อง',decoded
Path('Tools/DialogueAudit/typing-audio-validation.json').write_text(json.dumps({'result':'PASS','clip':'video-game-text-330163.mp3','controllers':controllers,'visitorName':'ลุงป้อง'},ensure_ascii=False,indent=2),encoding='utf-8')
print('PASS: both dialogue controllers have the requested clip on saved looping AudioSources, play-on-awake off; speaker = ลุงป้อง.')
