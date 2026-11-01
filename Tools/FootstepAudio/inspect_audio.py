from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).parent / '.deps'))
import soundfile as sf
import numpy as np
for name in ['concrete-footsteps-1-6265', 'footsteps-glass-75638']:
    x, sr = sf.read('Assets/' + name + '.mp3', always_2d=True)
    hop = int(sr * .01)
    env = np.array([np.sqrt(np.mean(x[i:i+hop]**2)) for i in range(0,len(x),hop)])
    candidates = [i for i in range(1,len(env)-1) if env[i] > env[i-1] and env[i] >= env[i+1] and env[i] > env.max() * .08]
    peaks = []
    for i in sorted(candidates, key=lambda i: env[i], reverse=True):
        if all(abs(i-j) >= 28 for j in peaks): peaks.append(i)
    print(name, 'duration', len(x)/sr, 'sr',sr, 'peak',abs(x).max())
    print([(round(i*.01,2), round(float(env[i]),4)) for i in sorted(peaks)])
