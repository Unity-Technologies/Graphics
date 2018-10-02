using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
    interface IControlledElement
    {
        Controller controller
        {
            get;
        }
        void OnControllerChanged(ref ControllerChangedEvent e);
        void OnControllerEvent(VFXControllerEvent e);
    }

    interface IControlledElement<T> : IControlledElement where T : Controller
    {
        new T controller
        {
            get;
        }
    }
    interface ISettableControlledElement<T> where T : Controller
    {
        T controller
        {
            get;
            set;
        }
    }
}
