using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class HDRP_TestSettings : GraphicsTestSettings
{
	public UnityEngine.Events.UnityEvent doBeforeTest;
	public int captureFramerate = 0;
	public int waitFrames = 0;

    public RenderPipelineAsset renderPipelineAsset;

    void Awake()
    {
        if (renderPipelineAsset == null)
        {
            Debug.LogWarning("No RenderPipelineAsset has been assigned in the test settings. This may result in a wrong test.");
            return;
        }

        var currentRP = GraphicsSettings.renderPipelineAsset;

        if (currentRP != renderPipelineAsset)
        {
            quitDebug.AppendLine($"{SceneManager.GetActiveScene().name} RP asset change: {( (currentRP)?AssetDatabase.GetAssetPath(currentRP):"null" )} => {AssetDatabase.GetAssetPath(renderPipelineAsset)}");

            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
        }
    }

    static StringBuilder quitDebug = new StringBuilder();

    void OnApplicationQuit()
    {
        if (quitDebug.Length == 0) return;

        Debug.Log($"Scenes that needed to change the RP asset:{Environment.NewLine}{quitDebug.ToString()}");

        quitDebug.Clear();
    }
}
