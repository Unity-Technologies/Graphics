using System;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    [Flags]
    public enum GizmoError
    {
        None = 0,
        Indeterminate = 1 << 0,
        HasLink = 1 << 2,
        NeedComponent = 1 << 3,
        NeedExplicitSpace = 1 << 4,
        NoGizmo = 1 << 5
    }

    interface IGizmoable
    {
        string name { get; }
    }

    interface IGizmoController
    {
        void DrawGizmos(VisualEffect component);
        Bounds GetGizmoBounds(VisualEffect component);

        bool gizmoNeedsComponent { get; } //Remove this (TODOPAUL)

        GizmoError GetGizmoError(VisualEffect component);

        ReadOnlyCollection<IGizmoable> gizmoables { get; }

        IGizmoable currentGizmoable { get; set; }
    }
}
