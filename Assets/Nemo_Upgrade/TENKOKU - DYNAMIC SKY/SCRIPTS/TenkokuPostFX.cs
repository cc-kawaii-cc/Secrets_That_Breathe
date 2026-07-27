using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tenkoku.Effects
{
    //[ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class TenkokuPostFX : MonoBehaviour
    {
        //PUBLIC VARIABLES
        public Shader useShader;

        //PRIVATE VARIABLES
        private Material useMat;

        void Start()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                enabled = false;
                return;
            }

            if (useShader == null || !useShader.isSupported)
            {
                enabled = false;
                return;
            }

            useMat = new Material(useShader);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (useMat == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            Graphics.Blit(source, destination, useMat);
        }
    }
}
