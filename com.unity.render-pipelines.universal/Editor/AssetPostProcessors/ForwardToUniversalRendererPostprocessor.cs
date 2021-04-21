using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class ForwardToUniversalRendererPostprocessor : AssetPostprocessor
    {
        static bool firstTimeUpgrade = true;
        static int editedAssetsCount = 0;
        static string fwdRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("de640fe3d0db1804a85f9fc8f5cadab6");
        static Object fwdRendererScriptObj;
        static string stdRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("f971995892640ec4f807ef396269e91e");
        static Object stdRendererScriptObj;

        static void SetupScriptObjects()
        {
            //Gets the ForwardRendererData.cs file
            if(!fwdRendererScriptObj) fwdRendererScriptObj = AssetDatabase.LoadAssetAtPath(fwdRendererScriptFilePath, typeof(Object));

            //Gets the UniversalRendererData.cs file
            if(!stdRendererScriptObj) stdRendererScriptObj = AssetDatabase.LoadAssetAtPath(stdRendererScriptFilePath, typeof(Object));
        }

        static void UpgradeAsset(Object rendererData, string rendererDataPath, Object fwdRendererScriptObj, Object stdRendererScriptObj)
        {
            if (rendererData == null) return;
            SerializedObject so = new SerializedObject(rendererData);

            //Double check to see if it's using ForwardRendererData
            SerializedProperty scriptProperty = so.FindProperty("m_Script");
            if (scriptProperty.objectReferenceValue == fwdRendererScriptObj)
            {
                //Change the script to use UniversalRendererData
                so.Update();
                scriptProperty.objectReferenceValue = stdRendererScriptObj;
                so.ApplyModifiedProperties();

                //Re-import asset
                //This prevents the "Importer(NativeFormatImporter) generated inconsistent result" warning
                AssetDatabase.ImportAsset(rendererDataPath);

                editedAssetsCount++;
            }
        }

        static void IterateSubAssets(string rendererDataPath)
        {
            SetupScriptObjects();

            //To prevent infinite importing loop caused by corrupted assets which are created from old bugs e.g. case 1214779
            Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(rendererDataPath);
            for (int j = 0; j < subAssets.Length; j++)
            {
                //Upgrade subAssets
                UpgradeAsset(subAssets[j], rendererDataPath, fwdRendererScriptObj, stdRendererScriptObj);
            }

            //Upgrade the main Asset
            Object rendererData = AssetDatabase.LoadAssetAtPath(rendererDataPath, typeof(Object));
            UpgradeAsset(rendererData, rendererDataPath, fwdRendererScriptObj, stdRendererScriptObj);
        }

        static void UpgradeAllAssets()
        {
            //Gets all the ForwardRendererData Assets in project
            string[] allRenderers = AssetDatabase.FindAssets("t:ForwardRendererData", null);

            //If there is no ForwardRendererData assets in project then skip the following
            if (allRenderers.Length == 0) return;

            for (int i = 0; i < allRenderers.Length; i++)
            {
                string rendererDataPath = AssetDatabase.GUIDToAssetPath(allRenderers[i]);
                IterateSubAssets(rendererDataPath);
            }
        }

        static void RefreshPipelineAssets()
        {
            //If there is no asset upgraded then we don't need to do the following
            if (editedAssetsCount == 0) return;

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

            //Reset counter
            editedAssetsCount = 0;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if(firstTimeUpgrade)
            {
                UpgradeAllAssets();
                firstTimeUpgrade = false;
            }
            else
            {
                for(int i=0; i<importedAssets.Length; i++)
                {
                    IterateSubAssets(importedAssets[i]);
                }
            }
            RefreshPipelineAssets();
        }
    }
}
