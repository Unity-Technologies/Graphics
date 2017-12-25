using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CustomEditor(typeof(GPUCopyAsset))]
public class GPUCopyAssetEditor : Editor
{
    GPUCopyAsset m_Target;

    void OnEnable()
    {
        m_Target = (GPUCopyAsset)target;
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Generate"))
        {
            var assetpath = AssetDatabase.GetAssetPath(target);
            var dirpath = Path.GetDirectoryName(assetpath);
            var targetpathcs = dirpath + "/GPUCopy.cs";
            var targetpathcc =dirpath + "/GPUCopy.compute";
            string cc, cs;
            m_Target.Generate(out cc, out cs);

            File.WriteAllText(targetpathcc, cc);
            File.WriteAllText(targetpathcs, cs);

            AssetDatabase.StartAssetEditing();
            AssetDatabase.ImportAsset(targetpathcc);
            AssetDatabase.ImportAsset(targetpathcs);
            AssetDatabase.StopAssetEditing();
        }
        base.OnInspectorGUI();
    }
}
