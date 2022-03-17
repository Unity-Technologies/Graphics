using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Defs;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView))]
    public static class ModelInspectorViewFactoryExtensions
    {
        public static IModelView CreateSectionInspector(this ElementBuilder elementBuilder, GraphDataNodeModel model)
        {
            var ui = new ModelInspector();

            ui.Setup(model, elementBuilder.View, elementBuilder.Context);

            if (elementBuilder.Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Settings:
                    {
                        var inspectorFields = new StaticPortNodeFieldsInspector(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                    case SectionType.Properties:
                        var nodeInspectorFields = NodePortsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(nodeInspectorFields);
                        break;
                    case SectionType.Advanced:
                    {
                        var inspectorFields = SerializedFieldsInspector.Create(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName, ModelInspectorView.AdvancedSettingsFilter);
                        ui.PartList.AppendPart(inspectorFields);
                        break;
                    }
                }
            }

            ui.BuildUI();
            ui.UpdateFromModel();

            return ui;
        }
    }

    // TODO: Still experimenting. Everything below here should be moved to where it makes sense.

    static class TempNodeModelExtensions
    {
        public static IEnumerable<IPortReader> GetStaticPorts(this INodeReader nodeReader)
        {
            foreach (var portReader in nodeReader.GetInputPorts())
            {
                if (!portReader.GetField("IsStatic", out bool isStatic) || !isStatic) continue;
                yield return portReader;
            }
        }

        public static IEnumerable<IPortReader> GetStaticPortsOfType<T>(this INodeReader nodeReader) where T : IRegistryEntry
        {
            return nodeReader.GetStaticPorts().Where(p => p.GetRegistryKey().Name == Registry.Registry.ResolveKey<T>().Name);
        }

        public static bool IsType<T>(this IPortReader portReader) where T : IRegistryEntry
        {
            return portReader.GetRegistryKey().Name == Registry.Registry.ResolveKey<T>().Name;
        }

        public static bool TryGetGraphTypeSize(this IPortReader portReader, out GraphType.Length length, out GraphType.Height height)
        {
            length = GraphType.Length.Any;
            height = GraphType.Height.Any;
            return portReader.GetField(GraphType.kLength, out length) && portReader.GetField(GraphType.kHeight, out height);
        }
    }

    // TODO: Just for proof of concept -- should probably be generic like ModelPropertyField if possible
    public class TestFloatPropertyField : ModelPropertyField<float>
    {
        public TestFloatPropertyField(ICommandTarget commandTarget, IModel model, string portName, string label, string fieldTooltip)
            : base(commandTarget, model, portName, label, fieldTooltip)
        {
            SetValueGetterOrDefault(portName, (m) =>
            {
                if (m is not GraphDataNodeModel nodeModel) return default;
                if (!nodeModel.TryGetNodeReader(out var nodeReader)) return default;
                if (!nodeReader.TryGetPort(portName, out var portReader)) return default;
                if (!portReader.GetField("c0", out float value)) return default;

                return value;
            });

            m_Field.RegisterCallback<ChangeEvent<float>, ModelPropertyField<float>>(
                (e, f) =>
                {
                    f.CommandTarget.Dispatch(new SetGraphTypeValueCommand(
                            (GraphDataNodeModel)f.Model,
                            portName,
                            GraphType.Length.One,
                            GraphType.Height.One,
                            e.newValue
                        ));
                }, this);
        }
    }

    public class StaticPortNodeFieldsInspector : FieldsInspector
    {
        public StaticPortNodeFieldsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            if (m_Model is not GraphDataNodeModel nodeModel) yield break;
            if (!nodeModel.TryGetNodeReader(out var nodeReader)) yield break;

            foreach (var port in nodeReader.GetStaticPorts())
            {
                if (port.IsType<GraphType>())
                {
                    if (!port.TryGetGraphTypeSize(out var length, out var height)) continue;
                    switch (length, height)
                    {
                        // Invalid. This is matching against the actual "Any" value (-1), which should not be here.
                        case (_, GraphType.Height.Any):
                        case (GraphType.Length.Any, _):
                            Debug.LogWarning($"Port {port.GetName()} on {nodeReader.GetName()}: expected concrete GraphType size, but got Any when displaying node inspector. Ignoring this field.");
                            continue;

                        // Matrix.
                        case (_, >GraphType.Height.One):
                            break;

                        // Scalar or vector.
                        case (GraphType.Length.One, GraphType.Height.One):
                            yield return new TestFloatPropertyField(
                                m_OwnerElement?.RootView,
                                m_Model,
                                port.GetName(),
                                port.GetName(),
                                "asdf"
                            );
                            break;

                        default:
                            break;
                    }
                }

                if (port.IsType<GradientType>())
                {

                }
            }
        }
    }
}
