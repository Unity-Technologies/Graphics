using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    partial class ProbeGIBaking
    {
        static List<MeshRenderer> addedOccluders;

        private static bool IsValidForBaking(GameObject gameObject)
        {
            // TODO: Do a better filtering here.
            return gameObject.activeInHierarchy;
        }

        private static void AddOccluders(Vector3 pos, Vector3 size)
        {
            addedOccluders = new List<MeshRenderer>();
            Bounds volumeBounds = new Bounds(pos, size);
            for (int sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; ++sceneIndex)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                GameObject[] gameObjects = scene.GetRootGameObjects();
                foreach (GameObject gameObject in gameObjects)
                {
                    MeshRenderer[] renderComponents = gameObject.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mr in renderComponents)
                    {
                        if (IsValidForBaking(mr.gameObject))
                        {
                            if (mr.bounds.Intersects(volumeBounds))
                            {
                                if (!mr.gameObject.GetComponent<MeshCollider>())
                                {
                                    mr.gameObject.AddComponent<MeshCollider>();
                                    addedOccluders.Add(mr);
                                }
                            }
                        }
                    }
                }
            }

            var autoSimState = Physics.autoSimulation;
            Physics.autoSimulation = false;
            Physics.Simulate(0.1f);
            Physics.autoSimulation = autoSimState;
        }

        private static void CleanupOccluders()
        {
            foreach (MeshRenderer meshRenderer in addedOccluders)
            {
                MeshCollider collider = meshRenderer.gameObject.GetComponent<MeshCollider>();
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static bool HasMeshColliderHits(RaycastHit[] outBoundHits, RaycastHit[] inBoundHits)
        {
            foreach (var hit in outBoundHits)
            {
                if (hit.collider is MeshCollider && IsValidForBaking(hit.collider.gameObject))
                {
                    return true;
                }
            }

            foreach (var hit in inBoundHits)
            {
                if (hit.collider is MeshCollider && IsValidForBaking(hit.collider.gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 PushPositionOutOfGeometry(Vector3 worldPosition, float distanceSearch)
        {
            for (int x = -1; x <= 1; ++x)
            {
                for (int y = -1; y <= 1; ++y)
                {
                    for (int z = -1; z <= 1; ++z)
                    {
                        Vector3 searchDir = new Vector3(x, y, z);
                        Vector3 normDir = searchDir.normalized;
                    }
                }
            }

            return worldPosition;
        }
    }
}
