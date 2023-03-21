using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXSGInputs
    {
        private Dictionary<VFXExpression, string> m_Interpolators = new Dictionary<VFXExpression, string>();
        private Dictionary<string, VFXExpression> m_FragInputs = new Dictionary<string, VFXExpression>();
        private Dictionary<string, VFXExpression> m_VertInputs = new Dictionary<string, VFXExpression>();

        public IEnumerable<KeyValuePair<VFXExpression, string>> interpolators => m_Interpolators;
        public IEnumerable<KeyValuePair<string, VFXExpression>> fragInputs => m_FragInputs;
        public IEnumerable<KeyValuePair<string, VFXExpression>> vertInputs => m_VertInputs;

        public bool IsInterpolant(VFXExpression exp)
        {
            return m_Interpolators.ContainsKey(exp);
        }

        public string GetInterpolantName(VFXExpression exp)
        {
            return m_Interpolators[exp];
        }

        public VFXSGInputs(VFXExpressionMapper mapper, VFXUniformMapper uniforms, IEnumerable<string> vertInputNames, IEnumerable<string> fragInputNames)
        {
            Init(mapper, uniforms, vertInputNames, fragInputNames);
        }

        public void Init(VFXExpressionMapper mapper, VFXUniformMapper uniforms, IEnumerable<string> vertInputNames, IEnumerable<string> fragInputNames)
        {
            m_Interpolators.Clear();
            m_FragInputs.Clear();
            m_VertInputs.Clear();

            foreach(var inputName in vertInputNames)
            {
                var exp = mapper.FromNameAndId(inputName, -1); // Postulate that inputs are only generated from context slots.
                if (exp == null)
                    throw new ArgumentException("Cannot find an expression matching the vertInput: " + inputName);

                m_VertInputs.Add(inputName, exp);
            }

            foreach(var inputName in fragInputNames)
            {
                var exp = mapper.FromNameAndId(inputName, -1); // Postulate that inputs are only generated from context slots.
                if (exp == null)
                    throw new ArgumentException("Cannot find an expression matching the fragInput: " + inputName);

                m_FragInputs.Add(inputName, exp);
                if (!(exp.Is(VFXExpression.Flags.Constant) || uniforms.Contains(exp) || m_Interpolators.ContainsKey(exp))) // No interpolator needed for constants or uniforms
                    m_Interpolators.Add(exp, inputName);
            }
        }
    }
}
