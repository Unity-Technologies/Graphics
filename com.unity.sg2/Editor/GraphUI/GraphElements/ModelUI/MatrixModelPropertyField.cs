using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using Unity.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class MatrixConstantPropertyField : CustomizableModelPropertyField
    {
        readonly Constant m_ConstantModel;
        readonly MatrixField m_Field;

        public MatrixConstantPropertyField(
            Constant constantModel,
            GraphElementModel owner,
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
