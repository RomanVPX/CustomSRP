using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

// PIPELINE ADD PASS --------------------------------------------------------------------------------------------
// This pass does a image effect that Albedo + Emission = final color
public partial class SRP0802_RenderGraph
{
    private Material m_Material;

    private class SRP0802_AddPassData
    {
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
    }

    private void Render_SRP0802_AddPass(RenderGraph graph, TextureHandle albedo, TextureHandle emission)
    {
        if(m_Material == null)
        {
            m_Material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/CustomSRP/SRP0802_RenderGraph/FinalColor"));
        }

        using RenderGraphBuilder builder = graph.AddRenderPass("Add Pass", out SRP0802_AddPassData passData, new ProfilingSampler("Add Pass Profiler"));
        //Textures
        passData.m_Albedo = builder.ReadTexture(albedo);
        passData.m_Emission = builder.ReadTexture(emission);

        //Builder
        builder.SetRenderFunc((SRP0802_AddPassData data, RenderGraphContext context) =>
        {
            m_Material.SetTexture("_CameraAlbedoTexture", data.m_Albedo);
            m_Material.SetTexture("_CameraEmissionTexture", data.m_Emission);
            context.cmd.Blit(null, BuiltinRenderTextureType.CameraTarget, m_Material);
        });
    }
}
