

namespace UnityEditor.VFX
{
    class VFXSystem : VFXModel<VFXModel,VFXContext>
    {
        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            if (!base.AcceptChild(model, index))
                return false;

            // Check if context types are inserted in the right order
            VFXContextType contextType = ((VFXContext)model).ContextType;
            int realIndex = index == -1 ? m_Children.Count : index;

            if (realIndex > 0 && GetChild(realIndex - 1).ContextType > contextType)
                return false;
            if (realIndex < m_Children.Count - 1 && GetChild(realIndex + 1).ContextType < contextType)
                return false;

            return true;
        }
    }
}