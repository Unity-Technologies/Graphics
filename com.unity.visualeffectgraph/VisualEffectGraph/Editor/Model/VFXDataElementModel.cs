using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXDataNodeModel : VFXElementModel<VFXElementModel,VFXDataBlockModel>, VFXUIDataHolder
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
                }       
            }
        }

        public void UpdateCollapsed(bool collapsed) {}
        public void UpdatePosition(Vector2 position)
        {
            m_Position = position;
        }

        private Vector2 m_Position;
        private bool m_Exposed;
    }

    class VFXDataBlockModel : VFXModelWithSlots<VFXDataNodeModel, VFXElementModel>, VFXUIDataHolder
    {
        public VFXDataBlockModel(VFXDataBlockDesc desc)
        {
            m_BlockDesc = desc;
            InitSlots(null,new VFXProperty[] {desc.Property});
        }

        public VFXDataBlockDesc Desc    { get { return m_BlockDesc; }}
        public VFXOutputSlot Slot       { get { return GetOutputSlot(0); }}

        public void UpdatePosition(Vector2 position) {}
        public void UpdateCollapsed(bool collapsed)
        {
            m_Collapsed = collapsed;
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return false; // Nothing can be attached to Blocks !
        }

        private VFXDataBlockDesc m_BlockDesc;
        private bool m_Collapsed;
    }
}