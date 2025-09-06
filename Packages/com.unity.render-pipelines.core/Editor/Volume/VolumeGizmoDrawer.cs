using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Collection of static methods to draw gizmos for volume colliders.
    /// </summary>
    public static class VolumeGizmoDrawer
    {
        /// <summary>
        /// Draws a box collider gizmo.
        /// </summary>
        /// <param name="transform">Transform of the box collider.</param>
        /// <param name="center">Center position of the box collider.</param>
        /// <param name="size">Size of the box collider.</param>
        public static void DrawBoxCollider(Transform transform, Vector3 center, Vector3 size)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            if (VolumesPreferences.drawWireFrame)
                Gizmos.DrawWireCube(center, size);
            if (VolumesPreferences.drawSolid)
                Gizmos.DrawCube(center, size);
        }

        /// <summary>
        /// Draws a sphere collider gizmo.
        /// </summary>
        /// <param name="transform">Transform of the sphere collider.</param>
        /// <param name="center">Center position of the sphere collider.</param>
        /// <param name="radius">Radius of the sphere collider.</param>
        public static void DrawSphereCollider(Transform transform, Vector3 center, float radius)
        {
            // For sphere the only scale that is used is the transform.x
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * transform.lossyScale.x);

            if (VolumesPreferences.drawWireFrame)
                Gizmos.DrawWireSphere(center, radius);
            if (VolumesPreferences.drawSolid)
                Gizmos.DrawSphere(center, radius);
        }

        /// <summary>
        /// Draws a mesh collider gizmo.
        /// </summary>
        /// <param name="transform">Transform of the mesh collider.</param>
        /// <param name="mesh">Mesh to draw.</param>
        public static void DrawMeshCollider(Transform transform, Mesh mesh)
        {
            // We'll just use scaling as an approximation for volume skin. It's far from being
            // correct (and is completely wrong in some cases). Ultimately we'd use a distance
            // field or at least a tessellate + push modifier on the collider's mesh to get a
            // better approximation, but the current Gizmo system is a bit limited and because
            // everything is dynamic in Unity and can be changed at anytime, it's hard to keep
            // track of changes in an elegant way (which we'd need to implement a nice cache
            // system for generated volume meshes).

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            if (VolumesPreferences.drawWireFrame)
                Gizmos.DrawWireMesh(mesh);
            if (VolumesPreferences.drawSolid)
                Gizmos.DrawMesh(mesh); // Mesh pivot should be centered or this won't work
        }

#if ENABLE_PHYSICS_MODULE
        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void OnDrawGizmos(Volume volume, GizmoType gizmoType)
        {
            if (!volume.enabled || volume.isGlobal || volume.colliders == null)
                return;

            var gizmoColor = VolumesPreferences.volumeGizmoColor;
            var gizmoColorWhenBlendRegionEnabled = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f * gizmoColor.a);

            foreach (var collider in volume.colliders)
            {
                if (!collider || !collider.enabled)
                    continue;

                // Reduce gizmo opacity when blend region is enabled because there are two of them
                bool blendDistanceEnabled = volume.blendDistance > 0f;
                Gizmos.color = blendDistanceEnabled && VolumesPreferences.drawSolid ? gizmoColorWhenBlendRegionEnabled : gizmoColor;

                switch (collider)
                {
                    case BoxCollider c:
                        DrawBoxCollider(c.transform, c.center, c.size);
                        if (blendDistanceEnabled)
                            DrawBlendDistanceBox(c, volume.blendDistance);
                        break;
                    case SphereCollider c:
                        DrawSphereCollider(c.transform, c.center, c.radius);
                        if (blendDistanceEnabled)
                            DrawBlendDistanceSphere(c, volume.blendDistance);
                        break;
                    case MeshCollider c:
                        DrawMeshCollider(c.transform, c.sharedMesh);
                        break;
                }
            }
        }

        static void DrawBlendDistanceBox(BoxCollider c, float blendDistance)
        {
            var twiceFadeRadius = blendDistance * 2;
            var transformScale = c.transform.localScale;
            // Divide by scale because blendDistance is absolute units and we don't want transform scale to affect it
            Vector3 size = c.size + new Vector3(
                twiceFadeRadius / transformScale.x,
                twiceFadeRadius / transformScale.y,
                twiceFadeRadius / transformScale.z
            );

            if (VolumesPreferences.drawWireFrame)
                Gizmos.DrawWireCube(c.center, size);
            if (VolumesPreferences.drawSolid)
                Gizmos.DrawCube(c.center, size);
        }

        static void DrawBlendDistanceSphere(SphereCollider c, float blendDistance)
        {
            // Divide by scale because blendDistance is absolute units and we don't want transform scale to affect it
            var blendSphereSize = c.radius + blendDistance / c.transform.lossyScale.x;
            if (VolumesPreferences.drawWireFrame)
                Gizmos.DrawWireSphere(c.center, blendSphereSize);
            if (VolumesPreferences.drawSolid)
                Gizmos.DrawSphere(c.center, blendSphereSize);
        }
#endif
    }
}
