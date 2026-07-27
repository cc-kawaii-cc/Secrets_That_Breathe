using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tenkoku.Effects
{
    [ExecuteInEditMode]
    //[ImageEffectAllowedInSceneView]
    [RequireComponent(typeof(Camera))]
    public class TenkokuSkyBlur : MonoBehaviour
    {
        public int downSample = 4;
        public float blurSpread = 0.6f;
        public Shader blurShader = null;
        public Material material = null;

        private int i = 0;
        private int rtW;
        private int rtH;
        private float off;

        void Start()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                enabled = false;
                return;
            }

            if (blurShader == null || !blurShader.isSupported)
            {
                enabled = false;
                return;
            }

            if (material == null)
            {
                material = new Material(blurShader);
                material.hideFlags = HideFlags.DontSave;
            }

            if (material.shader == null || !material.shader.isSupported)
            {
                enabled = false;
            }
        }

        void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
        {
            off = 0.5f + iteration * blurSpread;
            Graphics.BlitMultiTap(
                source,
                dest,
                material,
                new Vector2(-off, -off),
                new Vector2(-off, off),
                new Vector2(off, off),
                new Vector2(off, -off));
        }

        void DownSample4x(RenderTexture source, RenderTexture dest)
        {
            Graphics.BlitMultiTap(
                source,
                dest,
                material,
                new Vector2(-1.0f, -1.0f),
                new Vector2(-1.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(1.0f, -1.0f));
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            rtW = source.width / downSample;
            rtH = source.height / downSample;
            RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0, source.format);

            DownSample4x(source, buffer);

            for (i = 0; i < 3; i++)
            {
                RenderTexture buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0, source.format);
                FourTapCone(buffer, buffer2, i);
                RenderTexture.ReleaseTemporary(buffer);
                buffer = buffer2;
            }

            Graphics.Blit(buffer, destination);
            RenderTexture.ReleaseTemporary(buffer);
        }
    }
}
