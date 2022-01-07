using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.GraphElements;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.Factory
{
    [GraphElementsExtensionMethodsCache(typeof(ShaderGraphView), GraphElementsExtensionMethodsCacheAttribute.toolDefaultPriority + 1)]
    public static class ShaderGraphExampleViewFactoryExtensions
    {
        public static IModelUI CreateConnectionInfoNode(this ElementBuilder elementBuilder, CommandDispatcher store,
            ConnectionInfoNodeModel model)
        {
            var ui = new ConnectionInfoNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateDataNode(this ElementBuilder elementBuilder, CommandDispatcher store,
            DataNodeModel model)
        {
            var ui = new DataNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateConversionEdge(this ElementBuilder elementBuilder, CommandDispatcher store,
            ConversionEdgeModel model)
        {
            var ui = new ConversionEdge();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateCustomizableNode(this ElementBuilder elementBuilder, CommandDispatcher store,
            CustomizableNodeModel model)
        {
            var ui = new CustomizableNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreatePort(this ElementBuilder elementBuilder, CommandDispatcher store,
            PortModel model)
        {
            var ui = (Port)GraphViewFactoryExtensions.CreatePort(elementBuilder, store, model);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.shadergraph/Editor/GraphUI/GraphElements/Stylesheets/ShaderGraphPorts.uss");
            ui.styleSheets.Add(styleSheet);
            return ui;
        }

        public static VisualElement CreateCustomTypeEditor(this IConstantEditorBuilder editorBuilder,
            DayOfWeekConstant c)
        {
            var dropdown = new DropdownField(DayOfWeekConstant.Names, DayOfWeekConstant.Values.IndexOf(c.Value));
            dropdown.RegisterValueChangedCallback(_ => { c.Value = DayOfWeekConstant.Values[dropdown.index]; });

            var root = new VisualElement();
            root.Add(dropdown);

            return root;
        }
    }
}
