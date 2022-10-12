using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(RootView))]
    static class ConstantEditorFactoryExtensions
    {
        public static VisualElement BuildGradientTypeConstantEditor(this ConstantEditorBuilder builder, IEnumerable<GradientTypeConstant> constants)
        {
            var editor = new GradientField();
            editor.AddToClassList("sg-gradient-constant-field");
            editor.AddStylesheet("ConstantEditors.uss");
            editor.RegisterValueChangedCallback(change =>
            {
                builder.CommandTarget.Dispatch(new UpdateConstantsValueCommand(constants, change.newValue, builder.ConstantOwners));
            });

            return editor;
        }

        public static BaseModelPropertyField BuildGraphTypeConstantEditor(this ConstantEditorBuilder builder, IEnumerable<Constant> constants)
        {
            // TODO GTF UPGRADE: support edition of multiple models.

            var graphTypeConstants = constants.OfType<GraphTypeConstant>();
            if (!graphTypeConstants.Any())
            {
                return builder.BuildDefaultConstantEditor(constants);
            }

            var constant = graphTypeConstants.First();
            var owner = builder.ConstantOwners.First();

            var height = constant.GetHeight();
            if (height > GraphType.Height.One)
            {
                if (builder.ConstantOwners.First() is PortModel)
                {
                    return builder.BuildDefaultConstantEditor(constants);
                }

                return new MatrixConstantPropertyField(constant, owner, builder.CommandTarget, (int)height, builder.Label);
            }

            switch (owner)
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
                        return BuildColorConstantEditor(builder, graphTypeConstants, "", builder.Label, parameterUIDescriptor.Tooltip);
                    }

                    break;
                }
                case GraphDataVariableDeclarationModel declarationModel when declarationModel.DataType == ShaderGraphExampleTypes.Color:
                {
                    var isHdr = VariableSettings.colorMode.GetTyped(declarationModel) is VariableSettings.ColorMode.HDR;
                    return BuildColorConstantEditor(builder, graphTypeConstants, "", builder.Label, "", isHdr);
                }
            }

            // Try/Catch maybe.
            return builder.BuildDefaultConstantEditor(constants);
        }

        static BaseModelPropertyField BuildColorConstantEditor(ConstantEditorBuilder builder, IEnumerable<GraphTypeConstant> constants, string propertyName, string label, string tooltip, bool hdr = false)
        {
            // TODO GTF UPGRADE: support edition of multiple models.

            var constant = constants.First();
            var owner = builder.ConstantOwners.First();

            var length = constant.GetLength();

            var constantEditor = new SGModelPropertyField<Color>(
                builder.CommandTarget,
                builder.ConstantOwners,
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

                    field.CommandTarget.Dispatch(new UpdateConstantsValueCommand(constants, newValueVector, builder.ConstantOwners));
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
