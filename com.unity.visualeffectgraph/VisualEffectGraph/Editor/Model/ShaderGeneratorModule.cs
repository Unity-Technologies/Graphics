using UnityEngine;
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

        protected static bool UpdateFlag(Dictionary<VFXAttrib, int> attribs, VFXAttrib attrib, VFXContextDesc.Type type, bool writable = false)
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

        protected static void AddOrUpdateFlag(Dictionary<VFXAttrib, int> attribs, VFXAttrib attrib, VFXContextDesc.Type type, bool writable = false)
        {
            if (!UpdateFlag(attribs,attrib,type,writable))
                attribs[attrib] = GetFlag(type, writable);
        }

        public virtual bool UpdateAttributes(Dictionary<VFXAttrib, int> attribs, ref int flags) { return true; }
        public virtual void UpdateUniforms(HashSet<VFXParamValue> uniforms)             { }
        public virtual void WritePreBlock(ShaderSourceBuilder builder, ShaderMetaData data)   { }
        public virtual void WritePostBlock(ShaderSourceBuilder builder, ShaderMetaData data) { }
    }

    public class VFXOutputShaderGeneratorModule : VFXShaderGeneratorModule
    {
        public virtual void WriteIndex(ShaderSourceBuilder builder, ShaderMetaData data) { builder.WriteLine("uint index = id;"); }
        public virtual void WriteAdditionalVertexOutput(ShaderSourceBuilder builder, ShaderMetaData data) { } // TMP
        public virtual void WritePixelShader(VFXSystemModel system,ShaderSourceBuilder builder, ShaderMetaData data) { } // TMP
        public virtual int[] GetSingleIndexBuffer(ShaderMetaData data)                              { return null; }
    }
}
