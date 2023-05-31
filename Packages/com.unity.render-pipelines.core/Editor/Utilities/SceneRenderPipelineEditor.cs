using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(SceneRenderPipeline))]
    class SceneRenderPipelineEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new HelpBox("This script is <b>Editor Only</b>.\nIt <b>modifies the project configuration</b> when this scene is opened.\nUse it with caution.", HelpBoxMessageType.Warning));
        
            var rpAssetProperty = serializedObject.FindProperty("renderPipelineAsset");
            var rpAssetField = new PropertyField(rpAssetProperty);
            rpAssetField.RegisterValueChangeCallback(evt => GraphicsSettings.renderPipelineAsset = rpAssetProperty.objectReferenceValue as RenderPipelineAsset);
            root.Add(rpAssetField);
            return root;
        }
    }
}