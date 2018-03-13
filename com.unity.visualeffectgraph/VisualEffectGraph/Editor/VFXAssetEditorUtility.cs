using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEditor;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

namespace UnityEditor
{
    [InitializeOnLoad]
    public static class VisualEffectAssetEditorUtility
    {
        public const string templatePath = "Assets/VFXEditor/Editor/Templates";

        public const string templateAssetName = "Simple Particle System.vfx";

        [MenuItem("GameObject/Effects/Visual Effect", false, 10)]
        public static void CreateVisualEffectGameObject(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Visual Effect");
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            var vfxComp = go.AddComponent<VisualEffect>();

            if (Selection.activeObject != null && Selection.activeObject is VisualEffectAsset)
            {
                vfxComp.visualEffectAsset = Selection.activeObject as VisualEffectAsset;
                vfxComp.startSeed = (uint)Random.Range(int.MinValue, int.MaxValue);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        }

        [MenuItem("Assets/Create/Visual Effect", false, 306)]
        public static void CreateVisualEffectAsset()
        {
            VisualEffectResource resource = new VisualEffectResource();

            VFXViewController controller = VFXViewController.GetController(resource);
            controller.useCount++;

            var template = VisualEffectResource.GetResourceAtPath(templatePath + "/" + templateAssetName);
            if (template != null)
            {
                VFXViewController templateController = VFXViewController.GetController(template);
                templateController.useCount++;

                var data = VFXCopyPaste.SerializeElements(templateController.allChildren, Rect.zero);

                VFXCopyPaste.UnserializeAndPasteElements(controller, Vector2.zero, data);

                templateController.useCount--;
            }
            controller.graph.RecompileIfNeeded();

            ProjectWindowUtil.CreateAsset(resource, "New VFX.vfx");

            controller.useCount--;
        }

        [MenuItem("VFX Editor/Make All Assets Visible")]
        public static void MakeAllVisible()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeInstanceID);

            if (!string.IsNullOrEmpty(path))
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset != null)
                        asset.hideFlags = HideFlags.None;
                }
            }

            AssetDatabase.ImportAsset(path);
        }
    }
}
