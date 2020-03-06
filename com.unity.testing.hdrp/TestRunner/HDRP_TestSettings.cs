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
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class HDRP_TestSettings : GraphicsTestSettings
{
	public UnityEngine.Events.UnityEvent doBeforeTest;
	public int captureFramerate = 0;
	public int waitFrames = 0;
    public bool xrCompatible = true;

    [UnityEngine.Range(1.0f, 10.0f)]
    public float xrThresholdMultiplier = 1.0f;

    public bool checkMemoryAllocation = true;

    public RenderPipelineAsset renderPipelineAsset;

    void Awake()
    {
        // Built-in font shaders are incompatible with XR, replace them with a ShaderGraph version
        if (XRSystem.testModeEnabled && xrCompatible)
            doBeforeTest.AddListener(ReplaceBuiltinFontShaders);

        if (renderPipelineAsset == null)
        {
            Debug.LogWarning("No RenderPipelineAsset has been assigned in the test settings. This may result in a wrong test.");
            return;
        }

        var currentRP = GraphicsSettings.renderPipelineAsset;

        if (currentRP != renderPipelineAsset)
        {
            quitDebug.AppendLine($"{SceneManager.GetActiveScene().name} RP asset change: {((currentRP==null)?"null": currentRP.name)} => {renderPipelineAsset.name}");

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

    void ReplaceBuiltinFontShaders()
    {
#if UNITY_EDITOR
        var fontMaterialSG = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.testing.hdrp/Fonts/Font Material SG.mat");
        if (fontMaterialSG != null)
        {
            foreach (var textMesh in GameObject.FindObjectsOfType<TextMesh>())
            {
                var textMeshRenderer = textMesh.gameObject.GetComponent<MeshRenderer>();

                if (!textMeshRenderer.material.shader.name.StartsWith("Shader Graphs"))
                {
                    // From Unity source: Runtime\Resources\Assets\DefaultResources\Font.shader
                    var fontTexture = textMeshRenderer.material.GetTexture("_MainTex");
                    var fontColor = textMeshRenderer.material.GetColor("_Color");

                    textMeshRenderer.material = fontMaterialSG;
                    textMeshRenderer.material.SetTexture("_MainTex", fontTexture);
                    textMeshRenderer.material.SetColor("_Color", fontColor);

                    textMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }
#endif
    }
}
