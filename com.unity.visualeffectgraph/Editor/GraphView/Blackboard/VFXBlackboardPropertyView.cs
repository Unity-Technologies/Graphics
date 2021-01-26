using System;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;

namespace  UnityEditor.VFX.UI
{
    class ValueFilterEnumPropertyRMProvider : SimplePropertyRMProvider<VFXValueFilter>
    {
        bool m_NoEnum;
        public ValueFilterEnumPropertyRMProvider(string name, System.Func<VFXValueFilter> getter, System.Action<VFXValueFilter> setter, bool noEnum) : base(name, getter, setter)
        {
            m_NoEnum = noEnum;
        }

        public override IEnumerable<int>  filteredOutEnumerators
        {
            get
            {
                return m_NoEnum ? new int[] {2 } : null;
            }
        }
    }

    class VFXBlackboardPropertyView : VisualElement, IControlledElement<VFXParameterController>
    {
        public VFXBlackboardPropertyView()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public VFXBlackboardRow owner
        {
            get; set;
        }

        Controller IControlledElement.controller
        {
            get { return owner.controller; }
        }
        public VFXParameterController controller
        {
            get { return owner.controller; }
        }

        PropertyRM m_Property;
        PropertyRM m_MinProperty;
        PropertyRM m_MaxProperty;
        PropertyRM m_EnumProperty;
        List<PropertyRM> m_SubProperties;
        StringPropertyRM m_TooltipProperty;

        IEnumerable<PropertyRM> allProperties
        {
            get
            {
                var result = Enumerable.Empty<PropertyRM>();

                if (m_ExposedProperty != null)
                    result = result.Concat(Enumerable.Repeat<PropertyRM>(m_ExposedProperty, 1));
                if (m_Property != null)
                    result = result.Concat(Enumerable.Repeat(m_Property, 1));
                if (m_SubProperties != null)
                    result = result.Concat(m_SubProperties);
                if (m_TooltipProperty != null)
                    result = result.Concat(Enumerable.Repeat<PropertyRM>(m_TooltipProperty, 1));
                if (m_ValueFilterProperty != null)
                    result = result.Concat(Enumerable.Repeat<PropertyRM>(m_ValueFilterProperty, 1));
                if (m_MinProperty != null)
                    result = result.Concat(Enumerable.Repeat(m_MinProperty, 1));
                if (m_MaxProperty != null)
                    result = result.Concat(Enumerable.Repeat(m_MaxProperty, 1));
                if (m_EnumProperty != null)
                    result = result.Concat(Enumerable.Repeat(m_EnumProperty, 1));

                return result;
            }
        }


        void GetPreferedWidths(ref float labelWidth)
        {
            foreach (var port in allProperties)
            {
                float portLabelWidth = port.GetPreferredLabelWidth() + 5;

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
            }
        }

        void ApplyWidths(float labelWidth)
        {
            foreach (var port in allProperties)
            {
                port.SetLabelWidth(labelWidth);
            }
        }

        void CreateSubProperties(ref int insertIndex, List<int> fieldPath)
        {
            var subControllers = controller.GetSubControllers(fieldPath);

            var subFieldPath = new List<int>();
            int cpt = 0;
            foreach (var subController in subControllers)
            {
                subController.RegisterHandler(this);
                PropertyRM prop = PropertyRM.Create(subController, 85);
                if (prop != null)
                {
                    m_SubProperties.Add(prop);
                    Insert(insertIndex++, prop);
                }
                if (subController.expanded)
                {
                    subFieldPath.Clear();
                    subFieldPath.AddRange(fieldPath);
                    subFieldPath.Add(cpt);
                    CreateSubProperties(ref insertIndex, subFieldPath);
                }
                ++cpt;
            }
        }

        EnumPropertyRM m_ValueFilterProperty;
        BoolPropertyRM m_ExposedProperty;

        IPropertyRMProvider m_RangeProvider;

        public new void Clear()
        {
            m_ExposedProperty = null;
            m_ValueFilterProperty = null;
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (m_Property != null && e.change == VFXSubParameterController.ExpandedChange)
            {
                int insertIndex = 2;
                RecreateSubproperties(ref insertIndex);
                foreach (var prop in allProperties)
                {
                    prop.Update();
                }
            }
        }

