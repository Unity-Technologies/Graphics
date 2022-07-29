using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.UIElements;
using UnityEngine;
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
                    return builder.BuildDefaultConstantEditor(constant);
                }

                return new MatrixConstantPropertyField(constant, builder.ConstantOwner, builder.CommandTarget, (int)height, builder.Label);
            }

            switch (builder.ConstantOwner)
            {
                case GraphDataPortModel graphDataPort:
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

                    break;
                }
                case GraphDataVariableDeclarationModel declarationModel when declarationModel.DataType == ShaderGraphExampleTypes.Color:
                {
                    var isHdr = VariableSettings.colorMode.Get(declarationModel) is VariableSettings.ColorMode.HDR;
                    return BuildColorConstantEditor(builder, constant, "", builder.Label, "", isHdr);
                }
            }

            // Try/Catch maybe.
            return builder.BuildDefaultConstantEditor(constant);
        }

        static BaseModelPropertyField BuildColorConstantEditor(IConstantEditorBuilder builder, GraphTypeConstant constant, string propertyName, string label, string tooltip, bool hdr = false)
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
                colorField.hdr = hdr;
            }

            return constantEditor;
        }
    }
}
