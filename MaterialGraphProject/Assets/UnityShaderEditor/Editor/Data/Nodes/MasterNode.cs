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
        
        public static SurfaceMaterialOptions GetMaterialOptionsFromAlphaMode(AlphaMode alphaMode)
        {
            var materialOptions = new SurfaceMaterialOptions();
            switch (alphaMode)
            {
                case AlphaMode.Opaque:
                    materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                    materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.Zero;
                    materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                    materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                    materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.On;
                    materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Geometry;
                    materialOptions.renderType = SurfaceMaterialOptions.RenderType.Opaque;
                    break;
                case AlphaMode.AlphaBlend:
                    materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.SrcAlpha;
                    materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                    materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                    materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                    materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                    materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                    materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                    break;
                case AlphaMode.AdditiveBlend:
                    materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                    materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.One;
                    materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                    materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                    materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                    materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                    materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                    break;
            }
            return materialOptions;
        }
    }
}
