using System;
using System.Text;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
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
            quitDebug.AppendLine($"{SceneManager.GetActiveScene().name} RP asset change: {( (currentRP)?currentRP.name:"null" )} => {renderPipelineAsset.name}");

            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
        }

#if UNITY_EDITOR
        //Change game view size
        GameViewUtils.SetTestGameViewSize( ImageComparisonSettings.TargetWidth, ImageComparisonSettings.TargetHeight );
#endif
    }

    static StringBuilder quitDebug = new StringBuilder();

    void OnApplicationQuit()
    {
        if (quitDebug.Length == 0) return;

        Debug.Log($"Scenes that needed to change the RP asset:{Environment.NewLine}{quitDebug.ToString()}");

        quitDebug.Clear();
    }

    public void ApplyResolution()
    {
        Screen.SetResolution(
            ImageComparisonSettings.TargetWidth,
            ImageComparisonSettings.TargetHeight,
            Screen.fullScreenMode
            );

#if UNITY_EDITOR
        //Change game view size
        GameViewUtils.SetTestGameViewSize( ImageComparisonSettings.TargetWidth, ImageComparisonSettings.TargetHeight );
#endif
    }
}
