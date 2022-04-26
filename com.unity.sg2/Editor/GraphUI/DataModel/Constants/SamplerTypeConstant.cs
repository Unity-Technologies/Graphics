using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    internal struct SamplerStateData
    {
        public SamplerStateType.Filter filter;
        public SamplerStateType.Wrap wrap;
        public bool depthCompare;
        public SamplerStateType.Aniso aniso;
    }

    public class SamplerStateTypeConstant : BaseShaderGraphConstant
    {
        override protected object GetValue()
        {
            return new SamplerStateData {
            filter = SamplerStateType.GetFilter(GetField()),
            wrap = SamplerStateType.GetWrap(GetField()),
            depthCompare = SamplerStateType.GetDepthComparison(GetField()),
            aniso = SamplerStateType.GetAniso(GetField()) };
        }

        override protected void SetValue(object value)
        {
            var smp = (SamplerStateData)value;
            SamplerStateType.SetFilter(GetField(), smp.filter);
            SamplerStateType.SetWrap(GetField(), smp.wrap);
            SamplerStateType.SetDepthComparison(GetField(), smp.depthCompare);
            SamplerStateType.SetAniso(GetField(), smp.aniso);
    }

        override public object DefaultValue => Activator.CreateInstance(Type);
        override public Type Type => typeof(SamplerStateData);
        override public TypeHandle GetTypeHandle() => ShaderGraphExampleTypes.SamplerStateTypeHandle;
    }

}
