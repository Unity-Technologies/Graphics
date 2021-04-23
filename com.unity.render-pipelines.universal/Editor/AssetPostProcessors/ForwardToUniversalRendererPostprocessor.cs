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
        static string fwdRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("de640fe3d0db1804a85f9fc8f5cadab6"); //ForwardRendererData.cs
        static Object fwdRendererScriptObj;
        static string stdRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("f971995892640ec4f807ef396269e91e"); //UniversalRendererData.cs
        static Object stdRendererScriptObj;

        static void UpgradeAsset(Object rendererData, string rendererDataPath)
        {
            if (rendererData == null) return;
            
            //Gets the ForwardRendererData.cs file
            if(!fwdRendererScriptObj) fwdRendererScriptObj = AssetDatabase.LoadAssetAtPath(fwdRendererScriptFilePath, typeof(Object));

            //Gets the UniversalRendererData.cs file
            if(!stdRendererScriptObj) stdRendererScriptObj = AssetDatabase.LoadAssetAtPath(stdRendererScriptFilePath, typeof(Object));

            //Double check to see if it's using ForwardRendererData
            SerializedObject so = new SerializedObject(rendererData);
            SerializedProperty scriptProperty = so.FindProperty("m_Script");
            if (scriptProperty == null) return;
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

        static void IterateSubAssets(string assetPath)
        {
            //To prevent infinite importing loop caused by corrupted assets which are created from old bugs e.g. case 1214779
            Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            for (int j = 0; j < subAssets.Length; j++)
            {
                //Upgrade subAssets
                UpgradeAsset(subAssets[j], assetPath);
            }

            //Upgrade the main Asset
            Object rendererData = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
            UpgradeAsset(rendererData, assetPath);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            //For the first time get all the ForwardRendererData Assets in project
            //This will make sure opening projects with Library will still have the ForwardRendererData upgraded
            if(firstTimeUpgrade)
            {
                string[] allRenderers = AssetDatabase.FindAssets("t:ForwardRendererData glob:\"**/*.asset\"", null);
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    string rendererDataPath = AssetDatabase.GUIDToAssetPath(allRenderers[i]);
                    IterateSubAssets(rendererDataPath);
                }

                firstTimeUpgrade = false;
            }

            //Iterate any changed assets
            for(int i=0; i<importedAssets.Length; i++)
            {
                if(importedAssets[i].EndsWith(".asset"))
                {
                    IterateSubAssets(importedAssets[i]);
                }
            }

            //If there is no asset upgraded then we don't need to do the following
            if (editedAssetsCount == 0) return;

            //Gets all the UniversalRenderPipeline Assets in project
            string[] allURPassets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset glob:\"**/*.asset\"", null);
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
    }
}