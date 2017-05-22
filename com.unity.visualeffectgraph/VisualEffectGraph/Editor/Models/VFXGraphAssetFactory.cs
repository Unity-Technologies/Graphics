using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    public class VFXAssetFactory
    {
        [MenuItem("Assets/Create/VFXAsset", priority = 301)]
        private static void MenuCreateVFXAsset()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateVFXAsset>(), "New VFXAsset.asset", null, null);
        }

        internal static VFXAsset CreateVFXAssetAtPath(string path)
        {
            VFXAsset asset = new VFXAsset();
            asset.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }

    internal class DoCreateVFXAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            VFXAsset asset = VFXAssetFactory.CreateVFXAssetAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }
}
