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
		public override bool CanAttach(VFXElementModel element, int index)
		{
			if (base.CanAttach(element, index))
				return false;

			// Check if context types are inserted in the right order
			int realIndex = index == -1 ? m_Children.Count : index;
			if (realIndex > 0 && GetChild(realIndex - 1).GetContextType() > (element as VFXContextModel).GetContextType())
				return false;
			if (realIndex < m_Children.Count && GetChild(realIndex).GetContextType() < (element as VFXContextModel).GetContextType())
				return false;

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
			kTypeInit,
			kTypeUpdate,
			kTypeRender,
		};

		public const uint s_NbTypes = (uint)Type.kTypeRender + 1;

		public VFXContextModel(Type type)
		{
			m_Type = type;
		}

		public override bool CanAttach(VFXElementModel element, int index)
		{
			return base.CanAttach(element, index);
			// TODO Check if the block is comptatible with the context
		}

		public override void Invalidate()
		{
			// TODO
			// Recompute parameters field
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

		VFXBlockModel(VFXBlock desc)
		{
			m_BlockDesc = desc;
		}

		public void SetDesc(VFXBlock blockDesc)
		{
			if (blockDesc == null)
				throw new ArgumentNullException();

			if (m_BlockDesc == null || !m_BlockDesc.m_Hash.Equals(blockDesc.m_Hash)) // block desc has changed
			{
				m_BlockDesc = blockDesc;
				Invalidate();
			}
		}

		public override bool CanAttach(VFXElementModel element, int index)
		{
			return false; // Nothing can be attached to Blocks !
		}

		private VFXBlock m_BlockDesc;
		// TODO Store the uniform here ?
	}
}
