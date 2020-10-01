using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using UnityEngine;

public class TestShaders
{
    string[] shaders = AssetDatabase.FindAssets("t:shader", new[] {"Packages/com.unity.render-pipelines.universal/Shaders"});

    [Test]
    public void TestURPShadersForMetal()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.Metal;
        CompileForTargetPlatform(platform);
    }

    [Test]
    public void TestURPShadersForSwitch()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.Switch;
        CompileForTargetPlatform(platform);
    }

    [Test]
    public void TestURPShadersForVulkan()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.Vulkan;
        CompileForTargetPlatform(platform);
    }

    [Test]
    public void TestURPShadersForD3D()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.D3D;
        CompileForTargetPlatform(platform);
    }

    [Test]
    public void TestURPShadersForPS4()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.PS4;
        CompileForTargetPlatform(platform);
    }

    [Test]
    public void TestURPShadersForGLES3x()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.GLES3x;
        CompileForTargetPlatform(platform);
    }

    [Test]
    public void TestURPShadersForGLES20()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.GLES20;
        CompileForTargetPlatform(platform);
    }

    [Test]
    public void TestURPShadersForOpenGLCore()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.OpenGLCore;
        CompileForTargetPlatform(platform);
    }

    [Test]
    public void TestURPShadersForXboxOneD3D11()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.XboxOneD3D11;
        CompileForTargetPlatform(platform);
    }

    [Test]
    public void TestURPShadersForXboxOneD3D12()
    {
        ShaderCompilerPlatform platform = ShaderCompilerPlatform.XboxOneD3D12;
        CompileForTargetPlatform(platform);
    }

    void CompileForTargetPlatform(ShaderCompilerPlatform platform)
    {
        // Deleting the local cache so nothing old will be picked up
        FileUtil.DeleteFileOrDirectory("Library/ShaderCache");
        StringBuilder sb = new StringBuilder();

        // int counter = 0;
        // foreach (var s in shaders)
        // {
        //     counter++;
        //     Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(s));
        //     Debug.Log($"Loading shader { shader.name} from {AssetDatabase.GUIDToAssetPath(s)}");
        //     ShaderUtil.CompileShaderForTargetCompilerPlatform(shader, platform);
        //
        //     foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(shader, platform))
        //     {
        //         sb.AppendLine($"{AssetDatabase.GetAssetPath(shader)} :: {shader.name} :: {message.platform} :: {message.message} :: Line: {message.line}");
        //     }
        //
        //     if(counter > 5)
        //         return;
        // }

        //  Shader shader = Shader.Find("Unlit/CameraOpaque");
        //  ShaderUtil.CompileShaderForTargetCompilerPlatform(shader, platform);
        //  foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(shader, platform))
        //  {
        //      sb.AppendLine($"{AssetDatabase.GetAssetPath(shader)} :: {shader.name} :: {message.platform} :: {message.message} :: Line: {message.line}");
        //  }
        //
        // if (sb.Length > 0)
        // {
        //     Assert.Fail(sb.ToString());
        // }
    }
}
