using UnityEngine;
using UnityEngine.Rendering;

public abstract class Tenkoku_TemporalEffectBase : MonoBehaviour
{
    protected bool IsSupportedRenderPipeline()
    {
        return GraphicsSettings.currentRenderPipeline == null;
    }

    public void EnsureMaterial(ref Material material, Shader shader)
    {
        if (!IsSupportedRenderPipeline())
        {
            return;
        }

        if (shader != null)
        {
            if (material == null || material.shader != shader)
                material = new Material(shader);
            if (material != null)
                material.hideFlags = HideFlags.DontSave;
        }
        else
        {
            Debug.LogWarning("missing shader", this);
        }
    }

    public void EnsureKeyword(Material material, string name, bool enabled)
    {
        if (material == null)
        {
            return;
        }

        if (enabled != material.IsKeywordEnabled(name))
        {
            if (enabled)
                material.EnableKeyword(name);
            else
                material.DisableKeyword(name);
        }
    }

    public void EnsureRenderTarget(ref RenderTexture rt, int width, int height, RenderTextureFormat format, FilterMode filterMode, int depthBits = 0)
    {
        if (rt != null && (rt.width != width || rt.height != height || rt.format != format || rt.filterMode != filterMode))
        {
            RenderTexture.ReleaseTemporary(rt);
            rt = null;
        }
        if (rt == null)
        {
            rt = RenderTexture.GetTemporary(width, height, depthBits, format);
            rt.filterMode = filterMode;
            rt.wrapMode = TextureWrapMode.Clamp;
        }
    }

    public void FullScreenQuad()
    {
        GL.PushMatrix();
        GL.LoadOrtho();

        GL.Begin(GL.QUADS);
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 0.0f);

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 0.0f);

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 0.0f);

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f);

        GL.End();
        GL.PopMatrix();
    }
}
