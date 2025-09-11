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
            public static GUIContent flattenGroup = EditorGUIUtility.TrTextContent("Sort 3D as 2D", "Clears z values on 3D meshes affected by a Sorting Group allowing them to sort with other 2D objects and Sort 3D as 2D sorting groups.");
        }

        private SerializedProperty m_SortingOrder;
        private SerializedProperty m_SortingLayerID;

        public virtual void OnEnable()
        {
            alwaysAllowExpansion = true;
            m_SortingOrder   = serializedObject.FindProperty("m_SortingOrder");
            m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
        }

        public RenderAs2D TryToFindCreatedRenderAs2D(SortingGroup sortingGroup)
        {
            RenderAs2D[] renderAs2Ds = sortingGroup.GetComponents<RenderAs2D>();
            foreach(RenderAs2D renderAs2D in renderAs2Ds)
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

        void RenderFlattening()
        {

            bool tryToFlatten = TryToFindCreatedRenderAs2D(serializedObject.targetObject as SortingGroup) != null;
            bool result = DrawToggleWithLayout(tryToFlatten, Styles.flattenGroup);

            if (tryToFlatten != result)
            {
                tryToFlatten = result;
                foreach (Object targetObject in serializedObject.targetObjects)
                {
                    SortingGroup sortingGroup = targetObject as SortingGroup;
                    RenderAs2D renderAs2D = TryToFindCreatedRenderAs2D(sortingGroup);

                    if (tryToFlatten)
                    {
                        if (!renderAs2D)
                        {
                            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/RenderAs2D-Flattening.mat");
                            RenderAs2D newRenderAs2D = sortingGroup.gameObject.AddComponent<RenderAs2D>();
                            newRenderAs2D.Init(sortingGroup);
                            newRenderAs2D.material = mat;
                            newRenderAs2D.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                            DirtyScene();
                        }
                    }
                    else
                    {
                        if (renderAs2D)
                        {
                            Component.DestroyImmediate(renderAs2D);
                            DirtyScene();
                        }
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
                RenderFlattening();
            }
            else
                base.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
