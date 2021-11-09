using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScriptableRendererData), true)]
    public class ScriptableRendererDataEditor : Editor
    {
        private ScriptableRendererFeatureEditor m_rendererFeatureEditor;

        public void OnEnable()
        {
            m_rendererFeatureEditor = new ScriptableRendererFeatureEditor(this);
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_rendererFeatureEditor.DrawRendererFeatures();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
