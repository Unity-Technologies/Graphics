using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXUniformMapper
    {
        public VFXUniformMapper(VFXExpressionMapper mapper)
        {
            Init(mapper);
        }

        private static bool IsTexture(VFXExpression exp)
        {
            switch (exp.ValueType)
            {
                case VFXValueType.kTexture2D:
                case VFXValueType.kTexture3D:
                    return true;
            }

            return false;
        }

        private static bool IsUniform(VFXExpression exp)
        {
            switch (exp.ValueType)
            {
                case VFXValueType.kFloat:
                case VFXValueType.kFloat2:
                case VFXValueType.kFloat3:
                case VFXValueType.kFloat4:
                case VFXValueType.kInt:
                case VFXValueType.kUint:
                case VFXValueType.kTransform:
                    return true;
            }

            return false;
        }

        private void CollectAndAddUniforms(VFXExpression exp, string name)
        {
            if (!exp.Is(VFXExpression.Flags.PerElement))
            {
                if (exp.Is(VFXExpression.Flags.InvalidOnCPU))
                    throw new InvalidOperationException(string.Format("Collected uniform expression is invalid on CPU: {0}", exp));

                string prefix;
                Dictionary<VFXExpression, string> expressions;

                if (IsUniform(exp))
                {
                    prefix = "uniform_" + (exp.Is(VFXExpression.Flags.Constant) ? "CONSTANT_" : "");
                    expressions = m_UniformToName;
                }
                else if (IsTexture(exp))
                {
                    prefix = "texture_";
                    expressions = m_TextureToName;
                }
                else
                    return;

                if (expressions.ContainsKey(exp)) // Only need one name
                    return;

                name = prefix + (name != null ? name : expressions.Count().ToString());
                expressions[exp] = name;
            }
            else
                foreach (var parent in exp.Parents)
                    CollectAndAddUniforms(parent, null);
        }

        private void Init(VFXExpressionMapper mapper)
        {
            m_UniformToName = new Dictionary<VFXExpression, string>();
            m_TextureToName = new Dictionary<VFXExpression, string>();

            foreach (var exp in mapper.expressions)
                CollectAndAddUniforms(exp, mapper.GetData(exp).First().fullName);
        }

        public IEnumerable<VFXExpression> uniforms { get { return m_UniformToName.Keys; } }
        public IEnumerable<VFXExpression> textures { get { return m_TextureToName.Keys; } }

        public int numUniforms { get { return m_UniformToName.Count(); } }
        public int numTextures { get { return m_TextureToName.Count(); } }

        public string GetUniformName(VFXExpression exp) { return m_UniformToName[exp]; }
        public string GetTextureName(VFXExpression exp) { return m_TextureToName[exp]; }

        private Dictionary<VFXExpression, string> m_UniformToName;
        private Dictionary<VFXExpression, string> m_TextureToName;
    }
}
