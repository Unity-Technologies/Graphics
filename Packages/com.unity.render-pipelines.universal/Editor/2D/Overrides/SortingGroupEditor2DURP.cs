using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor
{
    [CustomEditor(typeof(UnityEngine.Rendering.SortingGroup))]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [CanEditMultipleObjects]
    internal class SortingGroupEditor2DURP : Editor
    {
        private static class Styles
        {
            public static GUIContent sort3DAs2D = EditorGUIUtility.TrTextContent("Sort 3D as 2D", "Clears z values on 3D meshes affected by a Sorting Group allowing them to sort with other 2D objects and Sort 3D as 2D sorting groups.");
        }

        private SerializedProperty m_SortingOrder;
        private SerializedProperty m_SortingLayerID;
        private SerializedProperty m_Sort3DAs2D;

        public virtual void OnEnable()
        {
            alwaysAllowExpansion = true;
            m_SortingOrder = serializedObject.FindProperty("m_SortingOrder");
            m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
            m_Sort3DAs2D = serializedObject.FindProperty("m_Sort3DAs2D");
        }

        public RenderAs2D TryToFindCreatedRenderAs2D(SortingGroup sortingGroup)
        {
            RenderAs2D[] renderAs2Ds = sortingGroup.GetComponents<RenderAs2D>();
            foreach (RenderAs2D renderAs2D in renderAs2Ds)
            {
                if (renderAs2D.IsOwner(sortingGroup))
                    return renderAs2D;
            }

            return null;
        }

        bool DrawToggleWithLayout(bool flatten, GUIContent content)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            var boolValue = EditorGUI.Toggle(rect, content, flatten);
            return boolValue;
        }

        void DirtyScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        void RenderSort3DAs2D()
        {
            EditorGUILayout.PropertyField(m_Sort3DAs2D, Styles.sort3DAs2D);
            foreach (var target in targets)
            {
                SortingGroup sortingGroup = (SortingGroup)target;
                if (sortingGroup.sort3DAs2D)
                {
                    GameObject go = sortingGroup.gameObject;
                    go.TryGetComponent<RenderAs2D>(out RenderAs2D renderAs2D);

                    if (renderAs2D != null && !renderAs2D.IsOwner(sortingGroup))
                    {
                        Component.DestroyImmediate(renderAs2D, true);
                        renderAs2D = null;
                    }

                    if(renderAs2D == null)
                    {
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/RenderAs2D-Flattening.mat");
                        renderAs2D = go.AddComponent<RenderAs2D>();
                        renderAs2D.Init(sortingGroup);
                        renderAs2D.material = mat;
                        EditorUtility.SetDirty(sortingGroup.gameObject);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rpAsset = UniversalRenderPipeline.asset;
            if (rpAsset != null && (rpAsset.scriptableRenderer is Renderer2D))
            {
                SortingLayerEditorUtility.RenderSortingLayerFields(m_SortingOrder, m_SortingLayerID);
                RenderSort3DAs2D();
            }
            else
                base.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
