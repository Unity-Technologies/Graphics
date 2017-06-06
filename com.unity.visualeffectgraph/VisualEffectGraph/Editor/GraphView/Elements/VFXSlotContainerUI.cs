using System.Collections.Generic;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXSlotContainerUI : VFXNodeUI
    {
        public VFXSlotContainerUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Position;

            leftContainer.alignContent = Align.Stretch;

            AddToClassList("VFXSlotContainerUI");
        }

        public override NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter presenter)
        {
            VFXContextDataAnchorPresenter anchorPresenter = presenter as VFXContextDataAnchorPresenter;

            VFXEditableDataAnchor anchor = VFXBlockDataAnchor.Create<VFXDataEdgePresenter>(anchorPresenter);

            anchorPresenter.sourceNode.viewPresenter.onRecompileEvent += anchor.OnRecompile;

            return anchor;
        }

        protected override void OnAnchorRemoved(NodeAnchor anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                GetPresenter<VFXParameterPresenter>().viewPresenter.onRecompileEvent += (anchor as VFXEditableDataAnchor).OnRecompile;
            }
        }

        // On purpose -- until we support Drag&Drop I suppose
        public override void SetPosition(Rect newPos)
        {
        }


        public VisualContainer m_SettingsContainer;

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXSlotContainerPresenter>();

            if (presenter == null)
                return;

            SetPosition(presenter.position);

            if( m_SettingsContainer == null && presenter.settings != null)
            {
                object settings = presenter.settings;

                m_SettingsContainer = new VisualContainer{name="settings"};

                leftContainer.InsertChild(1,m_SettingsContainer); //between title and input

                foreach(var setting in presenter.settings)
                {
                    AddSetting(setting);
                }

            }
            if( m_SettingsContainer != null)
            {
                for(int i = 0; i < m_SettingsContainer.childrenCount; ++i)
                {
                    PropertyRM prop = m_SettingsContainer.GetChildAt(i) as PropertyRM;
                    if (prop != null)
                        prop.Update();
                }
            }
        }

        protected void AddSetting(VFXSettingPresenter setting)
        {
            m_SettingsContainer.AddChild(PropertyRM.Create(setting, 100));
        }

        public VFXContextUI context
        {
            get {return this.GetFirstAncestorOfType<VFXContextUI>(); }
        }
    }
}
