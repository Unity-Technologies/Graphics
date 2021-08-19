 using UnityEditor;
 using UnityEngine;
 using UnityEngine.Rendering;
 using UnityEngine.Rendering.Universal;

 namespace UnityEditor.Rendering.Universal
 {
     class ForwardToUniversalRendererPostprocessor : AssetPostprocessor
     {
         static bool firstTimeUpgrade = true;
         static bool registeredRendererUpdate = false;
         static int editedAssetsCount = 0;
         static string fwdRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("f971995892640ec4f807ef396269e91e"); //ForwardRendererData.cs
         static Object fwdRendererScriptObj;
         static string stdRendererScriptFilePath = AssetDatabase.GUIDToAssetPath("de640fe3d0db1804a85f9fc8f5cadab6"); //UniversalRendererData.cs
         static Object stdRendererScriptObj;

         static void UpgradeAsset(Object rendererData, string rendererDataPath)
         {
             if (rendererData == null) return;

             //Gets the script file objects
             if (!fwdRendererScriptObj) fwdRendererScriptObj = AssetDatabase.LoadAssetAtPath(fwdRendererScriptFilePath, typeof(Object));
             if (!stdRendererScriptObj) stdRendererScriptObj = AssetDatabase.LoadAssetAtPath(stdRendererScriptFilePath, typeof(Object));

             //Double check to see if it's using ForwardRendererData
             SerializedObject so = new SerializedObject(rendererData);
             SerializedProperty scriptProperty = so.FindProperty("m_Script");
             Debug.LogWarning($"upgraded renderer {rendererData.name} at {rendererDataPath}");

             if (scriptProperty == null || scriptProperty.objectReferenceValue != fwdRendererScriptObj) return;
             //Change the script to use UniversalRendererData
             so.Update();
             scriptProperty.objectReferenceValue = stdRendererScriptObj;
             so.ApplyModifiedProperties();
             EditorUtility.SetDirty(rendererData);

             //Re-import asset
             //This prevents the "Importer(NativeFormatImporter) generated inconsistent result" warning
             AssetDatabase.ImportAsset(rendererDataPath);

             editedAssetsCount++;
         }

         static void IterateSubAssets(string assetPath)
         {
             //To prevent infinite importing loop caused by corrupted assets which are created from old bugs e.g. case 1214779
             Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
             foreach (var t in subAssets)
             {
                 //Upgrade subAssets
                 UpgradeAsset(t, assetPath);
                 return;
             }

             //Upgrade the main Asset
             Object rendererData = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
             UpgradeAsset(rendererData, assetPath);
         }

         [InitializeOnLoadMethod]
         static void RegisterUpgraderReimport()
         {
             //Putting in delayCall will make sure AssetDatabase is ready for the FindAssets search below
             EditorApplication.delayCall += () => {
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
                 string[] allURPassets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset glob:\"**/*.asset\"", null);
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
                     scriptPropertyAsset.boolValue = !scriptPropertyAsset.boolValue; //make the changes
                     soAsset.ApplyModifiedProperties();
                     scriptPropertyAsset.boolValue = tmp; //revert the changes
                     soAsset.ApplyModifiedProperties();
                     EditorUtility.SetDirty(pipelineAsset);
                 }
                          //Reset counter and register state
                 editedAssetsCount = 0;
                 registeredRendererUpdate = false;
             };
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
             Debug.LogWarning($"kicked off method");

             //Opening some projects e.g. URP Template relies on this interation for doing the asset upgrade.
             //This also makes sure the RendererData will be re-upgraded again if the uppgraded changes are discarded using source control.
             foreach (var t in importedAssets)
             {
                 var assetType = AssetDatabase.GetMainAssetTypeAtPath(t);
                 if (assetType == null) continue;
                 if (assetType.ToString().Contains("Universal.ForwardRendererData"))
                 {
                     IterateSubAssets(t);
                 }
             }

             //If there are assets being upgraded then we need to trigger an update on the Pipeline assets to avoid "no Default Renderer asset" error and making rendering fine again.
             //However at this moment the Pipeline assets are not yet updated, so the error might still happen in the case of discarded upgrade changes, but it won't harm rendering
             //This makes sure we re-register the delayCall only once
             if (!registeredRendererUpdate && editedAssetsCount > 0)
             {
                 RegisterUpgraderReimport();
                 registeredRendererUpdate = true;
             }
         }
     }
 }
