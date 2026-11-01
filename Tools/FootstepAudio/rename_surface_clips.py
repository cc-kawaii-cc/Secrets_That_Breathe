from pathlib import Path
import re
import uuid

root = Path(__file__).resolve().parents[2]
base = root / 'Assets/SecretsThatBreathe/Audio/Footsteps'
# Keep the original Lawyer Room Concrete bank intact in its existing location.
surfaces = base / 'Surfaces'
surfaces.mkdir(exist_ok=True)
meta = Path(str(surfaces) + '.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+
                    '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                    '  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
before = {}
for old, new in [('Concrete6265', 'Concrete'), ('Glass75638', 'Glass')]:
    folder = base / old
    target = surfaces / new
    assert folder.is_dir() and not target.exists()
    for audio in folder.glob('*.wav'):
        old_meta = Path(str(audio) + '.meta')
        before[re.search(r'guid: (\w+)', old_meta.read_text()).group(1)] = audio.read_bytes()
        renamed = audio.with_name(audio.name.replace(old, new))
        audio.rename(renamed)
        old_meta.rename(Path(str(renamed) + '.meta'))
    folder.rename(target)
    Path(str(folder) + '.meta').rename(Path(str(target) + '.meta'))

tools = Path(__file__).parent
for filename in ['split_footsteps.py', 'install_evening_surfaces.py', 'split-report.json']:
    path = tools / filename
    text = path.read_text(encoding='utf-8')
    for old, new in [('Concrete6265','Concrete'), ('Glass75638','Glass')]:
        text = text.replace("'"+old+"'", "'Surfaces/"+new+"'")
        text = text.replace('/'+old+'/', '/Surfaces/'+new+'/')
        text = text.replace(old+'_Footstep', new+'_Footstep')
    path.write_text(text, encoding='utf-8')
for guid, data in before.items():
    matching = [p for p in surfaces.rglob('*.wav.meta') if guid in p.read_text()]
    assert len(matching) == 1
    assert Path(str(matching[0])[:-5]).read_bytes() == data
print(f'Renamed {len(before)} clips; all WAV contents and Unity GUIDs preserved.')
