using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXCompilerData
    {
        private VFXExpressionGraph m_Graph;
        private Dictionary<VFXContext, VFXCompilerContextData> m_ContextData = new Dictionary<VFXContext, VFXCompilerContextData>();

        public VFXCompilerData(VFXExpressionGraph graph)
        {
            m_Graph = graph;
        }

        public VFXExpressionGraph graph { get { return m_Graph; } }

        public void AddUniformExpressionMapper(VFXContext context, VFXExpressionMapper binder)
        {
            GetOrCreateContextData(context).uniforms.Add(binder);
        }

        public void AddRuntimeExpressionsMapper(VFXContext context, VFXExpressionMapper binder)
        {
            GetOrCreateContextData(context).rtMappings.Add(binder);
        }

        private VFXCompilerContextData GetOrCreateContextData(VFXContext context)
        {
            if (context == null)
                throw new ArgumentNullException();

            VFXCompilerContextData data;
            if (!m_ContextData.TryGetValue(context, out data))
            {
                data = new VFXCompilerContextData();
                m_ContextData[context] = data;
            }

            return data;
        }
    }

    class VFXCompilerContextData
    {
        public List<VFXExpressionMapper> uniforms = new List<VFXExpressionMapper>();
        public List<VFXExpressionMapper> rtMappings = new List<VFXExpressionMapper>();
    }

    class VFXExpressionMapper
    {
        public VFXExpressionMapper(string prefix = null)
        {
            m_Prefix = prefix;
        }

        public IEnumerable<VFXExpression> expressions { get { return m_ExpressionsToNames.Keys; } }

        public void AddExpressionFromSlotContainer(IVFXSlotContainer slotContainer, VFXExpressionGraph graph = null)
        {
            foreach (var master in slotContainer.inputSlots)
            {
                foreach (var slot in master.GetExpressionSlots())
                {
                    var exp = slot.GetExpression();
                    if (graph != null)
                        exp = graph.GetReduced(exp);

                    if (!Contains(exp))
                        AddExpression(exp, slot.property.name);
                }
            }
        }

        public static VFXExpressionMapper FromContext(VFXContext context, VFXExpressionGraph graph = null, string prefix = null)
        {
            var mapper = new VFXExpressionMapper(prefix);

            mapper.AddExpressionFromSlotContainer(context, graph);
            foreach (var block in context.children)
                mapper.AddExpressionFromSlotContainer(block, graph);

            // DEBUG
            /*if (binder.m_ExpressionsToNames.Count > 0)
            {
                Debug.Log("---- EXPRESSION BINDER:");
                foreach (var kvp in binder.m_ExpressionsToNames)
                    Debug.Log("----" + kvp.Key.ValueType + " " + kvp.Value);
            }*/

            return mapper;
        }

        public string GetName(VFXExpression exp)
        {
            string name;
            m_ExpressionsToNames.TryGetValue(exp, out name);
            return name;
        }

        public bool Contains(VFXExpression exp)
        {
            return m_ExpressionsToNames.ContainsKey(exp);
        }

        public void AddExpression(VFXExpression exp, string name)
        {
            if (exp == null || name == null)
                throw new ArgumentNullException();

            if (m_ExpressionsToNames.ContainsKey(exp))
                throw new ArgumentException(string.Format("Expression {0} is already registered", exp));

            string finalName = "";
            if (!string.IsNullOrEmpty(m_Prefix))
                finalName += m_Prefix + "_";
            finalName += name;

            int currentSuffix = 0;
            if (m_NamesToSuffix.ContainsKey(name))
            {
                currentSuffix = m_NamesToSuffix[name] + 1;
                finalName += "_" + currentSuffix;
            }

            m_NamesToSuffix[name] = currentSuffix;
            m_ExpressionsToNames[exp] = finalName;
        }

        private Dictionary<VFXExpression, string> m_ExpressionsToNames = new Dictionary<VFXExpression, string>();
        private Dictionary<string, int> m_NamesToSuffix = new Dictionary<string, int>();
        private string m_Prefix;
    }
}
