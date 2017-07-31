using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXDataParticle : VFXData
    {
        public override VFXDataType type { get { return VFXDataType.kParticle; } }

        public uint capacity
        {
            get { return m_Capacity; }
            set { m_Capacity = value; }
        }

        public Bounds bbox
        {
            get { return m_Bounds; }
            set { m_Bounds = value; }
        }

        public bool worldSpace
        {
            get { return m_WorldSpace; }
            set { m_WorldSpace = value; }
        }

        // TODO tmp function to generate attribute buffers
        public void DebugBuildAttributeBuffers()
        {
            int nbOwners = m_Owners.Count;
            if (nbOwners > 16)
                throw new InvalidOperationException(string.Format("Too many contexts that use particle data {0} > 16", nbOwners));

            var keyToAttributes = new Dictionary<int, List<VFXAttribute>>();

            var LocalAttributes = new List<VFXAttribute>();

            foreach (var kvp in m_AttributesToContexts)
            {
                bool local = false;
                var attribute = kvp.Key;
                int key = 0;

                bool onlyInit = true;
                bool onlyOutput = true;
                bool onlyUpdateRead = true;
                bool onlyUpdateWrite = true;

                foreach (var kvp2 in kvp.Value)
                {
                    var context = kvp2.Key;
                    if (context.contextType != VFXContextType.kInit)
                        onlyInit = false;
                    if (context.contextType != VFXContextType.kOutput)
                        onlyOutput = false;
                    if (context.contextType != VFXContextType.kUpdate)
                    {
                        onlyUpdateRead = false;
                        onlyUpdateWrite = false;
                    }
                    else
                    {
                        if ((kvp2.Value & VFXAttributeMode.Read) != 0)
                            onlyUpdateWrite = false;
                        if ((kvp2.Value & VFXAttributeMode.Write) != 0)
                            onlyUpdateRead = false;
                    }

                    int shift = m_Owners.IndexOf(context) << 1;
                    int value = 0;
                    if ((kvp2.Value & VFXAttributeMode.Read) != 0)
                        value = 0x01;
                    if ((kvp2.Value & VFXAttributeMode.Write) != 0)
                        value = 0x02;
                    key |= (value << shift);
                }

                if (onlyInit || onlyOutput || onlyUpdateRead || onlyUpdateWrite)
                    local = true;
                if ((key & 0xAAAAAAAA) == 0) // no write mask
                    local = true;

                if (local)
                {
                    LocalAttributes.Add(attribute);
                    continue;
                }

                List<VFXAttribute> attributes;
                if (!keyToAttributes.ContainsKey(key))
                {
                    attributes = new List<VFXAttribute>();
                    keyToAttributes[key] = attributes;
                }
                else
                    attributes = keyToAttributes[key];

                attributes.Add(attribute);
            }

            var builder = new StringBuilder();
            builder.AppendLine("ATTRIBUTES FOR PARTICLE DATA PER KEY");
            foreach (var kvp in keyToAttributes)
            {
                builder.AppendLine(kvp.Key.ToString());
                foreach (var attrib in kvp.Value)
                    builder.AppendLine(string.Format("\t{0} {1}", attrib.type, attrib.name));
            }

            if (LocalAttributes.Count > 0)
            {
                builder.AppendLine("Local Attributes");
                foreach (var attrib in LocalAttributes)
                    builder.AppendLine(string.Format("\t{0} {1}", attrib.type, attrib.name));
            }


            Debug.Log(builder.ToString());
        }

        [SerializeField]
        private uint m_Capacity = 1024;
        [SerializeField]
        private Bounds m_Bounds;
        [SerializeField]
        private bool m_WorldSpace;
    }
}
