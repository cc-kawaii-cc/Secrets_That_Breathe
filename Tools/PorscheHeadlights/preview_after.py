import os
p=os.path.join(os.path.dirname(os.path.abspath(__file__)),'preview_porsche.py')
setup=open(p,encoding='utf-8').read().split("shot('before_front'")[0]
setup=setup.replace("os.path.join(out,'PorscheV2_Inspection.blend')", "os.path.join(out,'Source','PorscheV2_HeadlightsSeparated.blend')")
exec(setup)
shot('after_front',(0,-6,2.6),(0,-0.55,0.55),2.75)
shot('after_threequarter',(4,-6,3),(0,0,0.5),5.7)
shot('after_rear',(-4,6,2.5),(0,0,0.5),5.7)
for ob in scene.objects:
    if ob.type=='MESH':ob.hide_render=not ob.name.startswith('Headlight_')
shot('after_headlights_only',(0,-6,2.6),(0,-0.55,0.55),2.75)
print('PORSCHE_AFTER_PREVIEW_COMPLETE')
