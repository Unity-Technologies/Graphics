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
    class VFXParameterUI : VFXStandaloneSlotContainerUI
    {
        PropertyRM m_Property;
        List<PropertyRM> m_SubProperties;

        public VFXParameterUI()
        {
        }

        public new VFXParameterController controller
        {
            get { return base.controller as VFXParameterController; }
        }

        public override void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
            base.GetPreferedWidths(ref labelWidth, ref controlWidth);

            if (labelWidth < 70)
                labelWidth = 70;

            var properties = inputContainer.Query().OfType<PropertyRM>().ToList();

            foreach (var port in properties)
            {
                float portLabelWidth = port.GetPreferredLabelWidth();
                float portControlWidth = port.GetPreferredControlWidth();

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
                if (controlWidth < portControlWidth)
                {
                    controlWidth = portControlWidth;
                }
            }
        }

        public override void ApplyWidths(float labelWidth, float controlWidth)
        {
            base.ApplyWidths(labelWidth, controlWidth);

            var properties = inputContainer.Query().OfType<PropertyRM>().ToList();
            foreach (var port in properties)
            {
                port.SetLabelWidth(labelWidth);
            }
        }

        protected override bool syncInput
        {
            get { return false; }
        }

        void CreateSubProperties(List<int> fieldPath)
        {
            var subControllers = controller.GetSubControllers(fieldPath);

            var subFieldPath = new List<int>();
            int cpt = 0;
            foreach (var subController in subControllers)
            {
                PropertyRM prop = PropertyRM.Create(subController, 55);
                if (prop != null)
                {
                    m_SubProperties.Add(prop);
                    inputContainer.Add(prop);
                }
                if (prop == null || !prop.showsEverything)
                {
                    subFieldPath.Clear();
                    subFieldPath.AddRange(fieldPath);
                    subFieldPath.Add(cpt);
                    CreateSubProperties(subFieldPath);
                }
                ++cpt;
            }
        }

        protected override void SelfChange()
        {
            base.SelfChange();
            if (controller == null)
                return;

            if (m_Property == null)
            {
                m_Property = PropertyRM.Create(controller, 55);
                if (m_Property != null)
                {
                    inputContainer.Add(m_Property);
                    m_SubProperties = new List<PropertyRM>();
                    List<int> fieldpath = new List<int>();
                    if (!m_Property.showsEverything)
                    {
                        CreateSubProperties(fieldpath);
                    }
                }
                RefreshPorts();
            }
            if (m_Property != null)
                m_Property.Update();
            if (m_SubProperties != null)
            {
                foreach (var subProp in m_SubProperties)
                {
                    subProp.Update();
                }
            }
        }
    }
}
