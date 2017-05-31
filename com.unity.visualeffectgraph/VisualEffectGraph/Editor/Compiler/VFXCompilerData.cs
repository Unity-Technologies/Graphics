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
        public struct Data
        {
            public string name;
            public int blockId;
        }

        public VFXExpressionMapper(string prefix = null)
        {
            m_Prefix = prefix;
        }

        public IEnumerable<VFXExpression> expressions { get { return m_ExpressionsData.Keys; } }

        public void AddExpressionFromSlotContainer(IVFXSlotContainer slotContainer, int blockId, VFXExpressionGraph graph = null)
        {
            foreach (var master in slotContainer.inputSlots)
            {
                foreach (var slot in master.GetExpressionSlots())
                {
                    var exp = slot.GetExpression();
                    if (graph != null)
                        exp = graph.GetReduced(exp);

                    if (!Contains(exp))
                        AddExpression(exp, slot.fullName, blockId);
                }
            }
        }

        public static VFXExpressionMapper FromContext(VFXContext context, VFXExpressionGraph graph = null, string prefix = null)
        {
            var mapper = new VFXExpressionMapper(prefix);

            mapper.AddExpressionFromSlotContainer(context, -1, graph);
            foreach (var block in context.children)
                for (int i = 0; i < context.GetNbChildren(); ++i)
                    mapper.AddExpressionFromSlotContainer(context[i], i, graph);

            // DEBUG
            /*if (binder.m_ExpressionsToNames.Count > 0)
            {
                Debug.Log("---- EXPRESSION BINDER:");
                foreach (var kvp in binder.m_ExpressionsToNames)
                    Debug.Log("----" + kvp.Key.ValueType + " " + kvp.Value);
            }*/

            return mapper;
        }

        public Data GetData(VFXExpression exp)
        {
            Data data;
            m_ExpressionsData.TryGetValue(exp, out data);
            return data;
        }

        public bool Contains(VFXExpression exp)
        {
            return m_ExpressionsData.ContainsKey(exp);
        }

        public void AddExpression(VFXExpression exp, Data data)
        {
            AddExpression(exp, data.name, data.blockId);
        }

        public void AddExpression(VFXExpression exp, string name, int blockId = -1)
        {
            if (exp == null || name == null)
                throw new ArgumentNullException();

            if (m_ExpressionsData.ContainsKey(exp))
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
            var data = new Data();
            data.name = finalName;
            data.blockId = blockId;
            m_ExpressionsData[exp] = data;
        }

        private Dictionary<VFXExpression, Data> m_ExpressionsData = new Dictionary<VFXExpression, Data>();
        private Dictionary<string, int> m_NamesToSuffix = new Dictionary<string, int>();
        private string m_Prefix;
    }
}
