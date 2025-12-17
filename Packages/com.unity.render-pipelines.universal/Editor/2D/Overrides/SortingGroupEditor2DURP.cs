using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor
{
    [CustomEditor(typeof(SortingGroup))]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [CanEditMultipleObjects]
    internal class SortingGroupEditor2DURP : SortingGroupEditor
    {
        private enum SortType
        {
            Default,
            SortAtRoot,
            Sort3DAs2D
        }

        private static class GUIStyles
        {
            public static GUIContent Default = EditorGUIUtility.TrTextContent("Sorting Type",
               "Default sorting based on sorting layer and sorting order.");

            public static GUIContent sortAtRootStyle = EditorGUIUtility.TrTextContent("Sorting Type",
                "Ignores all parent Sorting Groups and sorts at the root level against other Sorting Groups and Renderers");

            public static GUIContent sort3DAs2D = EditorGUIUtility.TrTextContent("Sorting Type",
                "Clears z values on 3D meshes affected by a Sorting Group allowing them to sort with other 2D objects and Sort 3D as 2D sorting groups. This option also enables Sort At Root");
        }

        private SerializedProperty m_Sort3DAs2D;
        private SortType m_SortType;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Sort3DAs2D = serializedObject.FindProperty("m_Sort3DAs2D");

            // Initialize m_SortType
            m_SortType = m_Sort3DAs2D.boolValue ? SortType.Sort3DAs2D : m_SortAtRoot.boolValue ? SortType.SortAtRoot : SortType.Default;
        }

        void OnInspectorGUIFor2D()
        {
            serializedObject.Update();

            SortingLayerEditorUtility.RenderSortingLayerFields(m_SortingOrder, m_SortingLayerID);

            var prevSortType = m_SortType;
            var label = m_Sort3DAs2D.boolValue ? GUIStyles.sort3DAs2D : m_SortAtRoot.boolValue ? GUIStyles.sortAtRootStyle : GUIStyles.Default;
            m_SortType = (SortType)EditorGUILayout.EnumPopup(label, m_SortType);

            if (prevSortType != m_SortType)
            {
                switch (m_SortType)
                {
                    case SortType.SortAtRoot:
                        m_SortAtRoot.boolValue = true;
                        m_Sort3DAs2D.boolValue = false;
                        break;

                    case SortType.Sort3DAs2D:
                        m_SortAtRoot.boolValue = true;
                        m_Sort3DAs2D.boolValue = true;
                        break;

                    default:
                        m_SortAtRoot.boolValue = false;
                        m_Sort3DAs2D.boolValue = false;
                        break;
                }
            }

            foreach (var target in targets)
            {
                SortingGroup sortingGroup = (SortingGroup)target;
                GameObject go = sortingGroup.gameObject;
                go.TryGetComponent(out RenderAs2D renderAs2D);

                if (sortingGroup.sort3DAs2D)
                {
                    if (renderAs2D != null && !renderAs2D.IsOwner(sortingGroup))
                    {
                        DestroyImmediate(renderAs2D, true);
                        renderAs2D = null;
                    }

                    if (renderAs2D == null)
                    {
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/RenderAs2D-Flattening.mat");
                        renderAs2D = go.AddComponent<RenderAs2D>();
                        renderAs2D.Init(sortingGroup);
                        renderAs2D.material = mat;
                        EditorUtility.SetDirty(go);
                    }
                }
                else
                {
                    if (renderAs2D != null)
                    {
                        DestroyImmediate(renderAs2D, true);
                        renderAs2D = null;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            var rpAsset = UniversalRenderPipeline.asset;
            if (rpAsset != null && (rpAsset.scriptableRenderer is Renderer2D))
                OnInspectorGUIFor2D();
            else
                base.OnInspectorGUI();
        }
    }
}
