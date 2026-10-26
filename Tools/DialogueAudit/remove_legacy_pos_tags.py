from pathlib import Path
import shutil,json
paths=[Path('Assets/Tonpalm/Lawyer Room.unity'),Path('Assets/SecretsThatBreathe/Scripts/Editor/PhoneCallSetup.cs'),Path('Assets/SecretsThatBreathe/Scripts/Editor/DocumentSearchSetup.cs')]
marker=b'<pos=-30>:<pos=0>'
shutil.copy2(paths[0],'Tools/DialogueAudit/LawyerRoom-before-remove-pos-tags.unity.backup')
report=[]
for p in paths:
 before=p.read_bytes();count=before.count(marker)
 after=before.replace(marker,b'')
 assert b'<pos=' not in after
 assert before.count(b'\\n')==after.count(b'\\n')
 if count:p.write_bytes(after)
 report.append({'file':p.as_posix(),'removedLegacyTagPairs':count})
Path('Tools/DialogueAudit/removed-pos-tags.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
