using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering
{
    class CustomVolumePassGizmoDrawer
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void OnDrawGizmos(CustomPassVolume volume, GizmoType gizmoType)
        {
            if (!volume.enabled || volume.isGlobal || volume.colliders == null)
                return;

            Gizmos.color = VolumesPreferences.volumeGizmoColor;

            foreach (var collider in volume.colliders)
            {
                if (!collider || !collider.enabled)
                    continue;

                bool fadeRadiusEnabled = volume.fadeRadius > 0f;
                switch (collider)
                {
                    case BoxCollider c:
                        VolumeGizmoDrawer.DrawBoxCollider(c.transform, c.center, c.size);
                        if (fadeRadiusEnabled)
                            DrawFadeRadiusBox(volume, c);
                        break;
                    case SphereCollider c:
                        VolumeGizmoDrawer.DrawSphereCollider(c.transform, c.center, c.radius);
                        if (fadeRadiusEnabled)
                            DrawFadeRadiusSphere(volume, c);
                        break;
                    case MeshCollider c:
                        VolumeGizmoDrawer.DrawMeshCollider(c.transform, c.sharedMesh);
                        break;
                }
            }
        }

        public static void DrawFadeRadiusBox(CustomPassVolume volume, BoxCollider c)
        {
            var twiceFadeRadius = volume.fadeRadius * 2;
            // invert te scale for the fade radius because it's in fixed units
            Vector3 s = new Vector3(
                twiceFadeRadius / volume.transform.localScale.x,
                twiceFadeRadius / volume.transform.localScale.y,
                twiceFadeRadius / volume.transform.localScale.z
            );
            Gizmos.DrawWireCube(c.center, c.size + s);
        }

        public static void DrawFadeRadiusSphere(CustomPassVolume volume, SphereCollider c)
        {
            Gizmos.DrawWireSphere(c.center, c.radius + volume.fadeRadius / volume.transform.lossyScale.x);
        }
    }
}
