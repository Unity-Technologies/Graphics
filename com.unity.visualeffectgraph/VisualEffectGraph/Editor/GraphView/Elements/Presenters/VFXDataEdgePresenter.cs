using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    class VFXEdgeController : Controller
    {
    }

    class VFXEdgeController<T> : VFXEdgeController where T : IVFXAnchorPresenter
    {
        T m_Input;
        T m_Output;
        public virtual void Init(T input, T output)
        {
            m_Input = input;
            m_Output = output;

            m_Input.Connect(this);
            m_Output.Connect(this);
        }

        public T input { get { return m_Input; } }
        public T output { get { return m_Output; } }

        public override void OnRemoveFromGraph()
        {
            base.OnRemoveFromGraph();

            if (m_Input != null)
                m_Input.Disconnect(this);
            if (m_Output != null)
                m_Output.Disconnect(this);
        }
    }

    internal class VFXDataEdgePresenter : VFXEdgeController<VFXDataAnchorPresenter>
    {
        public VFXDataEdgePresenter()
        {
        }
    }
}
