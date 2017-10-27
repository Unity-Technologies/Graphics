using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.IO;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class BC6HTests
{
    // Test BC6H fast encoding
    [Test]
    public void BC6HEncodeFast()
    {
        var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ScriptableRenderPipeline/ScriptableRenderPipeline/Core/Resources/BC6H.compute");
        var sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ScriptableRenderPipeline/Tests/TextureCompressionTests/Editor/Resources/graffiti_shelter_1k.hdr");
        var sourceTextureId = new RenderTargetIdentifier(sourceTexture);


        var b = new BC6H(shader);

        var target = b.InstantiateTarget(sourceTexture.width, sourceTexture.height);
        target.Release();
        target.enableRandomWrite = true;
        target.Create();
        var targetId = new RenderTargetIdentifier(target);

        var cmb = new CommandBuffer { name = "TextureCompressionTests.BC6HEncodeFast" };
        b.EncodeFast(cmb, sourceTextureId, sourceTexture.width, sourceTexture.height, targetId);
        Graphics.ExecuteCommandBuffer(cmb);

        var targetFile = "Assets/ScriptableRenderPipeline/Tests/TextureCompressionTests/Editor/Resources/graffiti_shelter_1k_bc6h.hdr";

        var targetT2D = new Texture2D(target.width, target.height, TextureFormat.RGBAFloat, false, true);
        Graphics.CopyTexture(target, targetT2D);
        var bytes = targetT2D.GetRawTextureData();
        File.WriteAllBytes(targetFile, bytes);

        target.Release();
    }
}
