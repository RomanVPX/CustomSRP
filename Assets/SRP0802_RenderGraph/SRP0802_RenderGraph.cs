using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE MAIN --------------------------------------------------------------------------------------------
public partial class SRP0802_RenderGraph : RenderPipeline
{
    private RenderGraph graph = new RenderGraph("SRP0802_RenderGraphPass");

    public SRP0802_RenderGraph()
    {
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context,cameras);

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context,camera);

            //Culling
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams)) continue;
            CullingResults cull = context.Cull(ref cullingParams);

            //Camera setup some builtin variables e.g. camera projection matrices etc
            context.SetupCameraProperties(camera);

            //Execute graph 
            CommandBuffer cmd_RenderGraph = CommandBufferPool.Get("ExecuteRenderGraph");
            var rgParams = new RenderGraphParameters()
            {
                executionName = "SRP0802_RenderGraph_Execute",
                commandBuffer = cmd_RenderGraph,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount
            };

            using (graph.RecordAndExecute(rgParams))
            {
                SRP0802_BasePassData basePassData = Render_SRP0802_BasePass(camera, graph, cull); //BasePass
                Render_SRP0802_AddPass(graph, basePassData.m_Albedo, basePassData.m_Emission); //AddPass
            }

            context.ExecuteCommandBuffer(cmd_RenderGraph);
            CommandBufferPool.Release(cmd_RenderGraph);
            
            //Submit camera rendering
            context.Submit();
            EndCameraRendering(context,camera);
        }
        
        graph.EndFrame();
        EndFrameRendering(context,cameras);
    }

    protected override void Dispose(bool disposing)
    {
        graph.Cleanup();
        graph = null;
    }
}
