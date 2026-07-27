using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace Tenkoku.Core
{
    [ExecuteInEditMode]
    public class TenkokuCameraTools : MonoBehaviour
    {
        public enum tenCamToolType { sky, skybox, particles, none };
        public tenCamToolType cameraType;
        public RenderTexture renderTexDiff;

        private Tenkoku.Core.TenkokuModule tenkokuModuleObject;
        private Camera cam;
        private Transform camTrans;
        private Camera copyCam;
        private Transform copyCamTrans;
        private Matrix4x4 camMatrix;

        void Start()
        {
            if (Application.isPlaying)
            {
                tenkokuModuleObject = UnityEngine.Object.FindFirstObjectByType<Tenkoku.Core.TenkokuModule>();

                cam = gameObject.GetComponent<Camera>() as Camera;
                camTrans = gameObject.GetComponent<Transform>() as Transform;
                if (tenkokuModuleObject != null && tenkokuModuleObject.mainCamera != null)
                {
                    copyCam = tenkokuModuleObject.mainCamera.GetComponent<Camera>();
                    copyCamTrans = tenkokuModuleObject.mainCamera.GetComponent<Transform>();
                }
            }

            BuildTexture();
        }

        void LateUpdate()
        {
            if (Application.isPlaying && tenkokuModuleObject != null && tenkokuModuleObject.useCameraCam != null)
            {
                copyCam = tenkokuModuleObject.useCameraCam;

                if (tenkokuModuleObject.useCamera != null)
                {
                    copyCamTrans = tenkokuModuleObject.useCamera;
                }

                CameraUpdate();
            }
        }

        void CameraUpdate()
        {
            if (copyCam != null && copyCamTrans != null && cam != null && camTrans != null)
            {
                cam.enabled = true;
                camTrans.position = copyCamTrans.position;
                camTrans.rotation = copyCamTrans.rotation;
                cam.projectionMatrix = copyCam.projectionMatrix;
                cam.fieldOfView = copyCam.fieldOfView;
                if (GraphicsSettings.currentRenderPipeline == null)
                {
                    cam.renderingPath = copyCam.actualRenderingPath;
                }
                cam.farClipPlane = copyCam.farClipPlane;

                if (renderTexDiff != null)
                {
                    if (cameraType == tenCamToolType.sky && tenkokuModuleObject != null)
                    {
                        if (tenkokuModuleObject.atmosphereModelTypeIndex == 0)
                        {
#if UNITY_5_6_OR_NEWER
                            cam.allowHDR = false;
#else
                            cam.hdr = false;
#endif
                        }

                        if (tenkokuModuleObject.atmosphereModelTypeIndex == 1)
                        {
#if UNITY_5_6_OR_NEWER
                            cam.allowHDR = true;
#else
                            cam.hdr = true;
#endif
                        }
                    }

                    if (cameraType == tenCamToolType.skybox)
                    {
                        if (tenkokuModuleObject != null && tenkokuModuleObject.atmosphereModelTypeIndex == 1)
                        {
#if UNITY_5_6_OR_NEWER
                            cam.allowHDR = true;
#else
                            cam.hdr = true;
#endif
                        }
                        cam.targetTexture = renderTexDiff;
                        Shader.SetGlobalTexture("_Tenkoku_SkyBox", renderTexDiff);
                    }

                    if (cameraType == tenCamToolType.particles)
                    {
                        cam.targetTexture = renderTexDiff;
                        Shader.SetGlobalTexture("_Tenkoku_ParticleTex", renderTexDiff);
                    }
                }
                else
                {
                    BuildTexture();
                }
            }
        }

        void BuildTexture()
        {
            if (cameraType == tenCamToolType.sky || cameraType == tenCamToolType.skybox)
            {
                renderTexDiff = new RenderTexture(128, 128, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            }

            if (renderTexDiff == null)
            {
                return;
            }

            renderTexDiff.useMipMap = true;

#if UNITY_2017_1_OR_NEWER
            renderTexDiff.autoGenerateMips = true;
#else
            renderTexDiff.generateMips = true;
#endif

            if (cam != null)
            {
                cam.targetTexture = renderTexDiff;
            }
        }
    }
}
