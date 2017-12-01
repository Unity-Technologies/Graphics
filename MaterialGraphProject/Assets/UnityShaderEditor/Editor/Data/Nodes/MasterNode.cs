using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class MasterNode : AbstractMaterialNode, IMasterNode
    {
        [SerializeField]
        protected SurfaceMaterialOptions m_MaterialOptions = new SurfaceMaterialOptions();

        public override bool hasPreview
        {
            get { return true; }
        }

        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public SurfaceMaterialOptions options
        {
            get { return m_MaterialOptions; }
        }

        public abstract string GetShader(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures);
    }
}
