using System;
using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    abstract class VFXLinkablePresenter : NodePresenter
    {
        public abstract IVFXSlotContainer slotContainer { get; }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, slotContainer as UnityEngine.Object };
        }
    }
}
