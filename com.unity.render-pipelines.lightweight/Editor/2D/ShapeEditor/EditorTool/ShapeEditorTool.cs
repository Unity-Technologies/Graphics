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
    internal static class ShapeEditorToolContents
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

        internal static ScriptableShapeEditor[] GetShapeEditors<T>() where T : EditorTool
        {
            var tool = GetEditorTool<T>();
            var targets = tool.targets;

            return targets.Select( (t) => ShapeEditorCache.instance.GetShapeEditor(t) ).Where ( s => s != null).ToArray();
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

    internal class ShapeEditorCache : ScriptableSingleton<ShapeEditorCache>
    {
        private Dictionary<UnityObject, ScriptableShapeEditor> m_ShapeEditors = new Dictionary<UnityObject, ScriptableShapeEditor>();

        internal ScriptableShapeEditor GetShapeEditor(UnityObject target)
        {
            var shapeEditor = default(ScriptableShapeEditor);
            m_ShapeEditors.TryGetValue(target, out shapeEditor);
            return shapeEditor;
        }

        internal ScriptableShapeEditor CreateShapeEditor<T>(UnityObject target) where T : ScriptableShapeEditor
        {
            var shapeEditor = GetShapeEditor(target);

            if (shapeEditor == null)
            {
                shapeEditor = ScriptableObject.CreateInstance<T>();
                shapeEditor.owner = target;
                m_ShapeEditors[target] = shapeEditor;
            }

            return shapeEditor;
        }
    }

    internal abstract class ShapeEditorTool<T> : EditorTool, IDuringSceneGuiTool, IShapeEditorController where T : ScriptableShapeEditor
    {
        private Dictionary<UnityObject, GUISystem> m_GUISystems = new Dictionary<UnityObject, GUISystem>();
        private Dictionary<UnityObject, SerializedObject> m_SerializedObjects = new Dictionary<UnityObject, SerializedObject>();
        private IShapeEditorController m_ShapeEditorController = new ShapeEditorController();
        private PointRectSelector m_RectSelector = new PointRectSelector();
        private bool m_IsActive = false;

        private UnityObject currentTarget { get; set; }
        private IShapeEditor currentShapeEditor { get { return GetShapeEditor(currentTarget); } }

        public override GUIContent toolbarIcon
        {
            get { return ShapeEditorToolContents.icon; }
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
        }

        private void HandleActivation()
        {
            if (m_IsActive == false && EditorTools.EditorTools.IsActiveTool(this))
            {
                OnActivate();
                m_IsActive = true;
            }
            else if (m_IsActive)
            {
                OnDeactivate();
                m_IsActive = false;
            }
        }

        private void OnActivate()
        {
            Selection.selectionChanged += SelectionChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            InitializeCache();
        }

        private void OnDeactivate()
        {
            Selection.selectionChanged -= SelectionChanged;
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
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
        }

        public override bool IsAvailable()
        {
            return targets.Count() > 0;
        }

        private void InitializeCache()
        {
            foreach(var target in targets)
            {
                if (target == null)
                    continue;
                
                var shapeEditor = GetShapeEditorCreateIfNeeded(target);
                var shape = GetShape(target);
                var controlPoints = shape.ToControlPoints();
                var pointCount = shapeEditor.pointCount;

                shapeEditor.localToWorldMatrix = Matrix4x4.identity;
                shapeEditor.shapeType = shape.type;
                shapeEditor.isOpenEnded = shape.isOpenEnded;
                shapeEditor.Clear();

                foreach (var controlPoint in controlPoints)
                    shapeEditor.AddPoint(controlPoint);

                if (pointCount != shapeEditor.pointCount)
                    shapeEditor.pointSelection.Clear();
                
                CreateGUISystem(target);
                Initialize(shapeEditor as T, GetSerializedObject(target));
            }
        }

        private ScriptableShapeEditor GetShapeEditorCreateIfNeeded(UnityObject targetObject)
        {
            var shapeEditor = ShapeEditorCache.instance.GetShapeEditor(targetObject);

            if (shapeEditor == null)
                shapeEditor = ShapeEditorCache.instance.CreateShapeEditor<T>(targetObject);

            return shapeEditor;
        }

        private ScriptableShapeEditor GetShapeEditor(UnityObject targetObject)
        {
            return ShapeEditorCache.instance.GetShapeEditor(targetObject);
        }

        private GUISystem GetGUISystem(UnityObject target)
        {
            GUISystem guiSystem;
            m_GUISystems.TryGetValue(target, out guiSystem);
            return guiSystem;
        }

        private void CreateGUISystem(UnityObject target)
        {
            var guiSystem = new GUISystem(new GUIState());
            var driver = new ShapeEditorDriver();

            driver.controller = this;
            driver.Install(guiSystem);

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
            m_RectSelector.OnGUI();
            
            foreach(var target in targets)
            {
                if (target == null)
                    continue;

                currentTarget = target;

                var shapeEditor = GetShapeEditor(target);
                shapeEditor.localToWorldMatrix = GetLocalToWorldMatrix(target);
                shapeEditor.forward = GetForward(target);
                shapeEditor.up = GetUp(target);
                shapeEditor.right = GetRight(target);

                GetGUISystem(target).OnGUI();
            }

            currentTarget = null;
        }

        private void SetShape(IShapeEditor shapeEditor, UnityObject target)
        {
            shapeEditor.localToWorldMatrix = Matrix4x4.identity;

            var undoName = Undo.GetCurrentGroupName();
            var serializedObject = GetSerializedObject(target);
            
            serializedObject.UpdateIfRequiredOrScript();

            SetShape(shapeEditor as T, serializedObject);

            Undo.SetCurrentGroupName(undoName);
        }

        internal void SetShapes()
        {
            foreach(var target in targets)
            {
                if (target == null)
                    continue;
                
                SetShape(GetShapeEditor(target), target);
            }
        }

        private void RegisterUndo(string undoName)
        {
            foreach(var target in targets)
            {
                if (target == null)
                    continue;
                    
                var shapeEditor = GetShapeEditor(target);
                shapeEditor.undoObject.RegisterUndo(undoName);
            }
        }

        private void BeginSelection(ISelector<Vector3> selector)
        {
            RegisterUndo("Selection");
        }

        private void UpdateSelection(ISelector<Vector3> selector)
        {
            foreach(var target in targets)
            {
                if (target == null)
                    continue;
                    
                var shapeEditor = GetShapeEditor(target);
                shapeEditor.Select(selector);
            }
        }

        private Transform GetTransform(UnityObject target)
        {
            return (target as Component).transform;
        }

        protected virtual Matrix4x4 GetLocalToWorldMatrix(UnityObject target)
        {
            return GetTransform(target).localToWorldMatrix;
        }

        protected virtual Vector3 GetForward(UnityObject target)
        {
            return GetTransform(target).forward;
        }

        protected virtual Vector3 GetUp(UnityObject target)
        {
            return GetTransform(target).up;
        }

        protected virtual Vector3 GetRight(UnityObject target)
        {
            return GetTransform(target).right;
        }

        protected abstract IShape GetShape(UnityObject target);
        protected virtual void Initialize(T shapeEditor, SerializedObject serializedObject) { }
        protected abstract void SetShape(T shapeEditor, SerializedObject serializedObject);

        IShapeEditor IShapeEditorController.shapeEditor
        {
            get { return currentShapeEditor; }
            set {}
        }
        ISnapping<Vector3> IShapeEditorController.snapping
        {
            get { return m_ShapeEditorController.snapping; }
            set { m_ShapeEditorController.snapping = value; }
        }

        public bool enableSnapping
        {
            get { return m_ShapeEditorController.enableSnapping; }
            set { m_ShapeEditorController.enableSnapping = value; }
        }

        void IShapeEditorController.RegisterUndo(string name)
        {
            RegisterUndo(name);
        }

        void IShapeEditorController.ClearSelection()
        {
            foreach(var target in targets)
            {
                if (target == null)
                    continue;
                    
                m_ShapeEditorController.shapeEditor = GetShapeEditor(target);
                m_ShapeEditorController.ClearSelection();
            }   
        }

        void IShapeEditorController.SelectPoint(int index, bool select)
        {
            m_ShapeEditorController.shapeEditor = currentShapeEditor;
            m_ShapeEditorController.SelectPoint(index, select);
        }

        void IShapeEditorController.CreatePoint(int index, Vector3 position)
        {
            m_ShapeEditorController.shapeEditor = currentShapeEditor;
            m_ShapeEditorController.CreatePoint(index, position);

            SetShape(currentShapeEditor, currentTarget);
        }

        void IShapeEditorController.RemoveSelectedPoints()
        {
            foreach(var target in targets)
            {
                if (target == null)
                    continue;
                    
                m_ShapeEditorController.shapeEditor = GetShapeEditor(target);
                m_ShapeEditorController.RemoveSelectedPoints();
            }

            SetShapes();
        }

        void IShapeEditorController.MoveSelectedPoints(Vector3 delta)
        {
            foreach(var target in targets)
            {
                if (target == null)
                    continue;
                    
                var shapeEditor = GetShapeEditor(target);
                var localDelta = Vector3.Scale(shapeEditor.right + shapeEditor.up, delta);
                
                m_ShapeEditorController.shapeEditor = shapeEditor;
                m_ShapeEditorController.MoveSelectedPoints(localDelta);
            }

            SetShapes();
        }

        void IShapeEditorController.MoveEdge(int index, Vector3 delta)
        {
            m_ShapeEditorController.shapeEditor = currentShapeEditor;
            m_ShapeEditorController.MoveEdge(index, delta);

            SetShape(currentShapeEditor, currentTarget);
        }

        void IShapeEditorController.SetLeftTangent(int index, Vector3 position, bool setToLinear, Vector3 cachedRightTangent)
        {
            m_ShapeEditorController.shapeEditor = currentShapeEditor;
            m_ShapeEditorController.SetLeftTangent(index, position, setToLinear, cachedRightTangent);

            SetShape(currentShapeEditor, currentTarget);
        }

        void IShapeEditorController.SetRightTangent(int index, Vector3 position, bool setToLinear, Vector3 cachedLeftTangent)
        {
            m_ShapeEditorController.shapeEditor = currentShapeEditor;
            m_ShapeEditorController.SetRightTangent(index, position, setToLinear, cachedLeftTangent);

            SetShape(currentShapeEditor, currentTarget);
        }
    }
}
