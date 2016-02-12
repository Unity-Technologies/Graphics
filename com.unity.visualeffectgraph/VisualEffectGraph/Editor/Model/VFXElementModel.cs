using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental
{
	public abstract class VFXElementModel
	{
		public void Add(VFXElementModel child, int index = -1,bool notify = true)
		{
			if (!CanAttach(child,index))
				throw new ArgumentException();

			child.Detach(notify && child.m_Owner != this); // Dont notify if the owner is already this to avoid double invalidation
			
			m_Children.Insert(index, child);
			child.m_Owner = this;
			
			if (notify)
				Invalidate();
		}

		public void Remove(VFXElementModel child, bool notify = true)
		{
			if (child.m_Owner != this)
				return;

			m_Children.Remove(child);
			child.m_Owner = null;
			
			if (notify)
				Invalidate();
		}

		public void Attach(VFXElementModel owner, bool notify = true)
		{
			if (owner == null)
				throw new ArgumentNullException();

			owner.Add(this, -1, notify);
		}

		public void Detach(bool notify = true)
		{
			if (m_Owner == null)
				return;

			m_Owner.Remove(this, notify);
		}

		public abstract bool CanAttach(VFXElementModel element,int index);
		public abstract void Invalidate();

		protected VFXElementModel m_Owner;
		protected List<VFXElementModel> m_Children;
	}

	public abstract class VFXElementModelTyped<OwnerType, ChildrenType> : VFXElementModel
		where OwnerType : VFXElementModel
		where ChildrenType : VFXElementModel
	{
		public override bool CanAttach(VFXElementModel element,int index)
		{
			return index >= -1 && index <= m_Children.Count && element is ChildrenType;
		}

		public ChildrenType GetChild(int index)
		{
			return m_Children[index] as ChildrenType;
		}

		public OwnerType GetOwner()
		{
			return m_Owner as OwnerType;
		}
	}
}
