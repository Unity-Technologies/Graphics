using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[CustomEditor(typeof(SetupSceneForRenderPipelineTest))]
public class SetupSceneForRenderPipelineTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var sceneSetup = target as SetupSceneForRenderPipelineTest;
        if (sceneSetup == null)
            return;

        if (GUILayout.Button("Set renderpipe"))
        {
            GraphicsSettings.renderPipelineAsset = sceneSetup.renderPipeline;
        }
    }
}
