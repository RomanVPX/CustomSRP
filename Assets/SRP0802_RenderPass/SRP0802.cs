using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0802 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0802", priority = 1)]
    private static void CreateSRP0802()
    {
        var instance = CreateInstance<SRP0802>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0802.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0802Instance();
    }
}

public class SRP0802Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName1 = new ShaderTagId("SRP0802_Pass1"); //The shader pass tag just for SRP0802
    private static readonly ShaderTagId m_PassName2 = new ShaderTagId("SRP0802_Pass2"); //The shader pass tag just for SRP0802

    private const RenderTextureFormat ColorFormat = RenderTextureFormat.Default;
    private static readonly int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    private static readonly RenderTargetIdentifier m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
    private const int DepthBufferBits = 24;

    private AttachmentDescriptor m_Albedo_Desc = new AttachmentDescriptor(ColorFormat);
    private AttachmentDescriptor m_Emission_Desc = new AttachmentDescriptor(ColorFormat);
    private AttachmentDescriptor m_Output_Desc = new AttachmentDescriptor(ColorFormat);
    private AttachmentDescriptor m_Depth_Desc = new AttachmentDescriptor(RenderTextureFormat.Depth);

    public SRP0802Instance()
    {
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context,cameras);

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context,camera);

            //Culling
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams))
            { continue; }

            CullingResults cull = context.Cull(ref cullingParams);

            //Camera setup some builtin variables e.g. camera projection matrices etc
            context.SetupCameraProperties(camera);

            //Get the setting from camera component
            bool drawSkyBox = camera.clearFlags == CameraClearFlags.Skybox? true : false;
            bool clearDepth = camera.clearFlags == CameraClearFlags.Nothing? false : true;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color? true : false;

            //Color Texture Descriptor
            var colorRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight)
            {
                colorFormat = ColorFormat,
                depthBufferBits = DepthBufferBits,
                sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear),
                msaaSamples = 1,
                enableRandomWrite = false
            };

            //Get Temp Texture for Color Texture
            var cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";
            cmdTempId.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);
            cmdTempId.SetRenderTarget(m_ColorRT); //so that result won't flip
            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            var drawSettings1 = new DrawingSettings(m_PassName1, sortingSettings);
            var drawSettings2 = new DrawingSettings(m_PassName2, sortingSettings);
            var filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Native Arrays for Attachments
            var renderPassAttachments = new NativeArray<AttachmentDescriptor>(4, Allocator.Temp);
            renderPassAttachments[0] = m_Albedo_Desc;
            renderPassAttachments[1] = m_Emission_Desc;
            renderPassAttachments[2] = m_Output_Desc;
            renderPassAttachments[3] = m_Depth_Desc;
            var renderPassColorAttachments = new NativeArray<int>(2, Allocator.Temp);
            renderPassColorAttachments[0] = 0;
            renderPassColorAttachments[1] = 1;
            var renderPassOutputAttachments = new NativeArray<int>(1, Allocator.Temp);
            renderPassOutputAttachments[0] = 2;

            //Clear Attachments
            m_Output_Desc.ConfigureTarget(m_ColorRT, false, true);
            m_Output_Desc.ConfigureClear(new Color(0.0f, 0.0f, 0.0f, 0.0f),1,0);
            m_Albedo_Desc.ConfigureClear(camera.backgroundColor,1,0);
            m_Emission_Desc.ConfigureClear(new Color(0.0f, 0.0f, 0.0f, 0.0f),1,0);
            m_Depth_Desc.ConfigureClear(new Color(),1,0);
            
            //More clean to use ScopedRenderPass instead of BeginRenderPass+EndRenderPass
            using ( context.BeginScopedRenderPass(camera.pixelWidth, camera.pixelHeight, 1, renderPassAttachments, 3) )
            {
                //Output to Albedo & Emission
                using (context.BeginScopedSubPass(renderPassColorAttachments, false))
                {
                    //Opaque objects
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    drawSettings1.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    context.DrawRenderers(cull, ref drawSettings1, ref filterSettings);

                    //Transparent objects
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    drawSettings1.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    context.DrawRenderers(cull, ref drawSettings1, ref filterSettings);
                }
                //Read from Albedo & Emission, then output to Output
                using (context.BeginScopedSubPass(renderPassOutputAttachments, renderPassColorAttachments))
                {
                    //Skybox
                    if(drawSkyBox)
                    {
                        context.DrawSkybox(camera);
                    }

                    //Opaque objects
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    drawSettings2.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    context.DrawRenderers(cull, ref drawSettings2, ref filterSettings);

                    //Transparent objects
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    drawSettings2.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    context.DrawRenderers(cull, ref drawSettings2, ref filterSettings);
                }
            }

            //Blit To Camera so that the CameraTarget has content and make sceneview works
            var cmd = new CommandBuffer();
            cmd.name = "Cam:"+camera.name+" BlitToCamera";
            cmd.Blit(m_ColorRT,BuiltinRenderTextureType.CameraTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release(); 

            //CleanUp Texture
            var cmdClean = new CommandBuffer();
            cmdClean.name = "("+camera.name+")"+ "Clean Up";
            cmdClean.ReleaseTemporaryRT(m_ColorRTid);
            context.ExecuteCommandBuffer(cmdClean);
            cmdClean.Release();

            //Submit the CommandBuffers
            context.Submit();

            //CleanUp NativeArrays
            renderPassAttachments.Dispose();
            renderPassColorAttachments.Dispose();
            renderPassOutputAttachments.Dispose();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}
