using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class MaterialGraph : AbstractMaterialGraph
    {
        public MasterNode masterNode
        {
            get { return GetNodes<MasterNode>().FirstOrDefault(); }
        }

        public string GetShader(string name, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            PreviewMode pmode;
            return GetShader(masterNode, mode, name, out configuredTextures, out pmode);
        }

    }
}
