using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumetricLightScattering : ScriptableRendererFeature
{
    [System.Serializable]
    public class VolumetricLightScatteringSettings
    {
        [Header("Properties")]
        [Range(0.1f, 1f)]
        public float resolutionScale = .5f;

        [Range(0f, 1f)]
        public float intensity = 1f;

        [Range(0f, 1f)]
        public float blurWidth = .85f;
    }

    class LightScatteringPass : ScriptableRenderPass
    {
        private RTHandle occluders = k_CameraTarget;
        private readonly float resolutionScale;
        private readonly float intensity;
        private readonly float blurWidth;
        private readonly Material occludersMaterial;
        private readonly List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();
        private FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        private readonly Material radialBlurMaterial;
        private RTHandle cameraColorTarget;

        public LightScatteringPass(VolumetricLightScatteringSettings settings)
        {
            occluders = RTHandles.Alloc("_OccludersMap", name: "_OccludersMap");
            //occluders = k_CameraTarget;
            resolutionScale = settings.resolutionScale;
            intensity = settings.intensity;
            blurWidth = settings.blurWidth;
            occludersMaterial = new Material(Shader.Find("Hidden/RW/UnlitColor"));

            shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            shaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));

            radialBlurMaterial = new Material(Shader.Find("Hidden/RW/RadialBlur"));
        }

        public void Setup(RTHandle cameraColorTarget)
        {
            this.cameraColorTarget = cameraColorTarget;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            cameraTextureDescriptor.depthBufferBits = 0;

            cameraTextureDescriptor.width = Mathf.RoundToInt(cameraTextureDescriptor.width * resolutionScale);
            cameraTextureDescriptor.height = Mathf.RoundToInt(cameraTextureDescriptor.height * resolutionScale);

            RenderingUtils.ReAllocateIfNeeded(ref occluders, cameraTextureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name : occluders.name);
            //cmd.GetTemporaryRT(Shader.PropertyToID(occluders.name), cameraTextureDescriptor, FilterMode.Bilinear);

            ConfigureTarget(occluders);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //if (renderingData.cameraData.isPreviewCamera)
            //    return;

            if (!occludersMaterial || !radialBlurMaterial)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, new ProfilingSampler("VolumetricLightScattering")))
            {
                //
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Camera camera = renderingData.cameraData.camera;
                context.DrawSkybox(camera);

                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, SortingCriteria.CommonOpaque);
                drawingSettings.overrideMaterial = occludersMaterial;

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

                //
                Transform sunTransform = RenderSettings.sun.transform;
                Vector3 sunDirectionWorldSpace = RenderSettings.sun.transform.forward;
                Vector3 cameraPositionWorldSpace = camera.transform.position;
                Vector3 sunPositionWorldSpace = cameraPositionWorldSpace + sunDirectionWorldSpace;
                Vector3 sunPositionViewportSpace = camera.WorldToViewportPoint(sunPositionWorldSpace);

                radialBlurMaterial.SetVector("_Center", new Vector4(sunPositionViewportSpace.x, sunPositionViewportSpace.y, 0, 0));
                radialBlurMaterial.SetFloat("_Intensity", intensity);
                radialBlurMaterial.SetFloat("_BlurWidth", blurWidth);

                //Blit(cmd, occluders, cameraColorTarget, radialBlurMaterial);
                cmd.Blit(occluders, cameraColorTarget, radialBlurMaterial, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cameraColorTarget = null;
            //cmd.ReleaseTemporaryRT(Shader.PropertyToID(occluders.name));
        }

        void Dispose()
        {
            occluders?.Release();
        }
    }

    LightScatteringPass mScriptablePass;
    public VolumetricLightScatteringSettings settings = new VolumetricLightScatteringSettings();

    public override void Create()
    {
        mScriptablePass = new LightScatteringPass(settings);
        mScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(mScriptablePass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        mScriptablePass.Setup(renderer.cameraColorTargetHandle);
    }
}
