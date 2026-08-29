using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderAnalysis.Internal
{
    class SimpleDataCache
    {
        // Deliberately not a LayoutKind.Explicit union: overlapping the string reference with the
        // primitives at offset 0 is an invalid type layout (the GC would have to scan a bool as a
        // pointer). CoreCLR rejects it outright -- the whole assembly fails to load with
        // "Could not load type 'Item' ... object field at offset 0 that is incorrectly aligned or
        // overlapped by a non-object field". Every Set writes one field and the matching Get reads
        // that same field, so nothing ever relied on the aliasing.
        struct Item
        {
            internal bool @bool;
            internal byte @byte;
            internal short @short;
            internal ushort @ushort;
            internal int @int;
            internal uint @uint;
            internal string @string;
        }

        Dictionary<int, Item> m_Items = new Dictionary<int, Item>();

        public void Set(int key, bool v)
        {
            m_Items[key] = new Item { @bool = v };
        }

        public void Set(int key, byte v)
        {
            m_Items[key] = new Item { @byte = v };
        }

        public void Set(int key, short v)
        {
            m_Items[key] = new Item { @short = v };
        }

        public void Set(int key, ushort v)
        {
            m_Items[key] = new Item { @ushort = v };
        }

        public void Set(int key, int v)
        {
            m_Items[key] = new Item { @int = v };
        }

        public void Set(int key, uint v)
        {
            m_Items[key] = new Item { @uint = v };
        }

        public void Set(int key, string v)
        {
            m_Items[key] = new Item { @string = v };
        }

        public bool GetBool(int key)
        {
            Item t;
            return m_Items.TryGetValue(key, out t) && t.@bool;
        }

        public byte GetByte(int key)
        {
            Item t;
            return m_Items.TryGetValue(key, out t) ? t.@byte : default;
        }

        public short GetShort(int key)
        {
            Item t;
            return m_Items.TryGetValue(key, out t) ? t.@short : default;
        }

        public ushort GetUShort(int key)
        {
            Item t;
            return m_Items.TryGetValue(key, out t) ? t.@ushort : default;
        }

        public int GetInt(int key)
        {
            Item t;
            return m_Items.TryGetValue(key, out t) ? t.@int : default;
        }

        public uint GetUInt(int key)
        {
            Item t;
            return m_Items.TryGetValue(key, out t) ? t.@uint : default;
        }

        public string GetString(int key)
        {
            Item t;
            return m_Items.TryGetValue(key, out t) ? t.@string : default;
        }

        public void Clear()
        {
            m_Items.Clear();
        }
    }
}
