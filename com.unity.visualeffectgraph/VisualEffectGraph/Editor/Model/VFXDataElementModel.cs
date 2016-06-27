using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public class VFXDataNodeModel : VFXElementModel<VFXElementModel,VFXDataBlockModel>, VFXUIDataHolder
    {
        public bool Exposed
        {
            get { return m_Exposed; }
            set 
            {
                if (m_Exposed != value)
                {
                    m_Exposed = value;
                    Invalidate(InvalidationCause.kModelChanged);
                    for (int i = 0; i < GetNbChildren(); ++i)
                        GetChild(i).Slot.NotifyChange(VFXPropertySlot.Event.kExposedUpdated);
                }       
            }
        }

        public void CollectExposedExpressions(List<VFXNamedValue> exposedExpressions)
        {
            if (!m_Exposed) // if not exposed return
                return;

            for (int i = 0; i < GetNbChildren(); ++i)
            {
                VFXDataBlockModel block = GetChild(i);
                block.Slot.CollectExposableNamedValues(exposedExpressions, block.ExposedName);
            }
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
        private bool m_Exposed;
    }

    public class VFXDataBlockModel : VFXModelWithSlots<VFXDataNodeModel, VFXElementModel>, VFXUIDataHolder
    {
        public VFXDataBlockModel(VFXDataBlockDesc desc)
        {
            m_BlockDesc = desc;
            m_ExposedName = desc.Name;
            InitSlots(null,new VFXProperty[] {desc.Property});
        }

        public VFXDataBlockDesc Desc    { get { return m_BlockDesc; }}
        public VFXOutputSlot Slot       { get { return GetOutputSlot(0); }}

        public void UpdatePosition(Vector2 position) {}
        public void UpdateCollapsed(bool collapsed)
        {
            if (m_UICollapsed != collapsed)
            {
                m_UICollapsed = collapsed;
                Invalidate(InvalidationCause.kUIChanged);
            }
        }

        public void NotifyExposedUpdated()
        {
            NotifyExposedUpdatedRecursively(Slot);
        }

        private static void NotifyExposedUpdatedRecursively(VFXPropertySlot slot)
        {
            slot.NotifyChange(VFXPropertySlot.Event.kExposedUpdated);
            for (int i = 0; i < slot.GetNbChildren(); ++i)
                NotifyExposedUpdatedRecursively(slot.GetChild(i));
        }

        public string ExposedName
        {
            get { return m_ExposedName; }
            set
            {
                if (value != m_ExposedName)
                {
                    m_ExposedName = value;
                    Invalidate(InvalidationCause.kUIChanged);
                    if (GetOwner() != null && GetOwner().Exposed)
                        Slot.NotifyChange(VFXPropertySlot.Event.kExposedUpdated);
                }
            }
        }

        public bool UICollapsed { get { return m_UICollapsed; } }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return false; // Nothing can be attached to Blocks !
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            if (GetOwner() != null && GetOwner().Exposed)
                NotifyExposedUpdated();
        }

        protected override void OnAdded() 
        {
            base.OnAdded();
            if (GetOwner() != null && GetOwner().Exposed)
                NotifyExposedUpdated();
        }

        private VFXDataBlockDesc m_BlockDesc;
        private string m_ExposedName;
        private bool m_UICollapsed;
    }
}
