using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    interface IVFXPresenter
    {
        VFXModel model { get; }
        void Init(VFXModel model, VFXViewPresenter viewPresenter);
    }
}

