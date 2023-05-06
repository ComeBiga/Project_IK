using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumetricLightFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        public Stage stage;
        public float intensity = 1;
        public float scattering = 0;
        public float steps = 25;
        public float maxDistance = 75;
        public float jitter = 250;
    }

    public Settings settings = new Settings();

    private class Pass : ScriptableRenderPass
    {
        public Settings settings;
        private RenderTargetIdentifier source;
        RTHandle tempTexture;

        private string profilerTag;

        public void Dispose()
        {
            tempTexture?.Release();
        }

        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
        }

        public Pass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cameraTextureDescriptor.colorFormat = RenderTextureFormat.R16;
            cameraTextureDescriptor.msaaSamples = 1;

            cameraTextureDescriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, cameraTextureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempTexture");
            //cmd.GetTemporaryRT(Shader.PropertyToID(tempTexture.name), cameraTextureDescriptor);
            
            ConfigureTarget(tempTexture);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            cmd.Clear();

            try
            {
                settings.material.SetFloat("_Scattering", settings.scattering);
                settings.material.SetFloat("_Steps", settings.steps);
                settings.material.SetFloat("_JitterVolumetric", settings.jitter);
                settings.material.SetFloat("_MaxDistance", settings.maxDistance);
                settings.material.SetFloat("_Intensity", settings.intensity);

                cmd.Blit(source, tempTexture);
                cmd.Blit(tempTexture, source, settings.material, 0);

                context.ExecuteCommandBuffer(cmd);
            }
            catch
            {
                Debug.LogError("Error");
            }

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }

    Pass pass;
    RTHandle renderTextureHandle;

    public override void Create()
    {
        pass = new Pass("Volumetric Light");
        name = "Volumetric Light";
        pass.settings = settings;
        pass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //var cameraColorTargetIdent = renderer.cameraColorTarget;
        //var cameraColorTargetIdent = renderer.cameraColorTargetHandle;
        //pass.Setup(cameraColorTargetIdent);
        renderer.EnqueuePass(pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        var cameraColorTargetIdent = renderer.cameraColorTargetHandle;
        pass.Setup(cameraColorTargetIdent);
    }
}
