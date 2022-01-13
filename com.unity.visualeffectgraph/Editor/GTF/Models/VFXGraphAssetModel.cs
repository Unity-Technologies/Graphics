using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.VFX;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

public class VFXGraphAssetModel : GraphAssetModel
{
    [MenuItem("Assets/Create/Visual Effect/VFX Graph")]
    public static void CreateGraph(MenuCommand menuCommand)
    {
        const string path = "Assets";
        var template = new GraphTemplate<VFXStencil>(VFXStencil.graphName);
        ICommandTarget target = null;
        if (EditorWindow.HasOpenInstances<VFXGraphWindow>())
        {
            var window = EditorWindow.GetWindow<VFXGraphWindow>();
            if (window != null)
            {
                target = window.GraphTool;
            }
        }

        GraphAssetCreationHelpers<VFXGraphAssetModel>.CreateInProjectWindow(template, target, path);
    }

    [OnOpenAsset(1)]
    public static bool OpenGraphAsset(int instanceId, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceId);
        if (obj is VFXGraphAssetModel graphAssetModel)
        {
            var window = GraphViewEditorWindow.FindOrCreateGraphWindow<VFXGraphWindow>();
            window.SetCurrentSelection(graphAssetModel, GraphViewEditorWindow.OpenMode.OpenAndFocus);
            return true;
        }

        return false;
    }

    protected override Type GraphModelType => typeof(VFXGraphModel);
}
