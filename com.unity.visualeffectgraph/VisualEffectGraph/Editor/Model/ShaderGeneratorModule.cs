using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
    public class VFXShaderGeneratorModule
    {
        protected static bool UpdateFlag(Dictionary<VFXAttribute, VFXAttribute.Usage> attribs, VFXAttribute attrib, VFXContextDesc.Type type, bool writable = false)
        {
            VFXAttribute.Usage attribFlag;
            if (attribs.TryGetValue(attrib, out attribFlag))
            {
                attribFlag |= VFXAttribute.ContextToUsage(type,writable);
                attribs[attrib] = attribFlag;
                return true;
            }

            return false;
        }

        protected static void AddOrUpdateFlag(Dictionary<VFXAttribute, VFXAttribute.Usage> attribs, VFXAttribute attrib, VFXContextDesc.Type type, bool writable = false)
        {
            if (!UpdateFlag(attribs,attrib,type,writable))
                attribs[attrib] = VFXAttribute.ContextToUsage(type, writable);
        }

        public virtual bool UpdateAttributes(Dictionary<VFXAttribute, VFXAttribute.Usage> attribs, ref VFXBlockDesc.Flag flags) { return true; }
        public virtual void UpdateUniforms(HashSet<VFXExpression> uniforms, ref VFXBlockDesc.Flag flags) { }
        public virtual void WritePreBlock(ShaderSourceBuilder builder, ShaderMetaData data) { }
        public virtual void WritePostBlock(ShaderSourceBuilder builder, ShaderMetaData data) { }
        public virtual void WriteFunctions(ShaderSourceBuilder builder, ShaderMetaData data) { }
    } 

    public class VFXOutputShaderGeneratorModule : VFXShaderGeneratorModule
    {
		public enum OutputType
		{
			Point,
			Quad,
			Mesh,
            Hexahedron,
		};

		public virtual bool WriteVertexInputStructure(ShaderSourceBuilder builder, ShaderMetaData data) { return false; }
        public virtual void WriteIndex(ShaderSourceBuilder builder, ShaderMetaData data) { builder.WriteLine("uint index = id;"); }
        public virtual void WriteAdditionalVertexOutput(ShaderSourceBuilder builder, ShaderMetaData data) { } // TMP
        public virtual void WriteAdditionalPixelOutput(ShaderSourceBuilder builder, ShaderMetaData data) { } // TMP
        public virtual void WritePixelShader(ShaderSourceBuilder builder, ShaderMetaData data) { } // TMP
        public virtual OutputType GetOutputType() { return OutputType.Point; }
        public virtual VFXExpression GetMesh() { return null; }
        public virtual bool CanUseDeferred() { return false; }
        public virtual bool NeedsDepthTexture() { return false; }
        public virtual bool NeedsBackFaceCulling() { return false; }

        // Helper function
        protected static void WriteTex2DFetch(ShaderSourceBuilder builder, ShaderMetaData data, VFXValue texture, string uv, bool endLine)
        {
            builder.WriteFormat("{0}Texture.Sample(sampler{0}Texture,{1})", data.paramToName[(int)ShaderMetaData.Pass.kOutput][texture], uv);
            if (endLine)
                builder.WriteLine(";");
        }
    }
}
