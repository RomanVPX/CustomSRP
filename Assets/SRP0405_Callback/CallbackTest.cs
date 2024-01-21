using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CallbackTest : MonoBehaviour
{
    public Mesh mesh;
    public Material material;

    private void OnEnable()
    {
        SRP0405Instance.afterSkybox += MyAfterSkybox;
        SRP0405Instance.afterOpaqueObject += MyAfterOpaque;
        SRP0405Instance.afterTransparentObject += MyAfterTransparent;
    }

    private void OnDisable()
    {
        CleanUp();
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        SRP0405Instance.afterSkybox -= MyAfterSkybox;
        SRP0405Instance.afterOpaqueObject -= MyAfterOpaque;
        SRP0405Instance.afterTransparentObject -= MyAfterTransparent;
    }

    private void MyAfterSkybox(Camera cam, ScriptableRenderContext context)
    {
        //Debug.Log("after skybox is called");
    }

    private void MyAfterOpaque(Camera cam, ScriptableRenderContext context)
    {
        //Debug.Log("after opaque is called");
    }

    private void MyAfterTransparent(Camera cam, ScriptableRenderContext context)
    {
        //Debug.Log("after transparent is called");
        var cmd = new CommandBuffer();
        cmd.DrawMesh(mesh,Matrix4x4.identity,material,0,0);
        context.ExecuteCommandBuffer(cmd);
    }
}
