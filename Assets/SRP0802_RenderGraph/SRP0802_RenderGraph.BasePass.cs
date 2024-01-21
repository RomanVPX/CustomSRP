using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE BASE PASS --------------------------------------------------------------------------------------------
// This pass renders objects into 2 RenderTargets:
// Albedo - grey texture and the skybox
// Emission - animated color
public partial class SRP0802_RenderGraph
{
    private ShaderTagId m_PassName1 = new ShaderTagId("SRP0802_Pass1"); //The shader pass tag just for SRP0802

    public class SRP0802_BasePassData
    {
        public RendererListHandle m_RenderList_Opaque;
        public RendererListHandle m_RenderList_Transparent;
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
        public TextureHandle m_Depth;
    }

    private TextureHandle CreateColorTexture(RenderGraph graph, Camera camera, string name)
    {
        bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

        //Texture description
        var colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight)
        {
            colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default,colorRT_sRGB),
            depthBufferBits = 0,
            msaaSamples = MSAASamples.None,
            enableRandomWrite = false,
            clearBuffer = true,
            clearColor = Color.black,
            name = name
        };

        return graph.CreateTexture(colorRTDesc);
    }

    private TextureHandle CreateDepthTexture(RenderGraph graph, Camera camera)
    {
        bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

        //Texture description
        var colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight)
        {
            colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Depth,colorRT_sRGB),
            depthBufferBits = DepthBits.Depth24,
            msaaSamples = MSAASamples.None,
            enableRandomWrite = false,
            clearBuffer = true,
            clearColor = Color.black,
            name = "Depth"
        };

        return graph.CreateTexture(colorRTDesc);
    }

    public SRP0802_BasePassData Render_SRP0802_BasePass(Camera camera, RenderGraph graph, CullingResults cull)
    {
        using (RenderGraphBuilder builder = graph.AddRenderPass( "Base Pass", out SRP0802_BasePassData passData, new ProfilingSampler("Base Pass Profiler" ) ) )
        {
            //Textures - Multi-RenderTarget
            TextureHandle albedo_handle = CreateColorTexture(graph,camera,"Albedo");
            passData.m_Albedo = builder.UseColorBuffer(albedo_handle,0);
            TextureHandle emission_handle = CreateColorTexture(graph,camera,"Emission");
            passData.m_Emission = builder.UseColorBuffer(emission_handle,1);
            TextureHandle depth_handle = CreateDepthTexture(graph,camera);
            passData.m_Depth = builder.UseDepthBuffer(depth_handle, DepthAccess.Write);

            //Renderers
            var rendererDesc_base_opaque = new UnityEngine.Rendering.RendererUtils.RendererListDesc(m_PassName1,cull,camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque
            };
            RendererListHandle rHandle_base_opaque = graph.CreateRendererList(rendererDesc_base_opaque);
            passData.m_RenderList_Opaque = builder.UseRendererList(rHandle_base_opaque);

            var rendererDesc_base_transparent = new UnityEngine.Rendering.RendererUtils.RendererListDesc(m_PassName1,cull,camera)
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent
            };
            RendererListHandle rHandle_base_transparent= graph.CreateRendererList(rendererDesc_base_transparent);
            passData.m_RenderList_Transparent = builder.UseRendererList(rHandle_base_transparent);

            //Builder
            builder.SetRenderFunc((SRP0802_BasePassData data, RenderGraphContext context) =>
            {
                //Skybox - this will draw to the first target, i.e. Albedo
                if(camera.clearFlags == CameraClearFlags.Skybox)  {  context.renderContext.DrawSkybox(camera);  }

                CoreUtils.DrawRendererList( context.renderContext, context.cmd, data.m_RenderList_Opaque );
                CoreUtils.DrawRendererList( context.renderContext, context.cmd, data.m_RenderList_Transparent );
            });

            return passData;
        }
    }
}
