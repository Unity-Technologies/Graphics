using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

namespace UnityEditor
{
    [InitializeOnLoad]
    public class VisualEffectAssetEditorUtility
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
            VisualEffectAsset asset = new VisualEffectAsset();

            VFXViewController controller = VFXViewController.GetController(asset);
            controller.useCount++;

            VisualEffectAsset template = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(templatePath + "/" + templateAssetName);
            if (template != null)
            {
                VFXViewController templateController = VFXViewController.GetController(template);
                templateController.useCount++;

                var data = VFXCopyPaste.SerializeElements(templateController.allChildren, Rect.zero);

                VFXCopyPaste.UnserializeAndPasteElements(controller, Vector2.zero, data);

                templateController.useCount--;
            }
            controller.graph.RecompileIfNeeded();

            ProjectWindowUtil.CreateAsset(asset, "New VFX.vfx");

            controller.useCount--;
        }
    }
}
