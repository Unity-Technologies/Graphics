using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    interface IVFXMovable
    {
        void OnMoved();
    }
    interface IVFXResizable
    {
        void OnStartResize();
        void OnResized();
    }
}
