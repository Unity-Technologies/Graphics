using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXParameterPresenter : VFXNodePresenter, IVFXPresenter
    {
        [SerializeField]
        private string m_exposedName;
        [SerializeField]
        private bool m_exposed;

        public string exposedName { get { return m_exposedName; } set { m_exposedName = value; } }
        public bool exposed { get { return m_exposed; } set { m_exposed = value;  } }

        public VFXParameter parameter { get { return node as VFXParameter; } }

        protected override NodeAnchorPresenter CreateAnchorPresenter(VFXSlot slot, Direction direction)
        {
            var anchor = base.CreateAnchorPresenter(slot, direction);
            anchor.anchorType = slot.property.type;
            anchor.name = slot.property.type.Name;
            return anchor;
        }

        protected override void Reset()
        {
            if (parameter != null)
            {
                title = node.outputSlots[0].property.type.Name + " " + node.m_OnEnabledCount;
                exposed = parameter.exposed;
                exposedName = parameter.exposedName;
            }
            base.Reset();
        }
    }
}
