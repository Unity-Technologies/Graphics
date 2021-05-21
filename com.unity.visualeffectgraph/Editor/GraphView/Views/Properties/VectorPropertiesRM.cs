using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;

namespace UnityEditor.VFX.UI
{
    abstract class VectorPropertyRM<U, T> : SimpleVFXUIPropertyRM<U, T> where U : VFXVectorNField<T>, new()
    {
        public VectorPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            fieldControl.onValueDragFinished = () => ValueDragFinished();
            fieldControl.onValueDragStarted = () => ValueDragStarted();
        }

        protected void ValueDragFinished()
        {
            m_Provider.EndLiveModification();
            hasChangeDelayed = false;
            NotifyValueChanged();
        }

        protected void ValueDragStarted()
        {
            m_Provider.StartLiveModification();
        }
    }

    class Vector4PropertyRM : VectorPropertyRM<VFXVector4Field, Vector4>
    {
        public Vector4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 224;
        }
    }
}
