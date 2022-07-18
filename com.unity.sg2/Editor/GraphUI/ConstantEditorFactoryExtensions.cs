using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(RootView))]
    public static class ConstantEditorFactoryExtensions
    {
        public static VisualElement BuildGradientTypeConstantEditor(this IConstantEditorBuilder builder, GradientTypeConstant constant)
        {
            var editor = new GradientField();
            editor.AddToClassList("sg-gradient-constant-field");
            editor.AddStylesheet("ConstantEditors.uss");
            editor.RegisterValueChangedCallback(change =>
            {
                builder.CommandTarget.Dispatch(new UpdateConstantValueCommand(constant, change.newValue, builder.ConstantOwner));
            });

            return editor;
        }

        public static BaseModelPropertyField BuildGraphTypeConstantEditor(this IConstantEditorBuilder builder, GraphTypeConstant constant)
        {
            var height = constant.GetHeight();
            if (height > GraphType.Height.One)
            {
                if (builder.ConstantOwner is PortModel)
                {
                    return new MissingFieldEditor(builder.CommandTarget, "Matrix (TODO)");
                }
                // TODO (Joe): Default should be identity matrix
                return new MatrixConstantPropertyField(constant, builder.ConstantOwner, builder.CommandTarget, (int)height, builder.Label);
            }

            // TODO (Joe): Two color fields pointing to the same data can get visually out of sync.
            // Try changing a color field when it's visible in both the inspector and blackboard. Only one appears to
            // change. But if you right click on the unchanged one and select "Copy," the correct, updated value will
            // be copied.

            if (builder.ConstantOwner is GraphDataPortModel graphDataPort)
            {
                ((GraphDataNodeModel)graphDataPort.NodeModel).TryGetNodeHandler(out var nodeReader);

                var length = constant.GetLength();
                var stencil = (ShaderGraphStencil)graphDataPort.GraphModel.Stencil;
                var nodeUIDescriptor = stencil.GetUIHints(graphDataPort.owner.registryKey, nodeReader);
                var parameterUIDescriptor = nodeUIDescriptor.GetParameterInfo(constant.PortName);

                if (length >= GraphType.Length.Three && parameterUIDescriptor.UseColor)
                {
                    return BuildColorConstantEditor(builder, constant, "", builder.Label, parameterUIDescriptor.Tooltip);
                }
            }

            if (builder.ConstantOwner is VariableDeclarationModel declarationModel &&
                declarationModel.DataType == ShaderGraphExampleTypes.Color)
            {
                return BuildColorConstantEditor(builder, constant, "", builder.Label, "");
            }

            // Try/Catch maybe.
            return builder.BuildDefaultConstantEditor(constant);
        }

        static BaseModelPropertyField BuildColorConstantEditor(IConstantEditorBuilder builder, GraphTypeConstant constant, string propertyName, string label, string tooltip)
        {
            var length = constant.GetLength();

            var constantEditor = new SGModelPropertyField<Color>(
                builder.CommandTarget,
                builder.ConstantOwner,
                propertyName,
                label,
                tooltip,
                onValueChanged: (newValueColor, field) =>
                {
                    object newValueVector;

                    if (length == GraphType.Length.Three)
                    {
                        newValueVector = new Vector3(newValueColor.r, newValueColor.g, newValueColor.b);
                    }
                    else
                    {
                        newValueVector = (Vector4)newValueColor;
                    }

                    field.CommandTarget.Dispatch(new UpdateConstantValueCommand(constant, newValueVector, builder.ConstantOwner));
                },
                valueGetter: _ =>
                {
                    if (length == GraphType.Length.Three)
                    {
                        var vec3Value = (Vector3)constant.ObjectValue;
                        return new Color(vec3Value.x, vec3Value.y, vec3Value.z);
                    }

                    return (Color)(Vector4)constant.ObjectValue;
                });

            if (constantEditor.PropertyField is ColorField colorField)
            {
                colorField.showAlpha = (int)length == 4;
            }

            return constantEditor;
        }
    }
}
