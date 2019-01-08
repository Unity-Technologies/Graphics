using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    interface IControlledElement
    {
        Controller controller
        {
            get;
        }
        void OnControllerChanged(ref ControllerChangedEvent e);
    }

    interface IControllerListener
    {
        void OnControllerEvent(ControllerEvent e);
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
