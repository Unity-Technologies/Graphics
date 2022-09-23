using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class GradientTypeConstant : BaseShaderGraphConstant
    {
        // TODO: (Sai) When Gradients have support for assigning values from the Gradient Editor,
        // revisit their duplication to ensure values are copied over
        protected override void StoreValueForCopy()
        {
            var currentGradientValue = GetValue();
            if(currentGradientValue != null)
                storedValue = (Gradient)currentGradientValue;
        }

        public override object GetStoredValueForCopy()
        {
            return storedValue;
        }

        [SerializeField]
        Gradient storedValue;

        override protected object GetValue() => GradientTypeHelpers.GetGradient(GetField());
        override protected void SetValue(object value) => GradientTypeHelpers.SetGradient(GetField(), (Gradient)value);
        override public object DefaultValue => Activator.CreateInstance(Type);
        override public Type Type => typeof(Gradient);
        override public TypeHandle GetTypeHandle() => ShaderGraphExampleTypes.GradientTypeHandle;
    }

}
