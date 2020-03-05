using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    internal class DefaultVFXTargetData : TargetImplementationData
    {
        [SerializeField]
        bool m_Lit;

        [SerializeField]
        bool m_AlphaTest;

        public bool lit
        {
            get => m_Lit;
            set => m_Lit = value;
        }

        public bool alphaTest
        {
            get => m_AlphaTest;
            set => m_AlphaTest = value;
        }

        internal override void GetProperties(PropertySheet propertySheet, InspectorView inspectorView)
        {
            propertySheet.Add(new PropertyRow(new Label("Lit")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = lit;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(lit, evt.newValue))
                                return;
                            
                            m_Lit = evt.newValue;
                            inspectorView.OnChange();
                        });
                    });
                });

            propertySheet.Add(new PropertyRow(new Label("Alpha Test")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = alphaTest;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(alphaTest, evt.newValue))
                                return;
                            
                            m_AlphaTest = evt.newValue;
                            inspectorView.OnChange();
                        });
                    });
                });
        }
    }
}
