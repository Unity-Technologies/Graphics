using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public class VFXSpawnerNodeModel : VFXElementModel<VFXElementModel, VFXSpawnerBlockModel>, VFXUIDataHolder
    {
        public void UpdateCollapsed(bool collapsed) {}
        public void UpdatePosition(Vector2 position)
        {
            if (m_UIPosition != position)
            {
                m_UIPosition = position;
                Invalidate(InvalidationCause.kUIChanged);
            }
        }

        public bool CanLink(VFXContextModel context)
        {
            return context.GetContextType() == VFXContextDesc.Type.kTypeInit && !m_Contexts.Contains(context);
        }

        public bool Link(VFXContextModel context,bool reentrant = false)
        {
            if (CanLink(context))
            {
                m_Contexts.Add(context);
                if (!reentrant)
                    context.Link(this, true);
                return true;
            }

            return false;
        }

        public bool Unlink(VFXContextModel context,bool reentrant = false)
        {
            if (m_Contexts.Remove(context))
            {
                if (!reentrant)
                    context.Unlink(this, true);
                return true;
            }

            return false;
        }

        public int GetNbLinked()                    { return m_Contexts.Count; }
        public VFXContextModel GetLinked(int index) { return m_Contexts[index]; }

        public Vector2 UIPosition { get { return m_UIPosition; } }
        private Vector2 m_UIPosition;

        private List<VFXContextModel> m_Contexts = new List<VFXContextModel>();
    }

    public class VFXSpawnerBlockModel : VFXModelWithSlots<VFXSpawnerNodeModel, VFXElementModel>, VFXUIDataHolder
    {
        public enum Type
        {
            kConstantRate,
            kBurst,
        }

        public static string TypeToName(Type spawnerType)
        {
            switch (spawnerType)
            {
                case Type.kConstantRate:
                    return "Constant Rate";
                case Type.kBurst:
                    return "Burst";
                default:
                    throw new ArgumentException("Unknown spawner type");
            }
        }

        public VFXSpawnerBlockModel(Type spawnerType)
        {
            m_Type = spawnerType;
            VFXProperty[] properties = CreateProperties(m_Type);
            InitSlots(properties, null);
        }

        private static VFXProperty[] CreateProperties(Type spawnerType)
        {
            switch (spawnerType)
            {
                case Type.kConstantRate:
                    return new VFXProperty[] { VFXProperty.Create<VFXFloatType>("rate") };
                case Type.kBurst:
                    return new VFXProperty[] { VFXProperty.Create<VFXFloat2Type>("nb") };
                default:
                    throw new ArgumentException("Unknown spawner type");
            }
        }

        public void UpdateCollapsed(bool collapsed) { m_UICollapsed = collapsed; }
        public void UpdatePosition(Vector2 position) {}

        public bool UICollapsed { get { return m_UICollapsed; } }
        private bool m_UICollapsed;

        public Type SpawnerType { get { return m_Type; } }

        private Type m_Type;
    }
}
