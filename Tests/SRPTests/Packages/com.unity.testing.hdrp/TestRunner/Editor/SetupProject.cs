using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

public static class SetupProject
{
    public static Dictionary<string, string> RPAssets = new Dictionary<string, string>
    {
        { "deferred", "Assets/GraphicTests/Common/RP_Assets/HDRP_Test_Def.asset" },
        { "deferred-depth-prepass", "Assets/GraphicTests/Common/RP_Assets/HDRP_Test_Def_DepthPrepass.asset" },
        { "deferred-depth-prepass-alpha-only", "Assets/GraphicTests/Common/RP_Assets/HDRP_Test_Def_DepthPrepass_AlphaOnly.asset" },
        { "forward", "Assets/GraphicTests/Common/RP_Assets/HDRP_Test_Fwd.asset" }
    };

    public static Dictionary<string, Action> Options = new Dictionary<string, Action>
    {
        { "deferred", () => {SetRPAsset("deferred");} },
        { "deferred-depth-prepass", () => {SetRPAsset("deferred-depth-prepass");} },
        { "deferred-depth-prepass-alpha-only", () => {SetRPAsset("deferred-depth-prepass-alpha-only");} },
        { "forward", () => {SetRPAsset("forward");} }
    };

    public static void SetRPAsset(string rpAssetIdentifier)
    {
        RenderPipelineAsset rpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(RPAssets[rpAssetIdentifier]);

        GraphicsSettings.defaultRenderPipeline = rpAsset;
    }

    public static void ApplySettings()
    {
        var args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            Action action;
            if (Options.TryGetValue(arg, out action))
                action();
        }
        UnityEditor.TestTools.Graphics.SetupProject.ApplySettings();
    }

    [MenuItem("Graphic Tests/Project Setup/RP: Deferred")]
    public static void SetRP_Deferred()
    {
        SetRPAsset("deferred");
    }

    [MenuItem("Graphic Tests/Project Setup/RP: Deferred Depth Prepass")]
    public static void SetRP_DeferredDepthPrepass()
    {
        SetRPAsset("deferred-depth-prepass");
    }

    [MenuItem("Graphic Tests/Project Setup/RP: Deferred Depth Prepass Alpha Only")]
    public static void SetRP_DeferredDepthPrepassAlphaOnly()
    {
        SetRPAsset("deferred-depth-prepass-alpha-only");
    }

    [MenuItem("Graphic Tests/Project Setup/RP: Forward")]
    public static void SetRP_Forward()
    {
        SetRPAsset("forward");
    }
}
