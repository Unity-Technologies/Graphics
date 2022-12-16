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

        protected override object GetValue() => GradientTypeHelpers.GetGradient(GetField());

        protected override void SetValue(object value) => GradientTypeHelpers.SetGradient(GetField(), (Gradient)value);

        public override object DefaultValue => new Gradient();

        public override Type Type => typeof(Gradient);

        public override TypeHandle GetTypeHandle() => ShaderGraphExampleTypes.GradientTypeHandle;
    }
}
