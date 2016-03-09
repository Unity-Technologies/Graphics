using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.Experimental
{
    public class VFXShaderGeneratorModule
    {
        protected static bool UpdateFlag(Dictionary<VFXAttrib, int> attribs, VFXAttrib attrib, VFXContextDesc.Type type, bool writable = false)
        {
            int attribFlag;
            if (attribs.TryGetValue(attrib, out attribFlag))
            {
                attribFlag |= (writable ? 0x3 : 0x1) << (((int)type - 1) << 1);
                attribs[attrib] = attribFlag;
                return true;
            }

            return false;
        }

        public virtual bool MarkAttributes(Dictionary<VFXAttrib, int> attribs)          { return true; }
        public virtual void WritePreBlock(StringBuilder builder, ShaderMetaData data)   { }
        public virtual void WritePostBlock(StringBuilder builder, ShaderMetaData data)  { }
    }

    public class VFXOutputShaderGeneratorModule : VFXShaderGeneratorModule
    {
        public virtual void WriteIndex(StringBuilder builder, ShaderMetaData data)                  { builder.AppendLine("\t\t\t\tuint index = id;"); }
        public virtual void WriteAdditionalVertexOutput(StringBuilder builder, ShaderMetaData data) { } // TMP
        public virtual void WritePixelShader(StringBuilder builder, ShaderMetaData data)            { } // TMP
        public virtual int[] GetSingleIndexBuffer(ShaderMetaData data)                              { return null; }
    }
}
