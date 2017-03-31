using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    class VFXParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXParameter()
        {
        }

        public void Init(Type _type)
        {
            if (_type != null && outputSlots.Count == 0)
            {
                AddSlot(VFXSlot.Create(new VFXProperty(_type, "o"), VFXSlot.Direction.kOutput));
            }
            else
            {
                throw new InvalidOperationException("Cannot init VFXParameter");
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Invalidate(InvalidationCause.kStructureChanged);
        }
    }
}