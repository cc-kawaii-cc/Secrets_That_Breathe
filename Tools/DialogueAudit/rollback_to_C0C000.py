from pathlib import Path
from datetime import datetime
import re,json,shutil
scene=Path('Assets/Tonpalm/Lawyer Room.unity')
current=scene.read_bytes()
checkpoint=Path('Tools/DialogueAudit/LawyerRoom-before-C0C000-text.unity.backup')
s=checkpoint.read_text(encoding='utf-8')
report=json.loads(Path('Tools/DialogueAudit/yellow-C0C000-validation.json').read_text(encoding='utf-8'))
colour='{r: 0.7529412, g: 0.7529412, b: 0, a: 1}'
rgba=192 | (192<<8) | (255<<24)
for target in report['changedTextComponents']:
 pat=r'(^--- !u!114 &'+str(target)+r'\n.*?)(?=^--- !u!|\Z)'
 def recolour(m):
  b=m.group(1)
  assert 'm_fontColor: {r: 1, g: 1, b: 0, a: 1}' in b
  b=re.sub(r'^  (m_Color|m_fontColor): .*',lambda c:'  '+c.group(1)+': '+colour,b,flags=re.M)
  return re.sub(r'(m_fontColor32:\n    serializedVersion: \d+\n    rgba:) \d+',lambda c:c.group(1)+' '+str(rgba),b)
 s,n=re.subn(pat,recolour,s,flags=re.M|re.S)
 assert n==1,target
assert 'unusedTextCleanupRevision:' not in s
assert 'm_Name: Conversation\n' in s and 'm_Name: Line12\n' in s
assert s.count('m_fontColor: '+colour)==5
assert 'm_Name: TonRequestDialogueCanvas\n' in s
assert 'm_Name: ExitObjective\n' in s
assert s.count('m_Name: DialogueTypingAudio\n')==2
assert scene.read_bytes()==current,'Scene changed while preparing rollback; retry.'
backup=Path('Tools/DialogueAudit')/('LawyerRoom-before-rollback-to-C0C000-'+datetime.now().strftime('%Y%m%d-%H%M%S')+'.unity.backup')
shutil.copy2(scene,backup)
scene.write_text(s,encoding='utf-8')
assert scene.read_text(encoding='utf-8')==s
Path('Tools/DialogueAudit/rollback-to-C0C000-validation.json').write_text(json.dumps({'result':'PASS','restoredCheckpoint':'Immediately after yellow text colour change to #C0C000','sourceSnapshot':str(checkpoint),'previousSceneBackup':str(backup),'yellowTextCount':5,'oldConversationAndDuplicateLineRestored':True,'typingAudioAndSeparateRequestCanvasRetained':True,'automaticCleanupScriptAbsent':not Path('Assets/SecretsThatBreathe/Scripts/Editor/UnusedTextCleanup.cs').exists()},indent=2),encoding='utf-8')
print('Restored the exact pre-colour snapshot plus the original five #C0C000 colour changes. Later scene changes are archived in',backup.name)
