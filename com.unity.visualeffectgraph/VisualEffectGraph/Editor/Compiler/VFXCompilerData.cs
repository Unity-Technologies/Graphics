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

    struct VFXNamedExpression
    {
        public VFXNamedExpression(VFXExpression exp, string name)
        {
            this.exp = exp;
            this.name = name;
        }

        public VFXExpression exp;
        public string name;
    }

    class VFXExpressionMapper
    {
        public struct Data
        {
            public string fullName { get { return id == -1 ? name : string.Format("{0}_{1}", name, VFXCodeGeneratorHelper.GeneratePrefix((uint)id)); } }
            public string name;
            public int id;
        }

        public VFXExpressionMapper()
        {
        }

        public IEnumerable<VFXExpression> expressions { get { return m_ExpressionsData.Keys; } }

        private void AddExpressionFromSlotContainer(IVFXSlotContainer slotContainer, int blockId)
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

        public static VFXExpressionMapper FromContext(VFXContext context)
        {
            var mapper = new VFXExpressionMapper();

            mapper.AddExpressionFromSlotContainer(context, -1);
            int i = 0;
            foreach (var block in context.childrenWithImplicit)
                mapper.AddExpressions(block.parameters, i++);

            return mapper;
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

        public VFXExpression FromNameAndId(string name, int id)
        {
            foreach (var expression in m_ExpressionsData)
            {
                if (expression.Value.Any(o => o.name == name && o.id == id))
                {
                    return expression.Key;
                }
            }
            return null;
        }

        public void AddExpression(VFXExpression exp, string name, int id)
        {
            if (exp == null || name == null)
                throw new ArgumentNullException();

            if (m_ExpressionsData.SelectMany(o => o.Value).Any(o => o.name == name && o.id == id))
                throw new ArgumentException(string.Format("{0}_{1} has been added twice: {2}", name, id, exp));

            var data = new Data();
            data.name = name;
            data.id = id;

            if (!m_ExpressionsData.ContainsKey(exp))
            {
                m_ExpressionsData.Add(exp, new List<Data>());
            }
            m_ExpressionsData[exp].Add(data);
        }

        public void AddExpressions(IEnumerable<VFXNamedExpression> expressions, int id)
        {
            foreach (var exp in expressions)
                AddExpression(exp.exp, exp.name, id);
        }

        private Dictionary<VFXExpression, List<Data>> m_ExpressionsData = new Dictionary<VFXExpression, List<Data>>();
    }
}
