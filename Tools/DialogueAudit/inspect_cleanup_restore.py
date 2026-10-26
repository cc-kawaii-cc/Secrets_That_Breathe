from pathlib import Path
import re,json
backup=Path('Tools/DialogueAudit/LawyerRoom-before-unused-text-cleanup-disk.unity.backup').read_text(encoding='utf-8')
current=Path('Assets/Tonpalm/Lawyer Room.unity').read_text(encoding='utf-8')
def parse(s):return {int(m.group(2)):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
a=parse(backup);b=parse(current)
print('Backup blocks',len(a),'Current blocks',len(b),'Missing old blocks',len(set(a)-set(b)),'New blocks',len(set(b)-set(a)))
print('Cleanup flag',re.findall(r'unusedTextCleanupRevision: (\d+)',current))
print('Legacy root names',[(n,current.count('m_Name: '+n+'\n')) for n in ['Conversation','Line12']])
print('Cleanup report',Path('Tools/DialogueAudit/unused-text-cleanup-validation.json').read_text(encoding='utf-8'))
