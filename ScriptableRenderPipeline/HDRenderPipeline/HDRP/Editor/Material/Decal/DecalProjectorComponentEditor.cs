using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(DecalProjectorComponent))]
    public class DecalProjectorComponentEditor : Editor
    {
        private MaterialEditor m_MaterialEditor = null;
        private DecalProjectorComponent m_DecalProjectorComponent = null;

        private void OnEnable()
        {
            // Create an instance of the MaterialEditor
            m_DecalProjectorComponent = (DecalProjectorComponent)target;
            m_MaterialEditor = (MaterialEditor)CreateEditor(m_DecalProjectorComponent.Mat);
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (m_MaterialEditor != null)
            {
                // Draw the material's foldout and the material shader field
                // Required to call m_MaterialEditor.OnInspectorGUI ();
                m_MaterialEditor.DrawHeader();

                // We need to prevent the user to edit default decal materials
                bool isDefaultMaterial = false;
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    isDefaultMaterial = m_DecalProjectorComponent.Mat == hdrp.GetDefaultDecalMaterial();
                }
                using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
                {
                    // Draw the material properties
                    // Works only if the foldout of m_MaterialEditor.DrawHeader () is open
                    m_MaterialEditor.OnInspectorGUI();
                }
            }
        }
    }
}
