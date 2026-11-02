"""Read-only checks for the saved encounter and untouched scene objects."""
import json, re
from pathlib import Path

scene = Path('Assets/MainScenes/From_PM/MainScene_2_Evening.unity')
def parse(text):
    return {int(m[2]): (int(m[1]), m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}
def ref(body, name):
    return int(re.search(r'^  '+name+r': \{fileID: (-?\d+)\}', body, re.M)[1])
current = parse(scene.read_text(encoding='utf-8-sig'))
before = parse(next(Path('Tools/EveningPoliceEncounter').glob('before-encounter-*.unity.txt')).read_text(encoding='utf-8-sig'))
report = json.loads(Path('Tools/EveningPoliceEncounter/ui-report.json').read_text())
for ident, value in before.items():
    if ident != 1987101001:
        assert current[ident] == value, ('Original scene object changed', ident)
new = {i: v for i, v in current.items() if i not in before}
for ident, (kind, body) in new.items():
    for line in body.splitlines():
        if 'guid:' not in line:
            for value in re.findall(r'\{fileID: (-?\d+)\}', line):
                assert int(value) == 0 or int(value) in current, ('Missing reference', ident, value)
    if kind == 224:
        cursor, seen = ident, set()
        while cursor != 1987101001:
            assert cursor not in seen, ('Hierarchy cycle', ident)
            seen.add(cursor)
            cursor = ref(current[cursor][1], 'm_Father')
            assert cursor in current, ('UI outside SeatingCanvas', ident)
        for child in re.findall(r'^  - \{fileID: (\d+)\}', body, re.M):
            assert ref(current[int(child)][1], 'm_Father') == ident
assert len(report['lines']) == 15
for key, ident in report['lines'].items():
    body = current[ident][1]
    assert 'm_fontColor: {r: 1, g: 1, b: 1, a: 1}' in body
    assert ('m_fontSize: 25' if key.endswith('Narration') else 'm_fontSize: 30') in body
menus = []
for ident in report['choices']:
    body = current[ident][1]
    labels = [ref(body, side+'Label') for side in ('accept', 'refuse')]
    menus += labels
    go = ref(body, 'm_GameObject')
    components = [int(v) for v in re.findall(r'component: \{fileID: (\d+)\}', current[go][1])]
    print('Choice menu', ident, 'component classes:', [current[c][0] for c in components])
assert len(set(menus)) == 4
choice_text = re.search(r'^  m_text: (.+)$', current[menus[0]][1], re.M)[1]
assert '<color=#FF3333>(โกหก)</color>' in json.loads(choice_text)
assert all(i not in menus for i in report['lines'].values())
assert 'm_GameObject: {fileID: 0}' not in current[842000256][1]
result = {'originalSceneBlocksUnchangedExceptCanvasChildren': len(before)-1,
          'newSerializedBlocks': len(new), 'editableLineSources': 15,
          'independentChoiceLabels': 4, 'allNewUIUnderSeatingCanvas': True,
          'allLocalReferencesValid': True}
Path('Tools/EveningPoliceEncounter/validation.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
