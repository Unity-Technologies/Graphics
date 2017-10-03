using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    interface IEdgeDrawerOwner
    {
        void DirtyDrawer();
    }

    class Collapser : Manipulator
    {
        public Collapser()
        {
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseUp);
        }

        void OnMouseUp(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                VFXSlotContainerUI slotContainer = (VFXSlotContainerUI)target;

                slotContainer.collapse = !slotContainer.collapse;
            }
        }
    }
    class VFXStandaloneSlotContainerUI : VFXSlotContainerUI, IEdgeDrawerOwner
    {
        public VFXStandaloneSlotContainerUI()
        {
            m_EdgeDrawer = new VFXEdgeDrawer();
            m_EdgeDrawer.style.positionType = PositionType.Absolute;
            m_EdgeDrawer.style.positionLeft = 0;
            m_EdgeDrawer.style.positionRight = 0;
            m_EdgeDrawer.style.positionBottom = 0;
            m_EdgeDrawer.style.positionTop = 0;

            Add(m_EdgeDrawer);
        }

        VFXEdgeDrawer m_EdgeDrawer;

        public void DirtyDrawer()
        {
            m_EdgeDrawer.Dirty(ChangeType.Repaint);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            m_EdgeDrawer.presenter = GetPresenter<VFXSlotContainerPresenter>();
        }
    }


    class VFXOperatorUI : VFXStandaloneSlotContainerUI, IKeyFocusBlocker
    {
        public VFXOperatorUI()
        {
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXOperatorPresenter>();
            if (presenter == null || presenter.Operator == null)
                return;
        }
    }
}
