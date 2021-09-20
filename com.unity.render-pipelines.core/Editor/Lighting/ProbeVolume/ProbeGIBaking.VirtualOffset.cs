using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    partial class ProbeGIBaking
    {
        static List<MeshRenderer> addedOccluders;

        private static void AddOccluders()
        {
            addedOccluders = new List<MeshRenderer>();
            for (int sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; ++sceneIndex)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;

                GameObject[] gameObjects = scene.GetRootGameObjects();
                foreach (GameObject gameObject in gameObjects)
                {
                    MeshRenderer[] renderComponents = gameObject.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mr in renderComponents)
                    {
                        if (!mr.gameObject.GetComponent<MeshCollider>() && (GameObjectUtility.GetStaticEditorFlags(mr.gameObject).HasFlag(StaticEditorFlags.ContributeGI)))
                        {
                            var meshCollider = mr.gameObject.AddComponent<MeshCollider>();
                            meshCollider.hideFlags |= (HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild);
                            addedOccluders.Add(mr);
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

        private static bool HasMeshColliderHits(RaycastHit[] outBoundHits, RaycastHit[] inBoundHits, Vector3 outRay, Vector3 inRay, float rayEnd, out float distance)
        {
            distance = float.MaxValue;
            bool hasHit = false;

            foreach (var hit in outBoundHits)
            {
                if (hit.collider is MeshCollider && Vector3.Dot(outRay, hit.normal) > 0)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        hasHit = true;
                    }
                }
            }

            foreach (var hit in inBoundHits)
            {
                if (hit.collider is MeshCollider && Vector3.Dot(inRay, hit.normal) > 0)
                {
                    if ((rayEnd - hit.distance) < distance)
                    {
                        distance = hit.distance;
                        hasHit = true;
                    }
                }
            }

            return hasHit;
        }

        private static Vector3 PushPositionOutOfGeometry(Vector3 worldPosition, float distanceSearch, float biasOutGeo)
        {
            bool queriesHitBackBefore = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            float minDist = float.MaxValue;
            bool hitFound = false;
            Vector3 outDirection = Vector3.zero;
            for (int x = -1; x <= 1; ++x)
            {
                for (int y = -1; y <= 1; ++y)
                {
                    for (int z = -1; z <= 1; ++z)
                    {
                        Vector3 searchDir = new Vector3(x, y, z);
                        Vector3 normDir = searchDir.normalized;
                        Vector3 ray = normDir * distanceSearch;
                        var collisionLayerMask = ~0;
                        RaycastHit[] outBoundHits = Physics.RaycastAll(worldPosition, normDir, distanceSearch, collisionLayerMask);
                        RaycastHit[] inBoundHits = Physics.RaycastAll(worldPosition + ray, -1.0f * normDir, distanceSearch, collisionLayerMask);

                        float distanceForDir = 0;
                        bool hasMeshColliderHits = HasMeshColliderHits(outBoundHits, inBoundHits, normDir, -normDir, distanceSearch, out distanceForDir);
                        if (hasMeshColliderHits)
                        {
                            hitFound = true;
                            if (distanceForDir < minDist)
                            {
                                outDirection = searchDir;
                                minDist = distanceForDir;
                            }
                        }
                    }
                }
            }

            if (hitFound)
            {
                worldPosition = worldPosition + outDirection.normalized * (minDist * 1.05f + biasOutGeo);
            }

            Physics.queriesHitBackfaces = queriesHitBackBefore;

            return worldPosition;
        }
    }
}
