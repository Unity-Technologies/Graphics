using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class MatrixConstantPropertyField : CustomizableModelPropertyField
    {
        readonly IConstant m_ConstantModel;
        readonly MatrixField m_Field;

        public MatrixConstantPropertyField(
            IConstant constantModel,
            IGraphElementModel owner,
            ICommandTarget commandTarget,
            int size,
            string label = null)
            : base(commandTarget, label)
        {
            m_ConstantModel = constantModel;

            m_Field = new MatrixField(label, size);
            m_Field.RegisterValueChangedCallback(change =>
            {
                CommandTarget.Dispatch(new UpdateConstantValueCommand(
                    m_ConstantModel,
                    change.newValue,
                    owner
                ));
            });

            Add(m_Field);
        }

        public override bool UpdateDisplayedValue()
        {
            var value = (Matrix4x4)m_ConstantModel.ObjectValue;

            if (value == m_Field.value) return false;

            m_Field.SetValueWithoutNotify(value);
            return true;
        }
    }
}
