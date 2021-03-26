using System.Collections.Generic;
using Unity.Collections;
using System;
using UnityEditor;

namespace UnityEngine.Rendering
{
    internal partial class ProbeGIBaking
    {
        private static float FindInDistance(RaycastHit[] hits, ref int index, float kMaxDistance)
        {
            float inDistance = kMaxDistance;
            for (int i = 0; i < hits.Length; ++i)
            {
                RaycastHit hit = hits[i];
                float hitDistance = kMaxDistance - hit.distance;
                if (hit.collider is MeshCollider && IsValidForBaking(hit.collider.gameObject) && hitDistance < inDistance)
                {
                    inDistance = hitDistance;
                    index = i;
                }
            }

            return inDistance;
        }

        class MeshRendererState
        {
            public MeshRenderer meshRenderer;
            public bool neededCollider;
        }

        private static bool IsValidForBaking(GameObject gameObject)
        {
            {
                return true;
            }
        }

        static void FindValidMeshRendererStates(Vector3 worldPosition, Vector3 size, List<MeshRendererState> meshRenderers)
        {
            Vector3 extendedSize = size;

            Bounds volumeBounds = new Bounds(worldPosition, extendedSize);
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
                            // check if this mesh is inside the volume of the light probe volume first
                            if (mr.bounds.Intersects(volumeBounds))
                            {
                                MeshRendererState renderState = new MeshRendererState();
                                renderState.meshRenderer = mr;

                                if (!mr.gameObject.GetComponent<MeshCollider>())
                                {
                                    renderState.neededCollider = true;
                                    mr.gameObject.AddComponent<MeshCollider>();
                                }

                                meshRenderers.Add(renderState);
                            }
                        }
                    }
                }
            }
        }

        static List<MeshRendererState> rendererStates;

        private static Collider[] overlapSphereResults = new Collider[64];

        private static bool IsInsideGeometryV2(Vector3 worldProbePosition)
        {
            const float kMaxDistance = 1000.0f;

            float probeCollisionRadius = 0.01f;
            var collisionLayerMask = ~0;

            // Make sure we're not too close to geometry boundaries
            int numOverlappingColliders = Physics.OverlapSphereNonAlloc(worldProbePosition, probeCollisionRadius, overlapSphereResults, collisionLayerMask);
            for (int i = 0; i < numOverlappingColliders; ++i)
            {
                Collider overlappingCollider = overlapSphereResults[i];
                if (overlappingCollider is MeshCollider
                    && IsValidForBaking(overlappingCollider.gameObject))
                {
                    return true;
                }
            }

            int startAxis = 0;
            int endAxis = ProbeExtraData.s_AxisCount;

            for (int i = startAxis; i < endAxis; ++i)
            {
                Vector3 axis = ProbeExtraData.NeighbourAxis[i];

                //
                // send out an outbound ray for away, and test the distance to the first surface it hits
                // then send out an incoming ray from same destination and measure distance of the last surface it hit
                // then make sure the outbound distnace is less then the closest in bound distance
                //  - this is to help the case of single sided and double sided surfaces
                //
                RaycastHit[] outBoundHits = Physics.SphereCastAll(worldProbePosition, probeCollisionRadius, axis, kMaxDistance, collisionLayerMask);
                RaycastHit[] inBoundHits = Physics.SphereCastAll(worldProbePosition + axis * kMaxDistance, probeCollisionRadius, -axis, kMaxDistance, collisionLayerMask);

                int outIndex = 0, inIndex = 0;
                float outDistance = FindOutDistance(outBoundHits, ref outIndex, kMaxDistance);
                float inDistance = FindInDistance(inBoundHits, ref inIndex, kMaxDistance);
                if (outDistance > (inDistance - probeCollisionRadius))
                {
                    if (!(outDistance > (kMaxDistance - 0.001f) &&
                          inDistance > (kMaxDistance - 0.001f)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void AddOccluders(Vector3 pos, Vector3 size)
        {
            rendererStates = new List<MeshRendererState>();
            {
                FindValidMeshRendererStates(pos, size, rendererStates);
                Physics.autoSimulation = false;
                Physics.Simulate(0.1f);
                Physics.autoSimulation = true;
            }
        }

        private static Color SampleColor(RaycastHit hit, int axisIndex)
        {
            Color color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

            MeshCollider collider = hit.collider as MeshCollider;
            if (collider != null)
            {
                Mesh mesh = collider.sharedMesh;

                uint limit = (uint)hit.triangleIndex * 3;
                int submesh = 0;
                bool foundSubMesh = false;
                for (; submesh < mesh.subMeshCount; submesh++)
                {
                    uint numIndices = mesh.GetIndexCount(submesh);
                    if (numIndices > limit)
                    {
                        foundSubMesh = true;
                        break;
                    }

                    limit -= numIndices;
                }

                if (!foundSubMesh)
                {
                    submesh = mesh.subMeshCount - 1;
                }

                MeshRenderer renderer = collider.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Material material = renderer.sharedMaterials[submesh];
                    Texture2D texture = null;

                    if (material.HasProperty("_BaseColor"))
                    {
                        color = material.GetColor("_BaseColor").linear;
                    }
                }
            }

            return color;
        }

        private static float FindOutDistance(RaycastHit[] hits, ref int index, float kMaxDistance)
        {
            float outDistance = kMaxDistance;
            for (int i = 0; i < hits.Length; ++i)
            {
                RaycastHit hit = hits[i];
                if (hit.collider is MeshCollider
                    && IsValidForBaking(hit.collider.gameObject)
                    && hit.distance < outDistance)
                {
                    outDistance = hit.distance;
                    index = i;
                }
            }

            return outDistance;
        }

        private static bool HasMeshColliderHits(RaycastHit[] outBoundHits, RaycastHit[] inBoundHits)
        {
            foreach (var hit in outBoundHits)
            {
                if (hit.collider is MeshCollider
                    && IsValidForBaking(hit.collider.gameObject))
                {
                    return true;
                }
            }

            foreach (var hit in inBoundHits)
            {
                if (hit.collider is MeshCollider
                    && IsValidForBaking(hit.collider.gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ComputeOccluderColorNormal(Vector3 worldPosition, Vector3 ray, int axisIndex, ref Color color, ref Vector3 normal)
        {
            Vector3 normalizedRay = ray.normalized;
            var collisionLayerMask = ~0;

            RaycastHit[] outBoundHits = Physics.RaycastAll(worldPosition, normalizedRay, ray.magnitude, collisionLayerMask);
            RaycastHit[] inBoundHits = Physics.RaycastAll(worldPosition + ray, -1.0f * normalizedRay, ray.magnitude, collisionLayerMask);

            bool hasMeshColliderHits = HasMeshColliderHits(outBoundHits, inBoundHits);
            if (hasMeshColliderHits)
            {
                int outIndex = 0;
                float outDistance = FindOutDistance(outBoundHits, ref outIndex, ray.magnitude);
                if (outBoundHits.Length > 0)
                {
                    RaycastHit hit = outBoundHits[outIndex];
                    MeshCollider collider = hit.collider as MeshCollider;
                    if (collider != null)
                    {
                        color = SampleColor(hit, axisIndex);
                        color.a = outDistance;
                        normal = outBoundHits[outIndex].normal;
                    }
                    else
                    {
                        color = new Color(0.0f, 0.0f, 0.0f, outDistance);

                        // put a normal opposite of ray if no mesh collider found
                        normal = -normalizedRay;
                    }
                }
                else
                {
                    color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                }

                return true;
            }

            return false;
        }

        private static void GenerateExtraData(Vector3 position, ref ProbeExtraData extraData, float validity)
        {
            extraData.InitExtraData();

            extraData.valid = !(IsInsideGeometryV2(position));

            int hits = 0;
            for (int i = 0; i < ProbeExtraData.s_AxisCount; ++i)
            {
                Vector4 axis = ProbeExtraData.NeighbourAxis[i];
                Vector3 dirAxis = axis;
                float distance = axis.w * ProbeReferenceVolume.instance.MinDistanceBetweenProbes();


                Color color = Color.black;
                Vector3 normal = Vector3.zero;
                if (ComputeOccluderColorNormal(position, dirAxis * distance, i, ref color, ref normal))
                {
                    extraData.NeighbourColour[i] = new Vector3(color.r, color.g, color.b);
                    extraData.NeighbourDistance[i] = color.a;
                    extraData.NeighbourNormal[i] = normal;
                    hits++;
                }
                else
                {
                    extraData.NeighbourColour[i] = Vector3.zero;
                    extraData.NeighbourDistance[i] = 0;
                    extraData.NeighbourNormal[i] = -dirAxis.normalized;
                }
            }

            extraData.validity = validity;

            Debug.Log("HITS: " + hits + " POS " + position);
        }

        private static void CleanupRenderers()
        {
            foreach (MeshRendererState state in rendererStates)
            {
                if (state.neededCollider)
                {
                    MeshCollider collider = state.meshRenderer.gameObject.GetComponent<MeshCollider>();
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }
    }
}
