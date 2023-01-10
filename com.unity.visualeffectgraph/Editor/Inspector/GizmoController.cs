using System;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    [Flags]
    enum GizmoError
    {
        None = 0,
        HasLinkIndeterminate = 1 << 0,
        NeedComponent = 1 << 1,
        NeedExplicitSpace = 1 << 2,
        NotAvailable = 1 << 3
    }

    interface IGizmoable
    {
        string name { get; }
    }

    interface IGizmoError
    {
        GizmoError GetGizmoError(VisualEffect component);
    }

    interface IGizmoController : IGizmoError
    {
        void DrawGizmos(VisualEffect component);
        Bounds GetGizmoBounds(VisualEffect component);

        ReadOnlyCollection<IGizmoable> gizmoables { get; }
        IGizmoable currentGizmoable { get; set; }
    }
}
