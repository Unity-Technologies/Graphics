using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class MathOperatorInspector : FieldsInspector
    {
        public static MathOperatorInspector Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is MathOperator)
            {
                return new MathOperatorInspector(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <inheritdoc />
        public MathOperatorInspector(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            if (m_Model is MathOperator mathOperator)
            {
                yield return new ModelPropertyField<int>(
                    m_OwnerElement.View,
                    mathOperator,
                    nameof(mathOperator.InputPortCount),
                    "Number of Inputs",
                    typeof(SetNumberOfInputPortCommand));
            }
        }
    }
}
