using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public class VFXSpawnerNodeModel : VFXElementModel<VFXElementModel, VFXSpawnerBlockModel>, VFXUIDataHolder
    {
        public enum EventSlot
        {
            kEventSlotStart,
            kEventSlotStop,
        }

        public void UpdateCollapsed(bool collapsed) {}
        public void UpdatePosition(Vector2 position)
        {
            if (m_UIPosition != position)
            {
                m_UIPosition = position;
                Invalidate(InvalidationCause.kUIChanged);
            }
        }

        protected override void InnerInvalidate(InvalidationCause cause)
        {
            // Change model to data before dispathching to linked contexts to avoid an recompilation of system but force the regeneration of native data
            if (cause == InvalidationCause.kModelChanged)
                cause = InvalidationCause.kDataChanged;

            foreach (var context in m_Contexts)
                context.Invalidate(cause);
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

        private List<VFXEventModel> GetEventList(EventSlot slot)
        {
            switch(slot)
            {
                case EventSlot.kEventSlotStart:   return m_StartEvents;
                case EventSlot.kEventSlotStop:    return m_StopEvents;
            }

            throw new Exception();
        }

        public bool Link(VFXEventModel e,EventSlot slot,bool reentrant = false)
        {
            var eventList = GetEventList(slot);
            if (!eventList.Contains(e))
            {
                eventList.Add(e);
                Invalidate(InvalidationCause.kDataChanged);
                if (!reentrant)
                    e.Link(this, slot, true);
                return true;
            }

            return false;
        }

        public bool Unlink(VFXEventModel e, EventSlot slot, bool reentrant = false)
        {
            var eventList = GetEventList(slot);
            if (eventList.Remove(e))
            {
                Invalidate(InvalidationCause.kDataChanged);
                if (!reentrant)
                    e.Unlink(this, slot, true);
                return true;
            }

            return false;
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            while (m_Contexts.Count > 0)
                Unlink(m_Contexts[0]);
        }

        public int GetNbLinked()                            { return m_Contexts.Count; }
        public IEnumerable<VFXContextModel> LinkedContexts  { get { return m_Contexts; } }
        public IEnumerable<VFXEventModel> StartEvents       { get { return m_StartEvents; } }
        public IEnumerable<VFXEventModel> StopEvents        { get { return m_StopEvents; } }

        public Vector2 UIPosition { get { return m_UIPosition; } }
        private Vector2 m_UIPosition;

        private List<VFXContextModel> m_Contexts = new List<VFXContextModel>();
        private List<VFXEventModel> m_StartEvents = new List<VFXEventModel>();
        private List<VFXEventModel> m_StopEvents = new List<VFXEventModel>();
    }

    public class VFXSpawnerBlockModel : VFXModelWithSlots<VFXSpawnerNodeModel, VFXElementModel>, VFXUIDataHolder
    {
        // Must match C++ side enum
        public enum Type
        {
            kConstantRate,
            kBurst,
            kPeriodicBurst,
            kVariableRate,
        }

        public static string TypeToName(Type spawnerType)
        {
            switch (spawnerType)
            {
                case Type.kConstantRate:
                    return "Constant Rate";
                case Type.kBurst:
                    return "Burst";
                case Type.kPeriodicBurst:
                    return "Periodic Burst";
                case Type.kVariableRate:
                    return "Variable Rate";
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
                    return new VFXProperty[] { new VFXProperty( new VFXFloatType(10.0f),"Rate") };
                case Type.kBurst:
                    return new VFXProperty[] { 
                        new VFXProperty(new VFXFloat2Type(new Vector2(0,3)),"Count"),
                        new VFXProperty(new VFXFloat2Type(new Vector2(0.03f,0.25f)),"Delay")
                    };
                case Type.kPeriodicBurst:
                    return new VFXProperty[] { 
                        VFXProperty.Create<VFXFloat2Type>("nb"),
                        VFXProperty.Create<VFXFloat2Type>("period")
                    };
                case Type.kVariableRate:
                    return new VFXProperty[] { 
                        VFXProperty.Create<VFXFloat2Type>("nb"),
                        VFXProperty.Create<VFXFloat2Type>("period")
                    };
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

    public class VFXEventModel : VFXElementModel<VFXElementModel, VFXElementModel>, VFXUIDataHolder
    {
        public VFXEventModel(string name,bool locked)
        {
            m_Name = name;
            m_Locked = locked;
        }

        private List<VFXSpawnerNodeModel> GetSpawnerList(VFXSpawnerNodeModel.EventSlot slot)
        {
            switch (slot)
            {
                case VFXSpawnerNodeModel.EventSlot.kEventSlotStart: return m_StartSpawners;
                case VFXSpawnerNodeModel.EventSlot.kEventSlotStop: return m_StopSpawners;
            }

            throw new Exception();
        }

        public bool Link(VFXSpawnerNodeModel spawner, VFXSpawnerNodeModel.EventSlot slot, bool reentrant = false)
        {
            if (reentrant || spawner.Link(this, slot, true)) // Invalidation is performed in spawner's Link
            {
                GetSpawnerList(slot).Add(spawner);
                return true;
            }

            return false;
        }

        public bool Unlink(VFXSpawnerNodeModel spawner, VFXSpawnerNodeModel.EventSlot slot, bool reentrant = false)
        {
            if (reentrant || spawner.Unlink(this, slot, true)) // Invalidation is performed in spawner's Unlink
                return GetSpawnerList(slot).Remove(spawner);

            return false;
        }

        public void UpdateCollapsed(bool collapsed) {}
        public void UpdatePosition(Vector2 position)
        {
            if (m_UIPosition != position)
            {
                m_UIPosition = position;
                Invalidate(InvalidationCause.kUIChanged);
            }
        }

        public Vector2 UIPosition { get { return m_UIPosition; } }
        private Vector2 m_UIPosition;

        public bool Locked { get { return m_Locked; } }

        public string Name
        {
            get { return m_Name; }
            set 
            {
                if (!m_Locked && m_Name != value)
                {
                    m_Name = value;
                    Invalidate(InvalidationCause.kDataChanged);
                }
            }
        }

        protected override void InnerInvalidate(InvalidationCause cause)
        {
            // Dispatch invalidation to linked spawners
            foreach (var spawner in m_StartSpawners)
                spawner.Invalidate(cause);
            foreach (var spawner in m_StopSpawners)
                spawner.Invalidate(cause);
        }

        public bool IsLinked() { return m_StartSpawners.Count > 0 || m_StopSpawners.Count > 0; }
        public IEnumerable<VFXSpawnerNodeModel> StartSpawners { get { return m_StartSpawners;  } }
        public IEnumerable<VFXSpawnerNodeModel> EndSpawners { get { return m_StopSpawners; } }

        private List<VFXSpawnerNodeModel> m_StartSpawners = new List<VFXSpawnerNodeModel>();
        private List<VFXSpawnerNodeModel> m_StopSpawners = new List<VFXSpawnerNodeModel>();

        private string m_Name;
        private bool m_Locked;
    }
}
