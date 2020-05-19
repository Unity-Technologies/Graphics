using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class WriteNormals : CustomPass
{
    public Material material = null;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
    {
        var normal = GetNormalBuffer();
        HDUtils.DrawFullScreen(cmd, material, normal);
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}