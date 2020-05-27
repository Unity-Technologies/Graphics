using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.IO;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>
    /// Class containing a collection of Environment
    /// </summary>
    [HelpURL(Documentation.baseURLHDRP + Documentation.releaseVersion + Documentation.subURL + "Environment-Library" + Documentation.endURL)]
    public class EnvironmentLibrary : ScriptableObject
    {
        [field: SerializeField]
        List<Environment> environments { get; set; } = new List<Environment>();

        /// <summary>
        /// Number of elements in the collection
        /// </summary>
        public int Count => environments.Count;
        /// <summary>
        /// Indexer giving access to contained Environment
        /// </summary>
        public Environment this[int index] => environments[index];

        /// <summary>
        /// Create a new empty Environment at the end of the collection
        /// </summary>
        /// <returns>The created Environment</returns>
        public Environment Add()
        {
            Undo.SetCurrentGroupName("Add Environment");
            int group = Undo.GetCurrentGroup();

            Environment environment = ScriptableObject.CreateInstance<Environment>();
            environment.name = "New Environment";
            Undo.RegisterCreatedObjectUndo(environment, "Add Environment");

            Undo.RecordObject(this, "Add Environment");
            environments.Add(environment);

            // Store this new environment as a subasset so we can reference it safely afterwards.
            AssetDatabase.AddObjectToAsset(environment, this);

            Undo.CollapseUndoOperations(group);

            // Force save / refresh. Important to do this last because SaveAssets can cause effect to become null!
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return environment;
        }

        /// <summary>
        /// Remove Environment of the collection at given index
        /// </summary>
        /// <param name="index">Index where to remove Environment</param>
        public void Remove(int index)
        {
            Undo.SetCurrentGroupName("Remove Environment");
            int group = Undo.GetCurrentGroup();

            Environment environment = environments[index];
            Undo.RecordObject(this, "Remove Environment");
            environments.RemoveAt(index);
            Undo.DestroyObjectImmediate(environment);

            Undo.CollapseUndoOperations(group);

            // Force save / refresh
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Duplicate the Environment at given index and add it at the end of the Collection
        /// </summary>
        /// <param name="fromIndex">Index where to take data for duplication</param>
        /// <returns>The created Environment</returns>
        public Environment Duplicate(int fromIndex)
        {
            Environment environment = ScriptableObject.CreateInstance<Environment>();
            Environment environmentToCopy = environments[fromIndex];
            environmentToCopy.CopyTo(environment);

            Undo.RegisterCreatedObjectUndo(environment, "Duplicate Environment");

            environments.Add(environment);

            // Store this new environment as a subasset so we can reference it safely afterwards.
            AssetDatabase.AddObjectToAsset(environment, this);

            // Force save / refresh. Important to do this last because SaveAssets can cause effect to become null!
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return environment;
        }

        /// <summary>
        /// Compute position of given Environment in the collection
        /// </summary>
        /// <param name="environment">Environment to look at</param>
        /// <returns>Index of the searched environment. If not found, -1.</returns>
        public int IndexOf(Environment environment)
            => environments.IndexOf(environment);
    }

    [CustomEditor(typeof(EnvironmentLibrary))]
    class EnvironmentLibraryEditor : Editor
    {
        VisualElement root;

        public sealed override VisualElement CreateInspectorGUI()
        {
            var library = target as EnvironmentLibrary;
            root = new VisualElement();

            Button open = new Button(() =>
            {
                if (!LookDev.open)
                    LookDev.Open();
                LookDev.currentContext.UpdateEnvironmentLibrary(library);
                LookDev.currentEnvironmentDisplayer.Repaint();
            })
            {
                text = "Open in LookDev window"
            };

            root.Add(open);
            return root;
        }

        // Don't use ImGUI
        public sealed override void OnInspectorGUI() { }
    }

    class EnvironmentLibraryCreator : ProjectWindowCallback.EndNameEditAction
    {
        ObjectField m_Field = null;

        public void SetField(ObjectField field)
            => m_Field = field;

        public override void Cancelled(int instanceId, string pathName, string resourceFile)
            => m_Field = null;

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var newAsset = CreateInstance<EnvironmentLibrary>();
            newAsset.name = Path.GetFileName(pathName);
            AssetDatabase.CreateAsset(newAsset, pathName);
            ProjectWindowUtil.ShowCreatedAsset(newAsset);
            if (m_Field != null)
                m_Field.value = newAsset;
            m_Field = null;
        }

        [MenuItem("Assets/Create/LookDev/Environment Library", priority = 2000)]
        static void Create()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<EnvironmentLibraryCreator>(), "New EnvironmentLibrary.asset", icon, null);
        }

        public static void CreateAndAssignTo(ObjectField field)
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            var assetCreator = ScriptableObject.CreateInstance<EnvironmentLibraryCreator>();
            assetCreator.SetField(field);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, "New EnvironmentLibrary.asset", icon, null);
        }
    }

    static class EnvironmentLibraryLoader
    {
        static Action<UnityEngine.Object> LoadCallback(Action onUpdate)
        {
            return (UnityEngine.Object newLibrary) =>
            {
                LookDev.currentContext.UpdateEnvironmentLibrary(newLibrary as EnvironmentLibrary);
                onUpdate?.Invoke();
            };
        }
    }
}
