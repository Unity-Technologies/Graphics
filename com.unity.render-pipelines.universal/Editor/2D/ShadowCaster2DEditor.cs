using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    internal class ShadowCasterPath : ScriptablePath
    {
        internal Bounds GetBounds()
        {
            ShadowCaster2D shadowCaster = (ShadowCaster2D)owner;
            Renderer m_Renderer = shadowCaster.GetComponent<Renderer>();
            if (m_Renderer != null)
            {
                return m_Renderer.bounds;
            }
            else
            {
                Collider2D collider = shadowCaster.GetComponent<Collider2D>();
                if (collider != null)
                    return collider.bounds;
            }

            return new Bounds(shadowCaster.transform.position, shadowCaster.transform.lossyScale);
        }

        public override void SetDefaultShape()
        {
            Clear();
            Bounds bounds = GetBounds();

            AddPoint(new ControlPoint(bounds.min));
            AddPoint(new ControlPoint(new Vector3(bounds.min.x, bounds.max.y)));
            AddPoint(new ControlPoint(bounds.max));
            AddPoint(new ControlPoint(new Vector3(bounds.max.x, bounds.min.y)));

            base.SetDefaultShape();
        }
    }


    [CustomEditor(typeof(ShadowCaster2D))]
    [CanEditMultipleObjects]
    internal class ShadowCaster2DEditor : PathComponentEditor<ShadowCasterPath>
    {
        [EditorTool("Edit Shadow Caster Shape", typeof(ShadowCaster2D))]
        class ShadowCaster2DShadowCasterShapeTool : ShadowCaster2DShapeTool {};

        private static class Styles
        {
            public static GUIContent shadowMode = EditorGUIUtility.TrTextContent("Use Renderer Silhouette", "When this and Self Shadows are enabled, the Renderer's silhouette is considered part of the shadow. When this is enabled and Self Shadows disabled, the Renderer's silhouette is excluded from the shadow.");
            public static GUIContent selfShadows = EditorGUIUtility.TrTextContent("Self Shadows", "When enabled, the Renderer casts shadows on itself.");
            public static GUIContent castsShadows = EditorGUIUtility.TrTextContent("Casts Shadows", "Specifies if this renderer will cast shadows");
            public static GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply shadows to the specified sorting layers.");
        }

        SerializedProperty m_UseRendererSilhouette;
        SerializedProperty m_CastsShadows;
        SerializedProperty m_SelfShadows;
        SerializedProperty m_ReceivesShadows;


        SortingLayerDropDown m_SortingLayerDropDown;


        public void OnEnable()
        {
            m_UseRendererSilhouette = serializedObject.FindProperty("m_UseRendererSilhouette");
            m_SelfShadows = serializedObject.FindProperty("m_SelfShadows");
            m_CastsShadows = serializedObject.FindProperty("m_CastsShadows");

            m_SortingLayerDropDown = new SortingLayerDropDown();
            m_SortingLayerDropDown.OnEnable(serializedObject, "m_ApplyToSortingLayers");
        }

        public void ShadowCaster2DSceneGUI()
        {
            ShadowCaster2D shadowCaster = target as ShadowCaster2D;

            Transform t = shadowCaster.transform;
            Vector3[] shape = shadowCaster.shapePath;
            Handles.color = Color.white;

            for (int i = 0; i < shape.Length - 1; ++i)
            {
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(shape[i]), t.TransformPoint(shape[i + 1]) });
            }

            if (shape.Length > 1)
                Handles.DrawAAPolyLine(4, new Vector3[] { t.TransformPoint(shape[shape.Length - 1]), t.TransformPoint(shape[0]) });
        }

        public void ShadowCaster2DInspectorGUI<T>() where T : ShadowCaster2DShapeTool
        {
            DoEditButton<T>(PathEditorToolContents.icon, "Edit Shape");
            DoPathInspector<T>();
            DoSnappingInspector<T>();
        }

        public void OnSceneGUI()
        {
            if (m_CastsShadows.boolValue)
                ShadowCaster2DSceneGUI();
        }

        public bool HasRenderer()
        {
            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ShadowCaster2D shadowCaster = (ShadowCaster2D)targets[i];
                    Renderer renderer = shadowCaster.GetComponent<Renderer>();
                    if (renderer != null)
                        return true;
                }
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(!HasRenderer()))  // Done to support multiedit
            {
                EditorGUILayout.PropertyField(m_UseRendererSilhouette, Styles.shadowMode);
            }

            EditorGUILayout.PropertyField(m_CastsShadows, Styles.castsShadows);
            EditorGUILayout.PropertyField(m_SelfShadows, Styles.selfShadows);

            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.sortingLayerPrefixLabel, null);

            if (m_CastsShadows.boolValue)
                ShadowCaster2DInspectorGUI<ShadowCaster2DShadowCasterShapeTool>();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
