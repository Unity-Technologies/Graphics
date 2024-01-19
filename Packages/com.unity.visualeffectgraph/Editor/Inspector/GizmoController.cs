using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.VFX;

interface IGizmoable
{
    string name { get; }
}
interface IGizmoController
{
    void CollectGizmos();
    void DrawGizmos(VisualEffect component);
    Bounds GetGizmoBounds(VisualEffect component);

    ReadOnlyCollection<IGizmoable> gizmoables { get; }
    IGizmoable currentGizmoable { get; set; }
}
