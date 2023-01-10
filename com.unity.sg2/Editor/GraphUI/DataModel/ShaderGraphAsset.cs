using System;
using System.IO;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphAsset : GraphAsset
    {
        protected override Type GraphModelType => typeof(SGGraphModel);
        public SGGraphModel SGGraphModel => GraphModel as SGGraphModel;

        protected override void OnEnable()
        {
            Name = Path.GetFileNameWithoutExtension(FilePath);
            base.OnEnable();
        }
    }
}
