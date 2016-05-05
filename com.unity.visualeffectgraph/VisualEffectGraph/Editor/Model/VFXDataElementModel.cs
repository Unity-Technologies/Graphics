using UnityEngine;

namespace UnityEditor.Experimental.VFX
{
    class VFXDataNodeModel : VFXElementModel<VFXElementModel,VFXDataBlockModel>, VFXUIDataHolder
    {
        bool Exposed
        {
            get { return m_Exposed; }
            set { m_Exposed = value; }
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
        public VFXDataBlockModel(VFXDataBlock desc)
        {
            m_BlockDesc = desc;
            InitSlots(desc.Property);
        }

        public void UpdatePosition(Vector2 position) {}
        public void UpdateCollapsed(bool collapsed)
        {
            m_Collapsed = collapsed;
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return false; // Nothing can be attached to Blocks !
        }

        private VFXDataBlock m_BlockDesc;
        private bool m_Collapsed;
    }
}