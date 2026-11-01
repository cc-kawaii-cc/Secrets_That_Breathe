"""Extract curated single steps; requires numpy and soundfile (.deps is ignored)."""
from pathlib import Path
import json
import sys
import uuid
sys.path.insert(0, str(Path(__file__).parent / '.deps'))
import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / 'Assets/SecretsThatBreathe/Audio/Footsteps'
TEMPLATE = (DEST / 'Concrete/Concrete_Footstep_01.wav.meta').read_text()
SETS = [
    ('concrete-footsteps-1-6265', 'Surfaces/Concrete', 'Concrete_Footstep',
     [.30, .98, 1.64, 2.11, 2.62, 3.19, 3.69, 4.14, 4.59, 6.09, 6.73, 7.22]),
    ('footsteps-glass-75638', 'Surfaces/Glass', 'Glass_Footstep',
     [.85, 1.66, 2.47, 3.90, 7.21, 8.15, 8.78, 9.69,
      10.43, 14.78, 20.15, 23.39, 25.33, 28.60, 29.52, 32.36]),
]

def folder_meta(path):
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex +
                        '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                        '  userData: \n  assetBundleName: \n  assetBundleVariant: \n')

report = []
for source, folder, prefix, peaks in SETS:
    x, sr = sf.read(ROOT / 'Assets' / (source + '.mp3'), always_2d=True)
    mono = np.mean(x, axis=1)
    dest = DEST / folder
    dest.mkdir(exist_ok=True)
    folder_meta(dest)
    # Constant gain within each recording preserves natural step variation.
    gain = min(2.0, .85 / max(abs(mono)))
    for index, peak in enumerate(peaks, 1):
        # Keep attack lead-in and decay, ending before the next footfall.
        start = max(0, peak - .10)
        end = min(len(mono) / sr, peak + .36)
        if index < len(peaks): end = min(end, peaks[index] - .12)
        clip = mono[round(start*sr):round(end*sr)].copy() * gain
        fade_in, fade_out = round(.003*sr), round(.025*sr)
        clip[:fade_in] *= np.linspace(0, 1, fade_in)
        clip[-fade_out:] *= np.linspace(1, 0, fade_out)
        assert len(clip) > sr*.15 and np.max(abs(clip)) < 1
        output = dest / f'{prefix}_{index:02d}.wav'
        sf.write(output, clip, sr, subtype='PCM_16')
        meta = Path(str(output) + '.meta')
        if not meta.exists():
            lines = TEMPLATE.splitlines()
            lines[1] = 'guid: ' + uuid.uuid4().hex
            meta.write_text('\n'.join(lines) + '\n')
        decoded, rate = sf.read(output)
        assert rate == sr and len(decoded) == len(clip)
        assert decoded[0] == 0 and decoded[-1] == 0
        report.append(dict(file=output.relative_to(ROOT).as_posix(),
                           source=source+'.mp3', source_start=start, source_end=end,
                           duration=len(clip)/sr, sample_rate=sr,
                           peak=float(max(abs(decoded))), gain=gain))
(Path(__file__).parent / 'split-report.json').write_text(json.dumps(report, indent=2))
print(json.dumps({folder:len(peaks) for _,folder,_,peaks in SETS}))
print('Verified PCM round-trip, no clipping, zero-valued fade endpoints.')
