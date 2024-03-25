using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class VectorPropertyRM<T, U> : SimpleVFXUIPropertyRM<T, U> where T : VFXVectorNField<U>, new()
    {
        public VectorPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            fieldControl.onValueDragFinished += ValueDragFinished;
            fieldControl.onValueDragStarted += ValueDragStarted;
        }

        protected override void UpdateIndeterminate()
        {
            ((VFXVectorNField<U>)field).indeterminate = indeterminate;
        }
    }

    class Vector4PropertyRM : VectorPropertyRM<VFXVector4Field, Vector4>
    {
        public Vector4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth() => 224;

        public override INotifyValueChanged<Vector4> CreateField()
        {
            var label = new Label(ObjectNames.NicifyVariableName(provider.name));
            label.AddToClassList("label");
            Add(label);
            return new VFXVector4Field();
        }
    }
}
