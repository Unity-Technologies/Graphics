using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class ForwardToUniversalRendererPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        //Gets the UniversalRendererData.cs file
        string stdRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("f971995892640ec4f807ef396269e91e");
        var stdRendererScriptObj = AssetDatabase.LoadAssetAtPath(stdRendererScriptFilePath, typeof(Object));

        //Gets all the ForwardRendererData Assets in project
        string[] allRenderers = AssetDatabase.FindAssets("t:ForwardRendererData", null);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            string rendererDataPath = AssetDatabase.GUIDToAssetPath(allRenderers[i]);
            Object rendererData = AssetDatabase.LoadAssetAtPath(rendererDataPath, typeof(Object));
            SerializedObject so = new SerializedObject(rendererData);

            //change the script to use UniversalRendererData
            SerializedProperty scriptProperty = so.FindProperty("m_Script");
            so.Update();
            scriptProperty.objectReferenceValue = stdRendererScriptObj;
            so.ApplyModifiedProperties();

            //Save and re-import asset
            //This prevents the "Importer(NativeFormatImporter) generated inconsistent result" warning
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(rendererDataPath);
        }

        //If there is no ForwardRendererData in project then we don't need to do the following
        if (allRenderers.Length == 0) return;

        //Gets all the UniversalRenderPipeline Assets in project
        string[] allURPassets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", null);
        for (int i = 0; i < allURPassets.Length; i++)
        {
            string pipelineAssetPath = AssetDatabase.GUIDToAssetPath(allURPassets[i]);
            Object pipelineAsset = AssetDatabase.LoadAssetAtPath(pipelineAssetPath, typeof(Object));
            SerializedObject soAsset = new SerializedObject(pipelineAsset);

            //Make some changes on the Pipeline assets
            //If we don't do this, the pipeline assets won't recognise the changed renderer "type", so will complain about there is no Default Renderer asset
            SerializedProperty scriptPropertyAsset = soAsset.FindProperty("m_RequireDepthTexture");
            soAsset.Update();
            if (scriptPropertyAsset != null)
            {
                bool tmp = scriptPropertyAsset.boolValue;
                scriptPropertyAsset.boolValue = !scriptPropertyAsset.boolValue; //make the changes
                soAsset.ApplyModifiedProperties();
                scriptPropertyAsset.boolValue = tmp; //revert the changes
                soAsset.ApplyModifiedProperties();
            }
        }
    }
}
