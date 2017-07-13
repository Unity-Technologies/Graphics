using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

namespace UnityEditor.VFX
{
    abstract class VFXData : VFXModel
    {
        public abstract VFXDataType type { get; }

        public IEnumerable<VFXContext> owners
        {
            get { return m_Owners; }
        }

        public static VFXData CreateDataType(VFXDataType type)
        {
            switch (type)
            {
                case VFXDataType.kParticle:     return ScriptableObject.CreateInstance<VFXDataParticle>();
                case VFXDataType.kSpawnEvent:   return ScriptableObject.CreateInstance<VFXDataSpawnEvent>();
                default:                        return null;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_Owners == null)
                m_Owners = new List<VFXContext>();

            if (m_TestId == 0)
                m_TestId = UnityEngine.Random.Range(0, int.MaxValue);
        }

        // Never call this directly ! Only context must call this through SetData
        public void OnContextAdded(VFXContext context)
        {
            m_Owners.Add(context);
        }

        // Never call this directly ! Only context must call this through SetData
        public void OnContextRemoved(VFXContext context)
        {
            m_Owners.Remove(context);
        }

        public bool IsAttributeRead(VFXAttribute attrib)    { return (GetAttributeMode(attrib) & VFXAttributeMode.Read) != 0; }
        public bool IsAttributeWritten(VFXAttribute attrib) { return (GetAttributeMode(attrib) & VFXAttributeMode.Write) != 0; }
        public bool AttributeExists(VFXAttribute attrib)    { return GetAttributeMode(attrib) != VFXAttributeMode.None; }

        public VFXAttributeMode GetAttributeMode(VFXAttribute attrib)
        {
            VFXAttributeMode mode = VFXAttributeMode.None;
            Dictionary<VFXContext, VFXAttributeMode> contexts;
            if (m_AttributesToContexts.TryGetValue(attrib, out contexts))
            {
                foreach (var context in contexts)
                    mode |= context.Value;
            }

            return mode;
        }

        public int GetNbAttributes()
        {
            return m_AttributesToContexts.Count;
        }

        public IEnumerable<VFXAttributeInfo> GetAttributes()
        {
            foreach (var attrib in m_AttributesToContexts)
            {
                VFXAttributeInfo info;
                info.attrib = attrib.Key;
                info.mode = VFXAttributeMode.None;

                foreach (var context in attrib.Value)
                    info.mode |= context.Value;

                yield return info;
            }
        }

        private void FillAttributesToContext()
        {
            // Create attributesToContexts from contextsToAttributes
            foreach (var contextKvp in m_ContextsToAttributes)
            {
                var context = contextKvp.Key;
                foreach (var attribKvp in contextKvp.Value)
                {
                    var attrib = attribKvp.Key;
                    Dictionary<VFXContext, VFXAttributeMode> contexts;
                    if (!m_AttributesToContexts.TryGetValue(attrib, out contexts))
                    {
                        contexts = new Dictionary<VFXContext, VFXAttributeMode>();
                        m_AttributesToContexts.Add(attrib, contexts);
                    }

                    contexts[context] = attribKvp.Value;
                }
            }
        }

        public void CollectAttributes(VFXExpressionGraph graph)
        {
            m_ContextsToAttributes.Clear();
            m_AttributesToContexts.Clear();

            foreach (var context in owners)
            {
                AddAttributes(context, context.attributes);
                foreach (var block in context.children)
                    AddAttributes(context, block.attributes);

                CollectInputAttributes(context, graph);
            }

            FillAttributesToContext(); // Must fill a first time so that attributes can be fetched in optional attribute pass

            // Add optional attributes
            var optionalAttributes = new List<VFXAttributeInfo>[m_Owners.Count];
            for (int i = 0; i < m_Owners.Count; ++i)
                optionalAttributes[i] = m_Owners[i].optionalAttributes.ToList();
            for (int i = 0; i < m_Owners.Count; ++i)
                AddAttributes(m_Owners[i], optionalAttributes[i]);

            FillAttributesToContext(); // A second time to update with optional attributes

            //TMP Debug only
            DebugLogAttributes();
        }

        private void AddAttribute(VFXContext context, VFXAttributeInfo attribInfo)
        {
            if (attribInfo.mode == VFXAttributeMode.None)
                throw new ArgumentException("Cannot add an attribute without mode");

            if ((attribInfo.mode & VFXAttributeMode.Write) != 0 && context.contextType == VFXContextType.kOutput)
                throw new ArgumentException("Output contexts cannot write attributes");

            Dictionary<VFXAttribute, VFXAttributeMode> attribs;
            if (!m_ContextsToAttributes.TryGetValue(context, out attribs))
            {
                attribs = new Dictionary<VFXAttribute, VFXAttributeMode>();
                m_ContextsToAttributes.Add(context, attribs);
            }

            var attrib = attribInfo.attrib;
            var mode = attribInfo.mode;

            if (attribs.ContainsKey(attrib))
                mode |= attribs[attrib];

            //if (mode != VFXAttributeMode.None)
            attribs[attrib] = mode;
        }

        private void AddAttributes(VFXContext context, IEnumerable<VFXAttributeInfo> attribInfos)
        {
            foreach (var attribInfo in attribInfos)
                AddAttribute(context, attribInfo);
        }

        // Collect attribute expressions linked to a context
        private void CollectInputAttributes(VFXContext context, VFXExpressionGraph graph)
        {
            var mapper = context.GetExpressionMapper(context.ownedType == VFXDataType.kParticle ? VFXDeviceTarget.GPU : VFXDeviceTarget.CPU);
            foreach (var exp in mapper.expressions)
                AddAttributes(context, CollectInputAttributes(exp));
        }

        // Collect attribute expressions recursively
        private IEnumerable<VFXAttributeInfo> CollectInputAttributes(VFXExpression exp)
        {
            if (exp.Is(VFXExpression.Flags.PerElement)) // Testing per element allows to early out as it is propagated
            {
                foreach (var info in exp.GetNeededAttributes())
                    yield return info;

                foreach (var parent in exp.Parents)
                {
                    foreach (var info in CollectInputAttributes(parent))
                        yield return info;
                }
            }
        }

        private void DebugLogAttributes()
        {
            var builder = new StringBuilder();

            builder.AppendLine(string.Format("Attributes for data {0} of type {1}", GetHashCode(), GetType()));
            foreach (var context in owners)
            {
                Dictionary<VFXAttribute, VFXAttributeMode> attributeInfos;
                if (m_ContextsToAttributes.TryGetValue(context, out attributeInfos))
                {
                    builder.AppendLine(string.Format("\tContext {0}", context.GetHashCode()));
                    foreach (var kvp in attributeInfos)
                        builder.AppendLine(string.Format("\t\tAttribute {0} {1} {2}", kvp.Key.name, kvp.Key.type, kvp.Value));
                }
            }

            Debug.Log(builder.ToString());
        }

        [SerializeField]
        protected List<VFXContext> m_Owners;

        //[NonSerialized]
        public int m_TestId;

        protected Dictionary<VFXContext, Dictionary<VFXAttribute, VFXAttributeMode>> m_ContextsToAttributes = new Dictionary<VFXContext, Dictionary<VFXAttribute, VFXAttributeMode>>();
        protected Dictionary<VFXAttribute, Dictionary<VFXContext, VFXAttributeMode>> m_AttributesToContexts = new Dictionary<VFXAttribute, Dictionary<VFXContext, VFXAttributeMode>>();
    }
}
