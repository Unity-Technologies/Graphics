using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    [InitializeOnLoad]
    class RendererDataPatcher
    {
        #region Universal Renderer Patcher Params

        static bool firstTimeUpgrade = true;
        static int editedAssetsCount = 0;
        static string fwdRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("f971995892640ec4f807ef396269e91e");  //ForwardRendererData.cs
        static Object fwdRendererScriptObj;
        static string universalRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("de640fe3d0db1804a85f9fc8f5cadab6");  //UniversalRendererData.cs
        static Object universalRendererScriptObj;

        #endregion

        static RendererDataPatcher()
        {
            UniversalRendererPatcher();
        }

        #region Universal Renderer Patcher

        /// <summary>
        /// Patcher for fixing UniversalRendererData Scriptable Objects that were made in Unity 2021.2 Alpha & Beta
        /// </summary>
        static void UniversalRendererPatcher()
        {
            string[] allRenderers = AssetDatabase.FindAssets("t:ForwardRendererData glob:\"**/*.asset\"", null);

            foreach (var t in allRenderers)
            {
                string rendererDataPath = AssetDatabase.GUIDToAssetPath(t);
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(rendererDataPath);
                if (assetType == null) continue;
                if (assetType.ToString().Contains("Universal.ForwardRendererData"))
                {
                    IterateSubAssets(rendererDataPath);
                }
            }

            //Putting in delayCall will make sure AssetDatabase is ready for the FindAssets search below
            EditorApplication.delayCall += () =>
            {
                //This helps to scan the RendererData Assets which are subAssets caused by case 1214779
                if (firstTimeUpgrade)
                {
                    string[] allRenderers = AssetDatabase.FindAssets("t:ForwardRendererData glob:\"**/*.asset\"", null);
                    foreach (var t in allRenderers)
                    {
                        string rendererDataPath = AssetDatabase.GUIDToAssetPath(t);
                        IterateSubAssets(rendererDataPath);
                    }
                    firstTimeUpgrade = false;
                }
                //If there is no asset upgraded then we don't need to do the following
                if (editedAssetsCount == 0) return;

                //Gets all the UniversalRenderPipeline Assets in project
                string[] allURPassets =
                    AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset glob:\"**/*.asset\"", null);
                foreach (var t in allURPassets)
                {
                    string pipelineAssetPath = AssetDatabase.GUIDToAssetPath(t);
                    Object pipelineAsset = AssetDatabase.LoadAssetAtPath(pipelineAssetPath, typeof(Object));
                    SerializedObject soAsset = new SerializedObject(pipelineAsset);
                    //Make some changes on the Pipeline assets
                    //If we don't do this, the pipeline assets won't recognise the changed renderer "type", so will give "no Default Renderer asset" error and nothing will render
                    SerializedProperty scriptPropertyAsset = soAsset.FindProperty("m_RequireDepthTexture");
                    soAsset.Update();

                    if (scriptPropertyAsset == null) continue;

                    bool tmp = scriptPropertyAsset.boolValue;
                    scriptPropertyAsset.boolValue = !scriptPropertyAsset.boolValue;  //make the changes
                    soAsset.ApplyModifiedProperties();
                    scriptPropertyAsset.boolValue = tmp;  //revert the changes
                    soAsset.ApplyModifiedProperties();
                    EditorUtility.SetDirty(pipelineAsset);
                }
                //Reset counter and register state
                Debug.LogWarning($"URP Renderer(s) have been upgraded, please remember to save your project to write the upgrade to disk.");
                editedAssetsCount = 0;
            };
        }

        static bool UpgradeAsset(Object rendererData, string rendererDataPath)
        {
            if (rendererData == null) return false;

            //Gets the script file objects
            if (!fwdRendererScriptObj) fwdRendererScriptObj = AssetDatabase.LoadAssetAtPath(fwdRendererScriptFilePath, typeof(Object));
            if (!universalRendererScriptObj) universalRendererScriptObj = AssetDatabase.LoadAssetAtPath(universalRendererScriptFilePath, typeof(Object));

            //Double check to see if it's using ForwardRendererData
            SerializedObject so = new SerializedObject(rendererData);
            SerializedProperty scriptProperty = so.FindProperty("m_Script");

            if (scriptProperty == null || scriptProperty.objectReferenceValue != fwdRendererScriptObj) return false;
            //Write debug warning out before renderer is switched and data is nullified
            Debug.LogWarning($"Upgraded renderer {rendererData.name} at {rendererDataPath}.\nYou should only see this if you are upgrading from 2021.2 Alpha/Beta cycle.");
            //Change the script to use UniversalRendererData
            so.Update();
            scriptProperty.objectReferenceValue = universalRendererScriptObj;
            so.ApplyModifiedProperties();

            //Re-import asset
            //This prevents the "Importer(NativeFormatImporter) generated inconsistent result" warning
            AssetDatabase.ImportAsset(rendererDataPath);

            editedAssetsCount++;
            return true;
        }

        static void IterateSubAssets(string assetPath)
        {
            //To prevent infinite importing loop caused by corrupted assets which are created from old bugs e.g. case 1214779
            Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            if (subAssets.Any(t => UpgradeAsset(t, assetPath))) return;

            //Upgrade the main Asset
            Object rendererData = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
            UpgradeAsset(rendererData, assetPath);
        }

        #endregion
    }
}
