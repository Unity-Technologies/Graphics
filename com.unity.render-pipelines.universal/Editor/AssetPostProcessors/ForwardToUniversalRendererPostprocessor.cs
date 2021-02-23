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

        //Gets the ForwardRendererData.cs file
        string fwdRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("de640fe3d0db1804a85f9fc8f5cadab6");
        var fwdRendererScriptObj = AssetDatabase.LoadAssetAtPath(fwdRendererScriptFilePath, typeof(Object));

        int noOfFwdRendererData = 0;

        //Gets all the Renderer Assets in project
        Object[] allRenderers = Resources.FindObjectsOfTypeAll(typeof(ScriptableRendererData));
        for (int i = 0; i < allRenderers.Length; i++)
        {
            SerializedObject so = new SerializedObject(allRenderers[i]);
            SerializedProperty scriptProperty = so.FindProperty("m_Script");
            so.Update();
            //check if the script is using ForwardRendererData
            if (scriptProperty.objectReferenceValue.Equals(fwdRendererScriptObj))
            {
                //change the script to use UniversalRendererData
                scriptProperty.objectReferenceValue = stdRendererScriptObj;
                so.ApplyModifiedProperties();
                noOfFwdRendererData++;
            }
        }

        //If there is no ForwardRendererData in project then we don't need to do the following
        if (noOfFwdRendererData == 0) return;

        //Gets all the Pipeline Assets in project
        Object[] allRPassets = Resources.FindObjectsOfTypeAll(typeof(UniversalRenderPipelineAsset));
        for (int i = 0; i < allRPassets.Length; i++)
        {
            //Make some changes on the Pipeline assets
            //If we don't do this, the pipeline assets won't recognise the changed renderer "type", so will complain about there is no Default Renderer asset
            SerializedObject soAsset = new SerializedObject(allRPassets[i]);
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
