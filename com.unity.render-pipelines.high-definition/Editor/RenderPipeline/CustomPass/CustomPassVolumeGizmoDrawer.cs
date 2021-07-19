using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering
{
    class CustomPassGizmoDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void OnDrawGizmos(CustomPassVolume scr, GizmoType gizmoType)
        {
            if (!scr.enabled)
                return;

            if (scr.isGlobal || scr.m_Colliders.Count == 0)
                return;

            // Store the computation of the lossyScale
            var lossyScale = scr.transform.lossyScale;
            Gizmos.matrix = Matrix4x4.TRS(scr.transform.position, scr.transform.rotation, lossyScale);
            Gizmos.color = VolumesPreferences.volumeGizmoColor;

            // Draw a separate gizmo for each collider
            foreach (var collider in scr.m_Colliders)
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
                        if (scr.fadeRadius > 0)
                        {
                            // invert te scale for the fade radius because it's in fixed units
                            Vector3 s = new Vector3(
                                (scr.fadeRadius * 2) / scr.transform.localScale.x,
                                (scr.fadeRadius * 2) / scr.transform.localScale.y,
                                (scr.fadeRadius * 2) / scr.transform.localScale.z
                            );
                            Gizmos.DrawWireCube(c.center, c.size + s);
                        }
                        break;
                    case SphereCollider c:
                        // For sphere the only scale that is used is the transform.scale.x
                        Matrix4x4 oldMatrix = Gizmos.matrix;
                        Gizmos.matrix = Matrix4x4.TRS(scr.transform.position, scr.transform.rotation, Vector3.one * lossyScale.x);
                        if (VolumesPreferences.drawWireFrame)
                            Gizmos.DrawWireSphere(c.center, c.radius);
                        if (VolumesPreferences.drawSolid)
                            Gizmos.DrawSphere(c.center, c.radius);
                        if (scr.fadeRadius > 0)
                            Gizmos.DrawWireSphere(c.center, c.radius + scr.fadeRadius / lossyScale.x);
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

                        // We don't display the Gizmo for fade distance mesh because the distances would be wrong
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
