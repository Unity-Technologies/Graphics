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

			VFXContextModel.Type contextType = (element as VFXContextModel).GetContextType();
			if (contextType == VFXContextModel.Type.kTypeNone)
				return false;

			// Check if context types are inserted in the right order
			int realIndex = index == -1 ? m_Children.Count : index;
			if (realIndex > 0 && GetChild(realIndex - 1).GetContextType() > contextType)
				return false;
			if (realIndex < m_Children.Count && GetChild(realIndex).GetContextType() < contextType)
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

		public override bool CanAttach(VFXElementModel element, int index)
		{
			return base.CanAttach(element, index) && m_Type != Type.kTypeNone;
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

		public override bool CanAttach(VFXElementModel element, int index)
		{
			return false; // Nothing can be attached to Blocks !
		}

		private VFXBlock m_BlockDesc;
		// TODO Store the uniform here ?
	}
}
