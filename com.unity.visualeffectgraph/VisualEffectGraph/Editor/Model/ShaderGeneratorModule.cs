using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
    public class VFXShaderGeneratorModule
    {
        private static int GetFlag(VFXContextDesc.Type type, bool writable = false)
        {
            return (writable ? 0x3 : 0x1) << (((int)type - 1) << 1);
        }

        protected static bool UpdateFlag(Dictionary<VFXAttribute, int> attribs, VFXAttribute attrib, VFXContextDesc.Type type, bool writable = false)
        {
            int attribFlag;
            if (attribs.TryGetValue(attrib, out attribFlag))
            {
                attribFlag |= GetFlag(type,writable);
                attribs[attrib] = attribFlag;
                return true;
            }

            return false;
        }

        protected static void AddOrUpdateFlag(Dictionary<VFXAttribute, int> attribs, VFXAttribute attrib, VFXContextDesc.Type type, bool writable = false)
        {
            if (!UpdateFlag(attribs,attrib,type,writable))
                attribs[attrib] = GetFlag(type, writable);
        }

        public virtual bool UpdateAttributes(Dictionary<VFXAttribute, int> attribs, ref VFXBlockDesc.Flag flags)    { return true; }
        public virtual void UpdateUniforms(HashSet<VFXExpression> uniforms)                                         { }
        public virtual void UpdateExpressions(HashSet<VFXExpression> expressions)                                   { }
        public virtual void WritePreBlock(ShaderSourceBuilder builder, ShaderMetaData data)                         { }
        public virtual void WritePostBlock(ShaderSourceBuilder builder, ShaderMetaData data)                        { }
        public virtual void WriteFunctions(ShaderSourceBuilder builder, ShaderMetaData data)                        { }
    } 

    public class VFXOutputShaderGeneratorModule : VFXShaderGeneratorModule
    {
        public virtual void WriteIndex(ShaderSourceBuilder builder, ShaderMetaData data) { builder.WriteLine("uint index = id;"); }
        public virtual void WriteAdditionalVertexOutput(ShaderSourceBuilder builder, ShaderMetaData data) { } // TMP
        public virtual void WritePixelShader(ShaderSourceBuilder builder, ShaderMetaData data) { } // TMP
        public virtual int[] GetSingleIndexBuffer(ShaderMetaData data)                              { return null; }
    }
}
