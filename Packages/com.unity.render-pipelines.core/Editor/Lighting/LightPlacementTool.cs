using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor;

[EditorTool("Light Placement Tool", typeof(Light))]
class LightPlacementTool : EditorTool
{
    private const float k_DefaultZoom = 0.05f;
    private bool initialPositionSet;

    private static Vector3 initialPivot;
    private static Quaternion initialRotation;

    private static Vector3 originalPivot;
    private static Quaternion originalRotation;

    private static GameObject currentGameObject;
    private float previousZoom;
    private static SceneView.SceneViewState previousSceneViewState;
    private static SceneView lastSceneView;

    public override GUIContent toolbarIcon => EditorGUIUtility.TrIconContent(UnityEditor.Rendering.CoreEditorUtils.LoadIcon(@"Packages/com.unity.render-pipelines.core/Editor/Lighting/Icons/", "LightPlacement_Icon", ".png", false), "The Light Placement Tool temporarily changes the Scene Camera to look-through the currently selected Light. Use the Scene View Navigation controls to move it around the Scene.");

    public override void OnActivated()
    {
        base.OnActivated();
        if (lastSceneView == null)
        {
            lastSceneView = SceneView.lastActiveSceneView;
            lastSceneView.orthographic = false;
            previousSceneViewState = lastSceneView.sceneViewState;
            lastSceneView.sceneViewState.alwaysRefresh = true;
        }
        TransitionCameraToLight();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    public override void OnWillBeDeactivated()
    {
        base.OnWillBeDeactivated();
        if (lastSceneView != null)
        {
            lastSceneView.sceneViewState = previousSceneViewState;
        }
        Undo.undoRedoPerformed -= OnUndoRedo;
        ResetCamera();
    }

    public override void OnToolGUI(EditorWindow window)
    {
        base.OnToolGUI(window);
        SceneView view = SceneView.lastActiveSceneView;

        //View size getting smaller than 0.0001f causes the camera to get stuck
        if (view.size <= 0)
            view.FixNegativeSize();

        if (lastSceneView != view)
            return;

        if (currentGameObject == null)
            return;

        if (!initialPositionSet)
        {
            SetInitialPosition(view);
            return;
        }

        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            ToolManager.RestorePreviousTool();
            e.Use();
            view.Repaint();
            return;
        }

        Undo.RecordObject(currentGameObject.transform, "Move Light");
        Vector3 lookDir = (view.pivot - view.camera.transform.position).normalized;
        currentGameObject.transform.SetPositionAndRotation(view.camera.transform.position + lookDir * k_DefaultZoom * 2f, view.camera.transform.rotation);
    }

    private void OnUndoRedo()
    {
        if (currentGameObject != null && lastSceneView != null)
        {
            lastSceneView.pivot = currentGameObject.transform.position;
            lastSceneView.rotation = currentGameObject.transform.rotation;
        }
    }

    private void TransitionCameraToLight()
    {
        initialPivot = SceneView.lastActiveSceneView.pivot;
        initialRotation = SceneView.lastActiveSceneView.camera.transform.rotation;

        var targetComponent = target as Light;
        if (targetComponent != null)
        {
            currentGameObject = targetComponent.gameObject;
            originalPivot = currentGameObject.transform.position;
            originalRotation = currentGameObject.transform.rotation;
            initialPositionSet = false;
        }
    }

    private void SetInitialPosition(SceneView view)
    {
        previousZoom = view.size;
        view.pivot = originalPivot;
        view.rotation = originalRotation;
        view.size = k_DefaultZoom;
        view.Repaint();
        initialPositionSet = true;
    }

    private void ResetCamera()
    {
        SceneView.lastActiveSceneView.pivot = initialPivot;
        SceneView.lastActiveSceneView.rotation = initialRotation;
        SceneView.lastActiveSceneView.size = previousZoom;
    }
}
