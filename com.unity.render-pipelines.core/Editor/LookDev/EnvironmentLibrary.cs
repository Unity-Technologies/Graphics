using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>
    /// Class containing a collection of Environment
    /// </summary>
    public class EnvironmentLibrary : BaseEnvironmentLibrary
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
            Environment environment = ScriptableObject.CreateInstance<Environment>();
            environment.name = "New Environment";
            Undo.RegisterCreatedObjectUndo(environment, "Add Environment");

            environments.Add(environment);

            // Store this new environment as a subasset so we can reference it safely afterwards.
            AssetDatabase.AddObjectToAsset(environment, this);

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
            Environment environment = environments[index];
            Undo.RecordObject(this, "Remove Environment");
            environments.RemoveAt(index);
            Undo.DestroyObjectImmediate(environment);

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
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var newAsset = CreateInstance<EnvironmentLibrary>();
            newAsset.name = Path.GetFileName(pathName);
            AssetDatabase.CreateAsset(newAsset, pathName);
            ProjectWindowUtil.ShowCreatedAsset(newAsset);
        }

        [MenuItem("Assets/Create/LookDev/Environment Library", priority = 2000)]
        public static void Create()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<EnvironmentLibraryCreator>(), "EnvironmentLibrary.asset", icon, null);
        }
    }

    static class EnvironmentLibraryLoader
    {
        public static void Load(Action onInspectorRedrawRequested)
        {
            UnityEngine.Object target = LookDev.currentContext.environmentLibrary;
            UIElementObjectSelectorWorkaround.Show(target, typeof(EnvironmentLibrary), LoadCallback(onInspectorRedrawRequested));
        }

        static Action<UnityEngine.Object> LoadCallback(Action onUpdate)
        {
            return (UnityEngine.Object newLibrary) =>
            {
                LookDev.currentContext.UpdateEnvironmentLibrary(newLibrary as EnvironmentLibrary);
                onUpdate?.Invoke();
            };
        }


        // As in UIElement.ObjectField we cannot support cancel when closing window
        static class UIElementObjectSelectorWorkaround
        {
            static Action<UnityEngine.Object, Type, Action<UnityEngine.Object>> ShowObjectSelector;

            static UIElementObjectSelectorWorkaround()
            {
                Type playerSettingsType = typeof(PlayerSettings);
                Type objectSelectorType = playerSettingsType.Assembly.GetType("UnityEditor.ObjectSelector");
                var instanceObjectSelectorInfo = objectSelectorType.GetProperty("get", BindingFlags.Static | BindingFlags.Public);
                var showInfo = objectSelectorType.GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(UnityEngine.Object), typeof(Type), typeof(SerializedProperty), typeof(bool), typeof(List<int>), typeof(Action<UnityEngine.Object>), typeof(Action<UnityEngine.Object>) }, null);
                var objectSelectorVariable = Expression.Variable(objectSelectorType, "objectSelector");
                var objectParameter = Expression.Parameter(typeof(UnityEngine.Object), "unityObject");
                var typeParameter = Expression.Parameter(typeof(Type), "type");
                var onChangedObjectParameter = Expression.Parameter(typeof(Action<UnityEngine.Object>), "onChangedObject");
                var showObjectSelectorBlock = Expression.Block(
                    new[] { objectSelectorVariable },
                    Expression.Assign(objectSelectorVariable, Expression.Call(null, instanceObjectSelectorInfo.GetGetMethod())),
                    Expression.Call(objectSelectorVariable, showInfo, objectParameter, typeParameter, Expression.Constant(null, typeof(SerializedProperty)), Expression.Constant(false), Expression.Constant(null, typeof(List<int>)), Expression.Constant(null, typeof(Action<UnityEngine.Object>)), onChangedObjectParameter)
                    );
                var showObjectSelectorLambda = Expression.Lambda<Action<UnityEngine.Object, Type, Action<UnityEngine.Object>>>(showObjectSelectorBlock, objectParameter, typeParameter, onChangedObjectParameter);
                ShowObjectSelector = showObjectSelectorLambda.Compile();
            }

            public static void Show(UnityEngine.Object obj, Type type, Action<UnityEngine.Object> onObjectChanged)
            {
                ShowObjectSelector(obj, type, onObjectChanged);
            }
        }
    }
}
