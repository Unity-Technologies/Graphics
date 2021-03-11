using System.Collections.Generic;
using Unity.Collections;
using System;
using UnityEditor;

namespace UnityEngine.Rendering
{
    internal partial class ProbeGIBaking
    {
        //private static bool IsValidForBaking(GameObject gameObject)
        //{
        //    return true;
        //    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(gameObject);
        //    if (gameObject.activeSelf && (flags & StaticEditorFlags.ContributeGI) == StaticEditorFlags.ContributeGI)
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        //private static bool HasMeshColliderHits(RaycastHit[] outBoundHits, RaycastHit[] inBoundHits, out string name)
        //{
        //    name = "";
        //    foreach (var hit in outBoundHits)
        //    {
        //        if (hit.collider is MeshCollider && IsValidForBaking(hit.collider.gameObject))
        //        {
        //            name = hit.collider.name;
        //            return true;
        //        }
        //    }

        //    foreach (var hit in inBoundHits)
        //    {
        //        if (hit.collider is MeshCollider && IsValidForBaking(hit.collider.gameObject))
        //        {
        //            name = hit.collider.name;
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //private static float FindOutDistance(RaycastHit[] hits, ref int index, float kMaxDistance)
        //{
        //    float outDistance = kMaxDistance;
        //    for (int i = 0; i < hits.Length; ++i)
        //    {
        //        RaycastHit hit = hits[i];
        //        if (hit.collider is MeshCollider && IsValidForBaking(hit.collider.gameObject) && hit.distance < outDistance)
        //        {
        //            outDistance = hit.distance;
        //            index = i;
        //        }
        //    }

        //    return outDistance;
        //}

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

        // private void
        //private static void GenerateExtraData(Vector3 position, ref ProbeExtraData extraData)
        //{
        //    //if (extraData == null)
        //    //    extraData = new ProbeExtraData();

        //    extraData.InitExtraData();

        //    var count = 0;
        //    for (int i = 0; i < ProbeExtraData.s_AxisCount; ++i)
        //    {
        //        Vector4 rayAndLen = ProbeExtraData.NeighbourAxis[i];

        //        Vector3 unnormalizedDir = new Vector3(rayAndLen.x, rayAndLen.y, rayAndLen.z) * rayAndLen.w;
        //        Vector3 dir = unnormalizedDir.normalized;
        //        float rayLength = rayAndLen.w;

        //        Vector4 axis = ProbeExtraData.NeighbourAxis[i];

        //        Vector3 dirAxis = axis;
        //        float distance = axis.w * ProbeExtraData.s_NeighbourSearchDistance;

        //        Vector3 ray = dirAxis * distance;
        //        Vector3 normalizedRay = ray.normalized;

        //        var collisionLayerMask = ~0;
        //        RaycastHit[] outBoundHits = Physics.RaycastAll(position, normalizedRay, ray.magnitude, collisionLayerMask);
        //        RaycastHit[] inBoundHits = Physics.RaycastAll(position + ray, -1.0f * normalizedRay, ray.magnitude, collisionLayerMask);

        //        string name;
        //        bool hasHit = HasMeshColliderHits(outBoundHits, inBoundHits, out name);

        //        if (hasHit)
        //        {
        //            int outIndex = 0;
        //            float outDistance = FindOutDistance(outBoundHits, ref outIndex, rayLength);

        //            if (outBoundHits.Length > 0)
        //            {
        //                RaycastHit hit = outBoundHits[outIndex];
        //                MeshCollider collider = hit.collider as MeshCollider;

        //                extraData.NeighbourDistance[i] = outDistance;

        //                if (collider != null)
        //                {
        //                    // TODO: Add color

        //                    if (name.Contains("red"))
        //                    {
        //                        extraData.NeighbourColour[i] = new Vector3(1, 0, 0);
        //                    }
        //                    else if (name.Contains("green"))
        //                    {
        //                        extraData.NeighbourColour[i] = new Vector3(0, 1, 0);
        //                    }
        //                    else if (name.Contains("blue"))
        //                    {
        //                        extraData.NeighbourColour[i] = new Vector3(0, 0, 1);
        //                        Debug.Log("HIT BLUE " + position + " AXIS " + dir);
        //                    }
        //                    //else if (name.Contains("side"))
        //                    //{
        //                    //    extraData.NeighbourColour[i] = new Vector3(0, 1, 1);
        //                    //}
        //                    else
        //                    {
        //                        extraData.NeighbourColour[i] = new Vector3(1, 1, 1);
        //                    }
        //                    // Sample normal map normal.
        //                    extraData.NeighbourNormal[i] = outBoundHits[outIndex].normal;
        //                }
        //                else
        //                {
        //                    extraData.NeighbourColour[i] = new Vector3(0, 0, 0);
        //                    extraData.NeighbourNormal[i] = -dir;
        //                }
        //            }

        //            count++;
        //        }
        //    }

        //    Debug.Log("Hit count! " + count);
        //}
    }
}
