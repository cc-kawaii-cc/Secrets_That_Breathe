from pathlib import Path
import re
s=Path('Assets/Tonpalm/Lawyer Room.unity').read_text(encoding='utf-8')
m=re.search(r'^--- !u!82 &\d+\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)
print(m.group()[:6500])
