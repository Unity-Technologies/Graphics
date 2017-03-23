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
	public class VFXGraphAssetFactory
    {
        [MenuItem("Assets/Create/VFXGraphAsset", priority = 301)]
        private static void MenuCreateVFXGraphAsset()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateVFXGraphAsset>(), "New VFXGraph.asset", null, null);
        }

        internal static VFXGraphAsset CreateVFXGraphAssetAtPath(string path)
        {
            VFXGraphAsset asset = ScriptableObject.CreateInstance<VFXGraphAsset>();
            asset.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }

    internal class DoCreateVFXGraphAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            VFXGraphAsset asset = VFXGraphAssetFactory.CreateVFXGraphAssetAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }
}