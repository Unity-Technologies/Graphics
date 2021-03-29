using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.MeshDecal;

namespace UnityEditor.Rendering.MeshDecal
{
    [CustomEditor(typeof(MeshDecalProjector))]
    public class MeshDecalProjector_Editor : DecalEditorBase
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Separator();

            if (GUILayout.Button("Build Decal"))
                (target as MeshDecalProjector).GenerateDecal();

            if (GUILayout.Button("Force Update Decal in Manager"))
                MeshDecalProjectorsManager.RegisterDecalProjector((target as MeshDecalProjector));
        }
    }
}
