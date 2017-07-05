using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
            public string fullName { get { return id == -1 ? name : string.Format("{0}_{1}", name, id); } }
            public string name;
            public int id;
        }

        public VFXExpressionMapper(string prefix = null)
        {
            m_Prefix = string.IsNullOrEmpty(prefix) ? "" : string.Format("{0}_", prefix);
        }

        public IEnumerable<VFXExpression> expressions { get { return m_ExpressionsData.Keys; } }

        public void AddExpressionFromSlotContainer(IVFXSlotContainer slotContainer, int blockId)
        {
            foreach (var master in slotContainer.inputSlots)
            {
                foreach (var slot in master.GetExpressionSlots())
                {
                    var exp = slot.GetExpression();
                    if (!Contains(exp))
                        AddExpression(exp, slot.fullName, blockId);
                }
            }
        }

        public static VFXExpressionMapper FromContext(VFXContext context, string prefix = null)
        {
            var mapper = new VFXExpressionMapper(prefix);

            mapper.AddExpressionFromSlotContainer(context, -1);
            foreach (var block in context.children)
                for (int i = 0; i < context.GetNbChildren(); ++i)
                    mapper.AddExpressionFromSlotContainer(context[i], i);

            return mapper;
        }

        private void CollectAndAddUniforms(VFXExpression exp, List<Data> data)
        {
            if (!exp.Is(VFXExpression.Flags.PerElement))
            {
                if (exp.Is(VFXExpression.Flags.InvalidOnCPU))
                    throw new InvalidOperationException(string.Format("Collected uniform expression is invalid on CPU: {0}", exp));

                if (m_ExpressionsData.ContainsKey(exp)) // Only need one name for uniform
                    return;

                if (data != null)
                    AddExpression(exp, data[0]);
                else
                    AddExpression(exp, "", m_ExpressionsData.Count()); // needs a unique id: use number of registered expressions
            }
            else
                foreach (var parent in exp.Parents)
                    CollectAndAddUniforms(parent, null);
        }

        public static VFXExpressionMapper UniformMapper(VFXExpressionMapper mapper)
        {
            var uniformMapper = new VFXExpressionMapper("uniform");

            foreach (var kvp in mapper.m_ExpressionsData)
                uniformMapper.CollectAndAddUniforms(kvp.Key, kvp.Value);

            return uniformMapper;
        }

        public ReadOnlyCollection<Data> GetData(VFXExpression exp)
        {
            List<Data> data;
            if (m_ExpressionsData.TryGetValue(exp, out data))
            {
                return data.AsReadOnly();
            }
            return (new List<Data>()).AsReadOnly();
        }

        public bool Contains(VFXExpression exp)
        {
            return m_ExpressionsData.ContainsKey(exp);
        }

        public void AddExpression(VFXExpression exp, Data data)
        {
            AddExpression(exp, data.name, data.id);
        }

        public void AddExpression(VFXExpression exp, string name, int id)
        {
            if (exp == null || name == null)
                throw new ArgumentNullException();

            if (m_ExpressionsData.SelectMany(o => o.Value).Any(o => o.name == name && o.id == id))
                throw new ArgumentException(string.Format("{0}{1}_{2} has been added twice: {3}", m_Prefix, name, id, exp));

            var data = new Data();
            data.name = m_Prefix + name;
            data.id = id;

            if (!m_ExpressionsData.ContainsKey(exp))
            {
                m_ExpressionsData.Add(exp, new List<Data>());
            }
            m_ExpressionsData[exp].Add(data);
        }

        private Dictionary<VFXExpression, List<Data>> m_ExpressionsData = new Dictionary<VFXExpression, List<Data>>();
        private string m_Prefix;
    }
}
