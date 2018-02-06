using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor;
using UnityEditor.VFX;

namespace UnityEditor
{
    [InitializeOnLoad]
    public class VFXAssetEditorUtility
    {
        [MenuItem("GameObject/Effects/Visual Effect", false, 10)]
        public static void CreateVisualEffectGameObject(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Visual Effect");
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            var vfxComp = go.AddComponent<VFXComponent>();

            if (Selection.activeObject != null && Selection.activeObject is VFXAsset)
            {
                vfxComp.vfxAsset = Selection.activeObject as VFXAsset;
                vfxComp.startSeed = (uint)Random.Range(int.MinValue, int.MaxValue);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        }

        [MenuItem("Assets/Create/VFX", false, 306)]
        public static void CreateVFXAsset()
        {
            VFXAsset asset = new VFXAsset();

            ProjectWindowUtil.CreateAsset(asset, "New VFX.vfx");
        }
    }
}
