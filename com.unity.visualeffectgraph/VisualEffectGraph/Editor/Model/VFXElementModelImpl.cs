using UnityEngine;
using System;
using System.Collections;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    public class VFXAssetModel : VFXElementModelTyped<VFXElementModel, VFXSystemModel>
    {
        public override void Invalidate()
        {
            // Nothing
        }

        public void Dump()
        {
            // TODO log debug info
        }
    }

    public class VFXSystemModel : VFXElementModelTyped<VFXAssetModel, VFXContextModel>
    {
        public override bool CanAddChild(VFXElementModel element, int index)
        {
            if (!base.CanAddChild(element, index))
                return false;

            VFXContextModel.Type contextType = (element as VFXContextModel).GetContextType();
            if (contextType == VFXContextModel.Type.kTypeNone)
                return false;

            // Check if context types are inserted in the right order
            int realIndex = index == -1 ? m_Children.Count : index;
            if (realIndex > 0 && GetChild(realIndex - 1).GetContextType() > contextType)
                return false;
            //if (realIndex < m_Children.Count && GetChild(realIndex).GetContextType() < contextType)
            //	return false;

            return true;
        }

        public static bool ConnectContext(VFXContextModel context0, VFXContextModel context1)
        {
            if (context0 == context1)
                return false;

            VFXSystemModel system0 = context0.GetOwner();
            int context0Index = system0.m_Children.IndexOf(context0);

            if (!system0.CanAddChild(context1, context0Index + 1))
                return false;

            // If context0 is not the last one in system0, we need to reattach following contexts to a new one
            if (system0.GetNbChildren() > context0Index + 1)
            {
                VFXSystemModel newSystem = new VFXSystemModel();
                while (system0.GetNbChildren() > context0Index + 1)
                    system0.m_Children[context0Index + 1].Attach(newSystem);
                VFXEditor.AssetModel.AddChild(newSystem);
            }

            VFXSystemModel system1 = context1.GetOwner();
            int context1Index = system1.m_Children.IndexOf(context1);

            // Then we append context1 and all following contexts to system0
            while (system1.GetNbChildren() > context1Index)
                system1.m_Children[context1Index].Attach(system0);

            return true;
        }

        public override void Invalidate()
        {
            if (m_Children.Count == 0 && m_Owner != null) // If the system has no more attached contexts, remove it
            {
                Detach();
                return;
            }

            // TODO
            // gather attributes and check if attributes layout has changed

            m_Dirty = true;
        }

        public void RecompileIfNeeded()
        {
            if (m_Dirty)
            {
                // TODO Recompile
                m_Dirty = false;
            }
        }

        private bool m_Dirty = true;
    }


    public class VFXContextModel : VFXElementModelTyped<VFXSystemModel, VFXBlockModel>
    {
        public enum Type
        {
            kTypeNone,
            kTypeInit,
            kTypeUpdate,
            kTypeOutput,
        };

        public const uint s_NbTypes = (uint)Type.kTypeOutput + 1;

        public VFXContextModel(Type type)
        {
            m_Type = type;
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return base.CanAddChild(element, index) && m_Type != Type.kTypeNone;
            // TODO Check if the block is compatible with the context
        }

        public override void Invalidate()
        {
            // TODO
            // Recompute parameters field

            if (m_Owner != null && m_Type != Type.kTypeNone)
                m_Owner.Invalidate();
        }

        public Type GetContextType()
        {
            return m_Type;
        }

        private Type m_Type;
    }

    public class VFXBlockModel : VFXElementModelTyped<VFXContextModel, VFXElementModel>
    {
        public override void Invalidate()
        {
            if (m_Owner != null)
                m_Owner.Invalidate();
        }

        public VFXBlockModel(VFXBlock desc)
        {
            m_BlockDesc = desc;
            int nbParams = desc.m_Params.Length;
            m_ParamValues = new VFXParamValue[nbParams];
            for (int i = 0; i < nbParams; ++i)
                m_ParamValues[i] = VFXParamValue.Create(desc.m_Params[i].m_Type); // Create default bindings
        }

        public VFXBlock Desc
        {
            get { return m_BlockDesc; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                if (m_BlockDesc == null || !m_BlockDesc.m_Hash.Equals(value.m_Hash)) // block desc has changed
                {
                    m_BlockDesc = value;
                    Invalidate();
                }
            }
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return false; // Nothing can be attached to Blocks !
        }

        private VFXBlock m_BlockDesc;
        private VFXParamValue[] m_ParamValues;
    }
}
