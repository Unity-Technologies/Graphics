using System;
using System.IO;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphAsset : GraphAsset
    {
        public static SGAssetVersion CurrentVersion = new SGAssetVersion(1, 0, 0);
        public SGAssetVersion version;
        protected override Type GraphModelType => typeof(SGGraphModel);
        public SGGraphModel SGGraphModel => GraphModel as SGGraphModel;

        protected override void OnEnable()
        {
            Name = Path.GetFileNameWithoutExtension(FilePath);
            base.OnEnable();
        }
    }
}
