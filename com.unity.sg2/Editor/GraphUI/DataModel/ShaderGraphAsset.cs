using System;
using System.IO;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphAsset : GraphAsset
    {
        protected override Type GraphModelType => typeof(ShaderGraphModel);
        public ShaderGraphModel ShaderGraphModel => GraphModel as ShaderGraphModel;
        protected override void OnEnable()
        {
            Name = Path.GetFileNameWithoutExtension(FilePath);
            base.OnEnable();
        }
    }
}
