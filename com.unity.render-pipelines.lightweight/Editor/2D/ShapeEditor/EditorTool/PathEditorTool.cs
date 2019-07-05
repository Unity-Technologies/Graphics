using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.Rendering.LWRP.Path2D.GUIFramework;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal static class PathEditorToolContents
    {
        internal static readonly GUIContent shapeToolIcon = IconContent("ShapeTool", "Start editing the Shape in the Scene View.");
        internal static readonly GUIContent shapeToolPro = IconContent("ShapeToolPro", "Start editing the Shape in the Scene View.");

        internal static GUIContent IconContent(string name, string tooltip = null)
        {
            return new GUIContent(Resources.Load<Texture>(name), tooltip);
        }

        public static GUIContent icon
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return shapeToolPro;

                return shapeToolIcon;
            }
        }
    }

    internal interface IDuringSceneGuiTool
    {
        void DuringSceneGui(SceneView sceneView);
        bool IsAvailable();
    }

    [InitializeOnLoad]
    internal class EditorToolManager
    {
        private static List<IDuringSceneGuiTool> m_Tools = new List<IDuringSceneGuiTool>();

        static EditorToolManager()
        {
            SceneView.duringSceneGui += DuringSceneGui;
        }

        internal static void Add(IDuringSceneGuiTool tool)
        {
            if (!m_Tools.Contains(tool) && tool is EditorTool)
                m_Tools.Add(tool);
        }

        internal static void Remove(IDuringSceneGuiTool tool)
        {
            if (m_Tools.Contains(tool))
                m_Tools.Remove(tool);
        }

        internal static bool IsActiveTool<T>() where T : EditorTool
        {
            return EditorTools.EditorTools.activeToolType.Equals(typeof(T));
        }

        internal static bool IsAvailable<T>() where T : EditorTool
        {
            var tool = GetEditorTool<T>();

            if (tool != null)
                return tool.IsAvailable();

            return false;
        }

        internal static T GetEditorTool<T>() where T : EditorTool
        {
            foreach(var tool in m_Tools)
            {
                if (tool.GetType().Equals(typeof(T)))
                    return tool as T;
            }

            return null;
        }

        private static void DuringSceneGui(SceneView sceneView)
        {
            foreach (var tool in m_Tools)
            {
                if (tool.IsAvailable() && EditorTools.EditorTools.IsActiveTool(tool as EditorTool))
                    tool.DuringSceneGui(sceneView);
            }
        }
    }

    internal abstract class PathEditorTool<T> : EditorTool, IDuringSceneGuiTool where T : ScriptablePath
    {
        private Dictionary<UnityObject, T> m_Paths = new Dictionary<UnityObject, T>();
        private IGUIState m_GUIState = new GUIState();
        private Dictionary<UnityObject, GUISystem> m_GUISystems = new Dictionary<UnityObject, GUISystem>();
        private Dictionary<UnityObject, SerializedObject> m_SerializedObjects = new Dictionary<UnityObject, SerializedObject>();
        private MultipleEditablePathController m_Controller = new MultipleEditablePathController();
        private PointRectSelector m_RectSelector = new PointRectSelector();
        private bool m_IsActive = false;

        internal T[] paths
        {
            get { return m_Paths.Values.ToArray(); }
        }

        public bool enableSnapping
        {
            get { return m_Controller.enableSnapping; }
            set { m_Controller.enableSnapping = value; }
        }

        public override GUIContent toolbarIcon
        {
            get { return PathEditorToolContents.icon; }
        }

        public override bool IsAvailable()
        {
            return targets.Count() > 0;
        }

        public T GetPath(UnityObject targetObject)
        {
            var path = default(T);
            m_Paths.TryGetValue(targetObject, out path);
            return path;
        }

        public void SetPath(UnityObject target)
        {
            var path = GetPath(target);
            path.localToWorldMatrix = Matrix4x4.identity;

            var undoName = Undo.GetCurrentGroupName();
            var serializedObject = GetSerializedObject(target);
            
            serializedObject.UpdateIfRequiredOrScript();

            SetShape(path, serializedObject);

            Undo.SetCurrentGroupName(undoName);
        }

        private void RepaintInspectors()
        {
            var editorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

            foreach (var editorWindow in editorWindows)
            {
                if (editorWindow.titleContent.text == "Inspector")
                    editorWindow.Repaint();
            }
        }

        private void OnEnable()
        {
            m_IsActive = false;
            EditorToolManager.Add(this);

            SetupRectSelector();
            HandleActivation();

            EditorTools.EditorTools.activeToolChanged += HandleActivation;
        }

        private void OnDestroy()
        {
            EditorToolManager.Remove(this);

            EditorTools.EditorTools.activeToolChanged -= HandleActivation;
            UnregisterCallbacks();
        }

        private void HandleActivation()
        {
            if (m_IsActive == false && EditorTools.EditorTools.IsActiveTool(this))
                Activate();
            else if (m_IsActive)
                Deactivate();
        }

        private void Activate()
        {
            m_IsActive = true;
            RegisterCallbacks();
            InitializeCache();
            OnActivate();
        }

        private void Deactivate()
        {
            OnDeactivate();
            DestroyCache();
            UnregisterCallbacks();
            m_IsActive = false;
        }

        private void RegisterCallbacks()
        {
            UnregisterCallbacks();
            Selection.selectionChanged += SelectionChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private void UnregisterCallbacks()
        {
            Selection.selectionChanged -= SelectionChanged;
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        private void DestroyCache()
        {
            foreach (var pair in m_Paths)
            {
                var path = pair.Value;

                if (path != null)
                {
                    Undo.ClearUndo(path);
                    UnityObject.DestroyImmediate(path);
                }
            }
            m_Paths.Clear();
            m_Controller.ClearPaths();
            m_GUISystems.Clear();
            m_SerializedObjects.Clear();
        }

        private void UndoRedoPerformed()
        {
            ForEachTarget((target) =>
            {
                var path = GetPath(target);

                if (!path.modified)
                    InitializePath(target);
            });
        }

        private void SelectionChanged()
        {
            InitializeCache();
        }

        private void PlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += () => { InitializeCache(); }; //HACK: At this point target is null. Let's wait to next frame to refresh.
        }

        private void SetupRectSelector()
        {
            m_RectSelector.onSelectionBegin = BeginSelection;
            m_RectSelector.onSelectionChanged = UpdateSelection;
            m_RectSelector.onSelectionEnd = EndSelection;
        }

        private void ForEachTarget(Action<UnityObject> action)
        {
            foreach(var target in targets)
            {
                if (target == null)
                    continue;

                action(target);
            }
        }

        private void InitializeCache()
        {
            m_Controller.ClearPaths();

            ForEachTarget((target) =>
            {
                var path = GetOrCreatePath(target);
                var pointCount = path.pointCount;

                InitializePath(target);

                if (pointCount != path.pointCount)
                    path.selection.Clear();

                CreateGUISystem(target);

                m_Controller.AddPath(path);
            });
        }

        private void InitializePath(UnityObject target)
        {
            IShape shape = null;
            ControlPoint[] controlPoints = null;

            try
            {
                shape = GetShape(target);
                controlPoints = shape.ToControlPoints();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            var path = GetPath(target);
            path.Clear();

            if (shape != null && controlPoints != null)
            {
                path.localToWorldMatrix = Matrix4x4.identity;
                path.shapeType = shape.type;
                path.isOpenEnded = shape.isOpenEnded;

                foreach (var controlPoint in controlPoints)
                    path.AddPoint(controlPoint);
            }

            Initialize(path, GetSerializedObject(target));
        }

        private T GetOrCreatePath(UnityObject targetObject)
        {
            var path = GetPath(targetObject);

            if (path == null)
            {
                path = ScriptableObject.CreateInstance<T>();
                path.owner = targetObject;
                m_Paths[targetObject] = path;
            }

            return path;
        }

        private GUISystem GetGUISystem(UnityObject target)
        {
            GUISystem guiSystem;
            m_GUISystems.TryGetValue(target, out guiSystem);
            return guiSystem;
        }

        private void CreateGUISystem(UnityObject target)
        {
            var guiSystem = new GUISystem(m_GUIState);
            var view = new EditablePathView();

            view.controller = m_Controller;
            view.Install(guiSystem);

            m_GUISystems[target] = guiSystem;
        }

        private SerializedObject GetSerializedObject(UnityObject target)
        {
            var serializedObject = default(SerializedObject);

            if (!m_SerializedObjects.TryGetValue(target, out serializedObject))
            {
                serializedObject = new SerializedObject(target);
                m_SerializedObjects[target] = serializedObject;
            }

            return serializedObject;
        }

        void IDuringSceneGuiTool.DuringSceneGui(SceneView sceneView)
        {
            if (m_GUIState.eventType == EventType.Layout)
                m_Controller.ClearClosestPath();
                
            m_RectSelector.OnGUI();

            bool changed = false;
            
            ForEachTarget((target) =>
            {
                var path = GetPath(target);

                if (path != null)
                {
                    path.localToWorldMatrix = GetLocalToWorldMatrix(target);
                    path.forward = GetForward(target);
                    path.up = GetUp(target);
                    path.right = GetRight(target);
                    m_Controller.editablePath = path;

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        GetGUISystem(target).OnGUI();
                        OnCustomGUI(path);
                        changed |= check.changed;
                    }
                }
            });

            if (changed)
            {
                SetShapes();
                RepaintInspectors();
            }
        }

        private void BeginSelection(ISelector<Vector3> selector, bool isAdditive)
        {
            m_Controller.RegisterUndo("Selection");

            if (isAdditive)
            {
                ForEachTarget((target) =>
                {
                    var path = GetPath(target);
                    path.selection.BeginSelection();
                });
            }
            else
            {
                UpdateSelection(selector);
            }
        }

        private void UpdateSelection(ISelector<Vector3> selector)
        {
            var repaintInspectors = false;

            ForEachTarget((target) =>
            {
                var path = GetPath(target);

                repaintInspectors |= path.Select(selector);
            });

            if (repaintInspectors)
                RepaintInspectors();
        }

        private void EndSelection(ISelector<Vector3> selector)
        {
            ForEachTarget((target) =>
            {
                var path = GetPath(target);
                path.selection.EndSelection(true);
            });
        }

        internal void SetShapes()
        {
            ForEachTarget((target) =>
            {
                SetPath(target);
            });
        }

        private Transform GetTransform(UnityObject target)
        {
            return (target as Component).transform;
        }

        private Matrix4x4 GetLocalToWorldMatrix(UnityObject target)
        {
            return GetTransform(target).localToWorldMatrix;
        }

        private Vector3 GetForward(UnityObject target)
        {
            return GetTransform(target).forward;
        }

        private Vector3 GetUp(UnityObject target)
        {
            return GetTransform(target).up;
        }

        private Vector3 GetRight(UnityObject target)
        {
            return GetTransform(target).right;
        }

        protected abstract IShape GetShape(UnityObject target);
        protected virtual void Initialize(T path, SerializedObject serializedObject) { }
        protected abstract void SetShape(T path, SerializedObject serializedObject);
        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnCustomGUI(T path) { }
    }
}
