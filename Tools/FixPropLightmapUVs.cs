using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class FixPropLightmapUVs
{
    public static void Build()
    {
        try {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string report=""; int count=0;
            foreach(string folder in new[]{"DocumentBinder","KraftEnvelopes","A4Documents"}) {
                string root="Assets/SecretsThatBreathe/Props/"+folder+"/Models";
                foreach(string path in Directory.GetFiles(root,"*_Mesh.asset")) {
                    var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    var beforeUV=mesh.uv; int triangles=mesh.triangles.Length;
                    var settings=new UnwrapParam(); UnwrapParam.SetDefaults(out settings); settings.packMargin=.02f;
                    if(!Unwrapping.GenerateSecondaryUVSet(mesh,settings)) throw new Exception("Lightmap unwrap failed: "+path);
                    var uv=mesh.uv2;
                    if(uv.Length!=mesh.vertexCount || triangles!=mesh.triangles.Length) throw new Exception("Invalid generated mesh: "+path);
                    foreach(var p in uv) if(float.IsNaN(p.x)||float.IsNaN(p.y)||p.x<0||p.x>1||p.y<0||p.y>1) throw new Exception("Lightmap UV out of bounds: "+path);
                    // Unwrapping may split vertices, but never change the texture mapping on triangles.
                    foreach(var p in mesh.uv) { bool found=false; foreach(var old in beforeUV) if((p-old).sqrMagnitude<1e-12f) { found=true; break; } if(!found) throw new Exception("Texture UV changed"); }
                    EditorUtility.SetDirty(mesh); count++;
                    report+="PASS: "+path+", lightmap UV2 "+uv.Length+" vertices, "+triangles/3+" triangles, original texture UV retained.\n";
                }
            }
            if(count!=7) throw new Exception("Expected 7 prop meshes, got "+count);
            AssetDatabase.SaveAssets(); File.WriteAllText("LightmapUVVerification.txt",report);
            Debug.Log("PROP_LIGHTMAP_UV_PASS\n"+report); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
