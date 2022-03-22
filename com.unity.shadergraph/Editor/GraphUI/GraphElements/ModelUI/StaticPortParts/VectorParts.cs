using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // These parts could be done dynamically akin to MatrixParts. But UIElements offers existing controls for them,
    // so we will opt for those first.

    public class Vector2Part : SingleFieldPart<Vector2Field, Vector2>
    {
        protected override string UXMLTemplateName => "StaticPortParts/Vector2Part";
        protected override string FieldName => "sg-vector2-field";

        public Vector2Part(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName, portName) { }

        protected override void OnFieldValueChanged(ChangeEvent<Vector2> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;

            var value = change.newValue;
            m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
                m_PortName,
                GraphType.Length.Two,
                GraphType.Height.One,
                value.x,
                value.y));
        }

        protected override void UpdatePartFromPortReader(PortHandler reader)
        {
            var value = GraphTypeHelpers.GetAsVec2(reader.GetTypeField());
            m_Field.SetValueWithoutNotify(value);
        }
    }

    public class Vector3Part : SingleFieldPart<Vector3Field, Vector3>
    {
        protected override string UXMLTemplateName => "StaticPortParts/Vector3Part";
        protected override string FieldName => "sg-vector3-field";

        public Vector3Part(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName, portName) { }

        protected override void OnFieldValueChanged(ChangeEvent<Vector3> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;

            var value = change.newValue;
            m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
                m_PortName,
                GraphType.Length.Three,
                GraphType.Height.One,
                value.x,
                value.y,
                value.z));
        }

        protected override void UpdatePartFromPortReader(PortHandler reader)
        {
            var value = GraphTypeHelpers.GetAsVec3(reader.GetTypeField());
            m_Field.SetValueWithoutNotify(value);
        }
    }

    public class Vector4Part : SingleFieldPart<Vector4Field, Vector4>
    {
        protected override string UXMLTemplateName => "StaticPortParts/Vector4Part";
        protected override string FieldName => "sg-vector4-field";

        public Vector4Part(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName, portName) { }

        protected override void OnFieldValueChanged(ChangeEvent<Vector4> change)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;

            var value = change.newValue;
            m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel,
                m_PortName,
                GraphType.Length.Three,
                GraphType.Height.One,
                value.x,
                value.y,
                value.z,
                value.w));
        }

        protected override void UpdatePartFromPortReader(PortHandler reader)
        {
            var value = GraphTypeHelpers.GetAsVec4(reader.GetTypeField());
            m_Field.SetValueWithoutNotify(value);
        }
    }
}
