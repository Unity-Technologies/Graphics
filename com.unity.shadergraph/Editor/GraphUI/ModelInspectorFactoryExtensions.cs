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
                        var s = new StaticPortNodeFieldsInspector(ModelInspector.fieldsPartName, model, ui, ModelInspector.ussClassName);
                        ui.PartList.AppendPart(s);
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

    public class TempShimField : CustomizableModelPropertyField
    {
        readonly GraphDataNodeModel m_Model;
        readonly string m_PortName;

        BaseModelViewPart m_Part;

        public TempShimField(ICommandTarget commandTarget, GraphDataNodeModel model, string portName)
            : base(commandTarget, portName)
        {
            m_Model = model;
            m_PortName = portName;

            BuildEditor();
        }

        void BuildEditor()
        {
            if (!m_Model.TryGetNodeReader(out var nodeReader)) return;
            if (!nodeReader.TryGetPort(m_PortName, out var portReader)) return;

            var stencil = (ShaderGraphStencil)m_Model.GraphModel.Stencil;
            var hints = stencil.GetUIHints(m_Model.registryKey);

            if (portReader.IsType<GraphType>())
            {
                if (!portReader.TryGetGraphTypeSize(out var length, out var height)) return;
                if (!portReader.GetField(GraphType.kPrimitive, out GraphType.Primitive primitive)) return;

                // TODO: Still experimenting with nice ways to do this.
                switch (new {length, height, primitive})
                {
                    // Invalid. This is matching against the actual "Any" value (-1), which should not be here.
                    case {length: GraphType.Length.Any} or {height: GraphType.Height.Any}:
                        Debug.LogWarning($"Port {portReader.GetName()} on {nodeReader.GetName()}: expected concrete GraphType size, but got Any when displaying node inspector. Ignoring this field.");
                        return;

                    // Matrix.
                    case {height: > GraphType.Height.One}:
                        m_Part = new MatrixPart("sg-matrix", m_Model, null, "", m_PortName, (int)length);
                        m_Part.BuildUI(this);
                        hierarchy.Add(m_Part.Root);
                        break;

                    // Vectors.
                    case {length: GraphType.Length.Four} when hints.ContainsKey(m_PortName + ".UseColor"):
                        m_Part = new ColorPart("sg-color", m_Model, null, ussClassName, portReader.GetName(), includeAlpha: true);
                        m_Part.BuildUI(this);
                        hierarchy.Add(m_Part.Root);
                        break;

                    case {length: GraphType.Length.Four}:
                        m_Part = new Vector4Part("sg-vec4", m_Model, null, ussClassName, portReader.GetName());
                        m_Part.BuildUI(this);
                        hierarchy.Add(m_Part.Root);
                        break;

                    case {length: GraphType.Length.Three} when hints.ContainsKey(m_PortName + ".UseColor"):
                        m_Part = new ColorPart("sg-color", m_Model, null, ussClassName, portReader.GetName(), includeAlpha: false);
                        m_Part.BuildUI(this);
                        hierarchy.Add(m_Part.Root);
                        break;

                    case {length: GraphType.Length.Three}:
                        m_Part = new Vector3Part("sg-vec3", m_Model, null, ussClassName, portReader.GetName());
                        m_Part.BuildUI(this);
                        hierarchy.Add(m_Part.Root);
                        break;

                    case {length: GraphType.Length.Two}:
                        m_Part = new Vector2Part("sg-vec2", m_Model, null, ussClassName, portReader.GetName());
                        m_Part.BuildUI(this);
                        hierarchy.Add(m_Part.Root);
                        break;

                    // Scalars.
                    case {primitive: GraphType.Primitive.Float} when hints.ContainsKey(m_PortName + ".UseSlider"):
                        m_Part = new SliderPart("sg-slider", m_Model, null, "", m_PortName);
                        m_Part.BuildUI(this);
                        break;

                    case {primitive: GraphType.Primitive.Float}:
                        m_Part = new FloatPart("sg-float", m_Model, null, "", m_PortName);
                        m_Part.BuildUI(this);
                        break;

                    case {primitive: GraphType.Primitive.Bool}:
                        m_Part = new BoolPart("sg-bool", m_Model, null, "", m_PortName);
                        m_Part.BuildUI(this);
                        break;

                    case {primitive: GraphType.Primitive.Int}:
                        m_Part = new IntPart("sg-int", m_Model, null, "", m_PortName);
                        m_Part.BuildUI(this);
                        break;

                    default:
                        break;
                }
            }

            if (portReader.IsType<GradientType>()) { }
        }

        public override bool UpdateDisplayedValue()
        {
            if (m_Part == null) return false;
            m_Part.UpdateFromModel();
            return true;
        }
    }

    public class StaticPortNodeFieldsInspector : FieldsInspector
    {
        readonly bool m_InspectorOnly;

        public StaticPortNodeFieldsInspector(string name, IModel model, IModelView ownerElement, string parentClassName, bool inspectorOnly = false)
            : base(name, model, ownerElement, parentClassName)
        {
            m_InspectorOnly = inspectorOnly;
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            if (m_Model is not GraphDataNodeModel nodeModel) yield break;
            if (!nodeModel.TryGetNodeReader(out var nodeReader)) yield break;

            foreach (var port in nodeReader.GetStaticPorts())
            {
                // if (m_InspectorOnly == hints.ContainsKey(port.GetName() + ".InspectorOnly")) continue;
                // Debug.Log($"Adding a port: {port.GetName()}");
                yield return new TempShimField(m_OwnerElement?.RootView, nodeModel, port.GetName());
            }
        }
    }
}
