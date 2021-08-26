using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class VolumeGizmoDrawer
    {
        #region GizmoCallbacks
        static readonly Dictionary<Type, IVolumeAdditionalGizmo> s_AdditionalGizmoCallbacks = new();

        [InitializeOnLoadMethod]
        static void InitVolumeGizmoCallbacks()
        {
            foreach (var additionalGizmoCallback in TypeCache.GetTypesDerivedFrom<IVolumeAdditionalGizmo>())
            {
                if (additionalGizmoCallback.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) == null)
                    continue;
                var instance = Activator.CreateInstance(additionalGizmoCallback) as IVolumeAdditionalGizmo;
                s_AdditionalGizmoCallbacks.Add(instance.type, instance);
            }
        }

        #endregion

        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void OnDrawGizmos(IVolume scr, GizmoType gizmoType)
        {
            if (scr is not MonoBehaviour monoBehaviour)
                return;

            if (!monoBehaviour.enabled)
                return;

            if (scr.isGlobal || scr.colliders == null)
                return;

            // Store the computation of the lossyScale
            var lossyScale = monoBehaviour.transform.lossyScale;
            Gizmos.matrix = Matrix4x4.TRS(monoBehaviour.transform.position, monoBehaviour.transform.rotation, lossyScale);
            Gizmos.color = VolumesPreferences.volumeGizmoColor;

            s_AdditionalGizmoCallbacks.TryGetValue(scr.GetType(), out var callback);

            // Draw a separate gizmo for each collider
            foreach (var collider in scr.colliders)
            {
                if (!collider || !collider.enabled)
                    continue;

                // We'll just use scaling as an approximation for volume skin. It's far from being
                // correct (and is completely wrong in some cases). Ultimately we'd use a distance
                // field or at least a tesselate + push modifier on the collider's mesh to get a
                // better approximation, but the current Gizmo system is a bit limited and because
                // everything is dynamic in Unity and can be changed at anytime, it's hard to keep
                // track of changes in an elegant way (which we'd need to implement a nice cache
                // system for generated volume meshes).
                switch (collider)
                {
                    case BoxCollider c:
                        if (VolumesPreferences.drawWireFrame)
                            Gizmos.DrawWireCube(c.center, c.size);
                        if (VolumesPreferences.drawSolid)
                            Gizmos.DrawCube(c.center, c.size);

                        callback?.OnBoxColliderDraw(scr, c);
                        break;
                    case SphereCollider c:
                        Matrix4x4 oldMatrix = Gizmos.matrix;
                        // For sphere the only scale that is used is the transform.x
                        Gizmos.matrix = Matrix4x4.TRS(monoBehaviour.transform.position, monoBehaviour.transform.rotation, Vector3.one * lossyScale.x);

                        if (VolumesPreferences.drawWireFrame)
                            Gizmos.DrawWireSphere(c.center, c.radius);
                        if (VolumesPreferences.drawSolid)
                            Gizmos.DrawSphere(c.center, c.radius);

                        callback?.OnSphereColliderDraw(scr, c);

                        Gizmos.matrix = oldMatrix;
                        break;
                    case MeshCollider c:
                        // Only convex mesh m_Colliders are allowed
                        if (!c.convex)
                            c.convex = true;

                        if (VolumesPreferences.drawWireFrame)
                            Gizmos.DrawWireMesh(c.sharedMesh);
                        if (VolumesPreferences.drawSolid)
                            // Mesh pivot should be centered or this won't work
                            Gizmos.DrawMesh(c.sharedMesh);

                        callback?.OnMeshColliderDraw(scr, c);
                        break;
                    default:
                        // Nothing for capsule (DrawCapsule isn't exposed in Gizmo), terrain, wheel and
                        // other m_Colliders...
                        break;
                }
            }
        }
    }
}
