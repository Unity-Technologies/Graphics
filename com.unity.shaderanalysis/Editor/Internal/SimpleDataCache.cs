using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEditor.ShaderAnalysis.Internal
{
    class SimpleDataCache
    {
        [StructLayout(LayoutKind.Explicit)]
        struct Item
        {
            [FieldOffset(0)] internal bool @bool;
            [FieldOffset(0)] internal byte @byte;
            [FieldOffset(0)] internal short @short;
            [FieldOffset(0)] internal ushort @ushort;
            [FieldOffset(0)] internal int @int;
            [FieldOffset(0)] internal uint @uint;
            [FieldOffset(0)] internal string @string;
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
