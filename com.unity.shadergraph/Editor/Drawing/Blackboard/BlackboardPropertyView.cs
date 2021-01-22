using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardPropertyView : GraphElement, IInspectable, ISGControlledElement<ShaderInputViewController>
    {
        ShaderInputViewModel m_ViewModel;

        ShaderInputViewModel ViewModel
        {
            get => m_ViewModel;
            set => m_ViewModel = value;
        }

        internal BlackboardPropertyView(ShaderInputViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        ShaderInputViewController m_Controller;

        // --- Begin ISGControlledElement implementation
        public void OnControllerChanged(ref SGControllerChangedEvent e)
        {

        }

        public void OnControllerEvent(SGControllerEvent e)
        {

        }

        public ShaderInputViewController controller
        {
            get => m_Controller;
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    Clear();
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }

        // --- ISGControlledElement implementation

        public string inspectorTitle { get; }

        public object GetObjectToInspect()
        {
            throw new NotImplementedException();
        }

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            throw new NotImplementedException();
        }
    }
}
