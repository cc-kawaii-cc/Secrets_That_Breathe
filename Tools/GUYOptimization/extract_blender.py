"""Extract files from the user's existing MSI without installing or running it."""
from pathlib import Path
import ctypes as c, subprocess, json
msi=c.WinDLL('msi.dll'); U=c.c_uint; W=c.c_wchar_p
msi.MsiOpenDatabaseW.argtypes=[W,W,c.POINTER(U)]
msi.MsiDatabaseOpenViewW.argtypes=[U,W,c.POINTER(U)]
msi.MsiViewExecute.argtypes=[U,U];msi.MsiViewFetch.argtypes=[U,c.POINTER(U)]
msi.MsiRecordGetStringW.argtypes=[U,U,W,c.POINTER(U)]
msi.MsiRecordReadStream.argtypes=[U,U,c.c_void_p,c.POINTER(U)]
def check(code):
    if code:raise OSError(code)
db=U();check(msi.MsiOpenDatabaseW('C:/Users/mo_an/Downloads/blender-5.2.3-windows-x64.msi',None,c.byref(db)))
def string(rec,i):
    n=U(32767);buf=c.create_unicode_buffer(n.value);check(msi.MsiRecordGetStringW(rec,i,buf,c.byref(n)));return buf.value
def rows(sql,n):
    v=U();check(msi.MsiDatabaseOpenViewW(db,sql,c.byref(v)));check(msi.MsiViewExecute(v,0))
    try:
        while True:
            rec=U();code=msi.MsiViewFetch(v,c.byref(rec))
            if code==259:break
            check(code)
            try:yield [string(rec,i+1) for i in range(n)]
            finally:msi.MsiCloseHandle(rec)
    finally:msi.MsiCloseHandle(v)
out=Path('Tools/GUYOptimization/Blender').resolve();out.mkdir(parents=True,exist_ok=True)
dirs={k:(parent,name.split('|')[-1].split(':')[0]) for k,parent,name in rows('SELECT `Directory`,`Directory_Parent`,`DefaultDir` FROM `Directory`',3)}
components=dict(rows('SELECT `Component`,`Directory_` FROM `Component`',2))
files=list(rows('SELECT `File`,`Component_`,`FileName` FROM `File`',3))
def folder(key):
    if key not in dirs or key=='TARGETDIR':return out
    parent,name=dirs[key]
    return folder(parent)/(name if name not in ['.','SourceDir'] else '')
for media,cabinet in rows('SELECT `DiskId`,`Cabinet` FROM `Media`',2):
    assert cabinet.startswith('#'),cabinet
    v=U();check(msi.MsiDatabaseOpenViewW(db,"SELECT `Data` FROM `_Streams` WHERE `Name`='"+cabinet[1:]+"'",c.byref(v)));check(msi.MsiViewExecute(v,0))
    rec=U();check(msi.MsiViewFetch(v,c.byref(rec)));cab=out/(media+'.cab')
    with cab.open('wb') as f:
        while True:
            buf=c.create_string_buffer(1048576);n=U(len(buf));check(msi.MsiRecordReadStream(rec,1,buf,c.byref(n)))
            if not n.value:break
            f.write(buf.raw[:n.value])
    msi.MsiCloseHandle(rec);msi.MsiCloseHandle(v)
    result=subprocess.run(['C:/Windows/System32/expand.exe','-F:*',cab.name,'.'],cwd=out,capture_output=True)
    if result.returncode:raise RuntimeError(result.stdout[-2000:])
for key,component,name in files:
    source=out/key;dest=folder(components[component])/name.split('|')[-1]
    assert dest.resolve().is_relative_to(out)
    dest.parent.mkdir(parents=True,exist_ok=True)
    if source.exists() and source!=dest:source.replace(dest)
exe=list(out.rglob('blender.exe'));assert len(exe)==1,exe
Path('Tools/GUYOptimization/blender-path.json').write_text(json.dumps(str(exe[0])))
print('Extracted Blender without installation:',exe[0])
