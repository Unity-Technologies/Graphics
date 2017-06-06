using System;
using System.Collections.Generic;
using UnityEngine;

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

        public void ClearAttributes()
        {
            contextToAttributes.Clear();
        }

        public void AddAttribute(VFXContext context, VFXAttributeInfo attribInfo)
        {
            Dictionary<VFXAttribute, VFXAttributeMode> attribs;
            if (!contextToAttributes.TryGetValue(context, out attribs))
            {
                attribs = new Dictionary<VFXAttribute, VFXAttributeMode>();
                contextToAttributes.Add(context, attribs);
            }

            var attrib = attribInfo.attrib;
            var mode = attribInfo.mode;

            if (attribs.ContainsKey(attrib))
                mode |= attribs[attrib];

            if (mode != VFXAttributeMode.None)
                attribs[attrib] = mode;
        }

        public void AddAttributes(VFXContext context, IEnumerable<VFXAttributeInfo> attribInfos)
        {
            foreach (var attribInfo in attribInfos)
                AddAttribute(context,attribInfo);
        }

        public void DebugLogAttributes()
        {
            Debug.Log(string.Format("Attributes for data {0} of type {1}", GetHashCode(), GetType()));
            foreach (var context in owners)
            {
                Dictionary<VFXAttribute,VFXAttributeMode> attributeInfos;
                if (contextToAttributes.TryGetValue(context,out attributeInfos))
                {
                    Debug.Log(string.Format("\tContext {0}", context.GetHashCode()));
                    foreach (var kvp in attributeInfos)
                        Debug.Log(string.Format("\t\tAttribute {0} {1} {2}", kvp.Key.name,kvp.Key.type,kvp.Value));
                }
            }
        }

        [SerializeField]
        private List<VFXContext> m_Owners;

        //[NonSerialized]
        public int m_TestId;

        private Dictionary<VFXContext, Dictionary<VFXAttribute,VFXAttributeMode>> contextToAttributes = new Dictionary<VFXContext, Dictionary<VFXAttribute,VFXAttributeMode>>();
    }
}
