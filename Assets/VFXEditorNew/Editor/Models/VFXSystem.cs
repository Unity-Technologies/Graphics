

namespace UnityEditor.VFX
{
    class VFXSystem : VFXModel<VFXModel,VFXContext>
    {
        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            if (!base.AcceptChild(model, index))
                return false;

            // Check if context types are inserted in the right order
            VFXContextType contextType = ((VFXContext)model).contextType;
            int realIndex = index == -1 ? m_Children.Count : index;

            if (realIndex > 0 && GetChild(realIndex - 1).contextType > contextType)
                return false;
            if (realIndex < m_Children.Count - 1 && GetChild(realIndex + 1).contextType < contextType)
                return false;

            return true;
        }

        // Helper function
        // Connect context0 to context1 and recreate another system if necessary
        public static bool ConnectContexts(VFXContext context0, VFXContext context1, VFXGraph root)
        {
            if (context0 == context1)
                return false;

            VFXSystem system0 = context0.GetParent();
            int context0Index = system0.GetIndex(context0);

            if (system0 == context1.GetParent() && context0Index > context1.GetParent().GetIndex(context1))
                return false;

            if (!system0.AcceptChild(context1, context0Index + 1))
                return false;

            if (system0.GetNbChildren() > context0Index + 1)
            {
                VFXSystem newSystem = new VFXSystem();

                while (system0.GetNbChildren() > context0Index + 1)
                    system0.m_Children[context0Index + 1].Attach(newSystem, true);

                root.AddChild(newSystem);
            }

            VFXSystem system1 = context1.GetParent();
            int context1Index = system1.m_Children.IndexOf(context1);

            // Then we append context1 and all following contexts to system0
            while (system1.GetNbChildren() > context1Index)
                system1.m_Children[context1Index].Attach(system0, true);

            // Remove empty systems
            if (system0.GetNbChildren() == 0)
                system0.Detach();
            if (system1.GetNbChildren() == 0)
                system1.Detach();

            return true;
        }

        // Helper function
        // Disconnect a context (from its input) and create another system if necessary
        public static bool DisconnectContext(VFXContext context, VFXGraph root)
        {
            VFXSystem system = context.GetParent();
            if (system == null)
                return false;

            int index = system.GetIndex(context);
            if (index == 0)
                return false;

            VFXSystem newSystem = new VFXSystem();
            while (system.GetNbChildren() > index)
                system.GetChild(index).Attach(newSystem, true);

            root.AddChild(newSystem);

            return true;
        }
    }
}