        public void SelfChange(int change)
        {
            if (change == VFXParameterController.ValueChanged)
            {
                foreach (var prop in allProperties)
                {
                    prop.Update();
                }
                return;
            }

            int insertIndex = 0;


            bool isOutputParameter = controller.isOutput;

            if (!isOutputParameter)
            {
                if (m_ExposedProperty == null)
                {
                    m_ExposedProperty = new BoolPropertyRM(new SimplePropertyRMProvider<bool>("Exposed", () => controller.exposed, t => controller.exposed = t), 55);
                    Insert(insertIndex++, m_ExposedProperty);
                }
                else
                {
                    insertIndex++;
                }

                if (m_Property == null || !m_Property.IsCompatible(controller))
                {
                    if (m_Property != null)
                    {
                        m_Property.RemoveFromHierarchy();
                    }
                    m_Property = PropertyRM.Create(controller, 55);
                    if (m_Property != null)
                    {
                        Insert(insertIndex++, m_Property);
                        if (!m_Property.showsEverything)
                        {
                            RecreateSubproperties(ref insertIndex);
                        }
                        List<int> fieldpath = new List<int>();

                        if (m_TooltipProperty == null)
                        {
                            m_TooltipProperty = new StringPropertyRM(new SimplePropertyRMProvider<string>("Tooltip", () => controller.model.tooltip, t => controller.model.tooltip = t), 55);
                            TextField field = m_TooltipProperty.Query<TextField>();
                            field.multiline = true;
                        }
                        Insert(insertIndex++, m_TooltipProperty);
                    }
                    else
                    {
                        m_TooltipProperty = null;
                    }
                }
                else
                {
                    insertIndex += 1 + (m_SubProperties != null ? m_SubProperties.Count : 0) + 1; //main property + subproperties + tooltip
                }
                bool mustRelayout = false;

                if (controller.canHaveValueFilter)
                {
                    if (m_MinProperty == null || !m_MinProperty.IsCompatible(controller.minController))
                    {
                        if (m_MinProperty != null)
                            m_MinProperty.RemoveFromHierarchy();
                        m_MinProperty = PropertyRM.Create(controller.minController, 55);
                    }
                    if (m_MaxProperty == null || !m_MaxProperty.IsCompatible(controller.minController))
                    {
                        if (m_MaxProperty != null)
                            m_MaxProperty.RemoveFromHierarchy();
                        m_MaxProperty = PropertyRM.Create(controller.maxController, 55);
                    }

                    if (m_ValueFilterProperty == null)
                    {
                        m_ValueFilterProperty = new EnumPropertyRM(new ValueFilterEnumPropertyRMProvider("Mode", () => controller.valueFilter, t => controller.valueFilter = t, controller.portType != typeof(uint)), 55);
                    }
                    Insert(insertIndex++, m_ValueFilterProperty);

                    if (controller.model.valueFilter == VFXValueFilter.Range)
                    {
                        if (m_MinProperty.parent == null)
                        {
                            Insert(insertIndex++, m_MinProperty);
                            Insert(insertIndex++, m_MaxProperty);
                            mustRelayout = true;
                        }
                    }
                    else if (m_MinProperty.parent != null)
                    {
                        m_MinProperty.RemoveFromHierarchy();
                        m_MaxProperty.RemoveFromHierarchy();
                    }
                    if (controller.valueFilter == VFXValueFilter.Enum)
                    {
                        if (m_EnumProperty == null || !m_EnumProperty.IsCompatible(controller.enumController))
                        {
                            if (m_EnumProperty != null)
                                m_EnumProperty.RemoveFromHierarchy();
                            m_EnumProperty = new VFXListParameterEnumValuePropertyRM(controller.enumController, 55);
                        }
                        if (m_EnumProperty.parent == null)
                        {
                            Insert(insertIndex++, m_EnumProperty);
                            mustRelayout = true;
                        }
                    }
                    else if (m_EnumProperty != null && m_EnumProperty.parent != null)
                    {
                        m_EnumProperty.RemoveFromHierarchy();
                    }
                }
                else
                {
                    if (m_MinProperty != null)
                    {
                        m_MinProperty.RemoveFromHierarchy();
                        m_MinProperty = null;
                    }
                    if (m_MaxProperty != null)
                    {
                        m_MaxProperty.RemoveFromHierarchy();
                        m_MaxProperty = null;
                    }
                    if (m_ValueFilterProperty != null)
                    {
                        m_ValueFilterProperty.RemoveFromHierarchy();
                        m_ValueFilterProperty = null;
                    }
                }

                if (mustRelayout)
                    Relayout();
            }
            else
            {
                m_Property = null;
                m_ExposedProperty = null;
                m_SubProperties = null;
                m_MinProperty = null;
                m_MaxProperty = null;
                m_ValueFilterProperty = null;
                if (m_TooltipProperty == null)
                {
                    m_TooltipProperty = new StringPropertyRM(new SimplePropertyRMProvider<string>("Tooltip", () => controller.model.tooltip, t => controller.model.tooltip = t), 55);
                }
                Insert(insertIndex++, m_TooltipProperty);
            }


            foreach (var prop in allProperties)
            {
                prop.Update();
            }
        }

        private void RecreateSubproperties(ref int insertIndex)
        {
            if (m_SubProperties != null)
            {
                foreach (var subProperty in m_SubProperties)
                {
                    (subProperty.provider as Controller).UnregisterHandler(this);
                    subProperty.RemoveFromHierarchy();
                }
            }
            else
            {
                m_SubProperties = new List<PropertyRM>();
            }
            m_SubProperties.Clear();
            List<int> fieldpath = new List<int>();
            if (!m_Property.showsEverything)
            {
                CreateSubProperties(ref insertIndex, fieldpath);
            }
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (panel != null)
            {
                Relayout();
            }
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void Relayout()
        {
            float labelWidth = 70;
            GetPreferedWidths(ref labelWidth);
            ApplyWidths(labelWidth);
        }
    }
}
