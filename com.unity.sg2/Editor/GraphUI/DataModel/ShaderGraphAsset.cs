using System;
using UnityEditor.GraphToolsFoundation.Overdrive;


namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphAsset : GraphAsset
    {
        protected override Type GraphModelType => typeof(ShaderGraphModel);
        public ShaderGraphModel ShaderGraphModel => GraphModel as ShaderGraphModel;
    }
}
