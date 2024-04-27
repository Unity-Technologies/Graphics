using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering
{
    public static class CapsuleShadowsUtils
    {
        // Draws a WireCapsule into the SceneView to represent the position, rotation and size of a CapsuleOccluder.
        public static void DrawWireCapsule(CapsuleOccluder capsule, Color color)
        {
            Handles.color = color;

            using (new Handles.DrawingScope(capsule.CapsuleToWorld))
            {
                var offset = Mathf.Max(0.0f, 0.5f * capsule.m_Height - capsule.m_Radius);
                var capCenter = new Vector3(0.0f, 0.0f, offset);

                Handles.DrawWireDisc(capCenter, Vector3.forward, capsule.m_Radius);
                Handles.DrawWireDisc(-capCenter, Vector3.forward, capsule.m_Radius);
                Handles.DrawLine(new Vector3(-capsule.m_Radius, 0.0f, -offset),
                    new Vector3(-capsule.m_Radius, 0.0f, offset));
                Handles.DrawLine(new Vector3(capsule.m_Radius, 0.0f, -offset),
                    new Vector3(capsule.m_Radius, 0.0f, offset));
                Handles.DrawLine(new Vector3(0.0f, -capsule.m_Radius, -offset),
                    new Vector3(0.0f, -capsule.m_Radius, offset));
                Handles.DrawLine(new Vector3(0.0f, capsule.m_Radius, -offset),
                    new Vector3(0.0f, capsule.m_Radius, offset));
                Handles.DrawWireArc(capCenter, Vector3.right, Vector3.up, 180.0f, capsule.m_Radius);
                Handles.DrawWireArc(-capCenter, Vector3.right, Vector3.up, -180.0f, capsule.m_Radius);
                Handles.DrawWireArc(capCenter, Vector3.up, Vector3.right, -180.0f, capsule.m_Radius);
                Handles.DrawWireArc(-capCenter, Vector3.up, Vector3.right, 180.0f, capsule.m_Radius);
            }
        }

        public static bool RayIntersectsCapsule(Vector3 rayOrigin, Vector3 rayDir,
            Vector3 capsuleA, Vector3 capsuleB, float capsuleRadius)
        {
            Vector3 rayEnd = rayOrigin + rayDir * 10000.0f; // Put endpoint arbitrarily far away
            (Vector3 a, Vector3 b) =
                CapsuleShadowsUtils.GetClosestMeetingPointsBetweenLines(rayOrigin, rayEnd, capsuleA, capsuleB);

            float sqrDst = (a - b).sqrMagnitude;
            return sqrDst < capsuleRadius * capsuleRadius;

        }

        // Calculates the point on either line (a1,a2 and b1,b2) where the lines are closest to one another
        private static (Vector3, Vector3) GetClosestMeetingPointsBetweenLines(Vector3 a1, Vector3 a2, Vector3 b1,
            Vector3 b2)
        {
            Vector3 lineAOffset = a2 - a1;
            Vector3 lineBOffset = b2 - b1;

            var r = b1 - a1;
            var ru = Vector3.Dot(r, lineAOffset);
            var rv = Vector3.Dot(r, lineBOffset);
            var uu = Vector3.Dot(lineAOffset, lineAOffset);
            var uv = Vector3.Dot(lineAOffset, lineBOffset);
            var vv = Vector3.Dot(lineBOffset, lineBOffset);

            var det = uu * vv - uv * uv;

            var s = Mathf.Clamp01((ru * vv - rv * uv) / det);
            var t = Mathf.Clamp01((ru * uv - rv * uu) / det);

            var S = Mathf.Clamp01((t * uv + ru) / uu);
            var T = Mathf.Clamp01((s * uv - rv) / vv);

            Vector3 pointOnA = a1 + S * lineAOffset;
            Vector3 pointOnB = b1 + T * lineBOffset;

            return (pointOnA, pointOnB);
        }

        public static void GenerateSingleCapsule(CapsuleShadows capsuleShadows, SkinnedMeshRenderer skinnedMesh, CapsuleOccluder capsule)
        {
            Transform[] bones = skinnedMesh.bones;
            Mesh sharedMesh = skinnedMesh.sharedMesh;
            Matrix4x4[] bindposes = sharedMesh.bindposes;

            int boneCount = bones.Length;
            if (boneCount != bindposes.Length)
                throw new Exception("Bone count mismatch!");

            Vector3[] vertices = sharedMesh.vertices;
            var bonesPerVertex = sharedMesh.GetBonesPerVertex();
            var boneWeights = sharedMesh.GetAllBoneWeights();

            Action<Action<int, Vector3>> vertexVisitor = action =>
            {
                int boneWeightIndex = 0;
                for (int vertIndex = 0; vertIndex < sharedMesh.vertexCount; ++vertIndex)
                {
                    Vector3 bindPosition = vertices[vertIndex];
                    int vertBoneCount = bonesPerVertex[vertIndex];
                    for (int vertBoneIndex = 0; vertBoneIndex < vertBoneCount; ++vertBoneIndex)
                    {
                        BoneWeight1 bw = boneWeights[boneWeightIndex];
                        if (bw.weight > 0.5f)
                            action(bw.boneIndex, bindposes[bw.boneIndex].MultiplyPoint3x4(bindPosition));
                        ++boneWeightIndex;
                    }
                }
            };

            // compute the mean vertex position per bone
            BoneData[] boneTemp = new BoneData[boneCount];
            vertexVisitor((boneIndex, localPosition) =>
            {
                ref BoneData temp = ref boneTemp[boneIndex];
                temp.mean += localPosition;
                temp.count += 1;
            });
            foreach (ref BoneData temp in boneTemp.AsSpan())
            {
                temp.mean /= temp.count;
                temp.boneBounds = new Bounds(temp.mean, Vector3.zero);
            }

            // compute covariance
            vertexVisitor((boneIndex, localPosition) =>
            {
                ref BoneData temp = ref boneTemp[boneIndex];
                temp.boneBounds.Encapsulate(localPosition);
                Vector3 offset = localPosition - temp.mean;
                temp.diag += new Vector3(offset.x * offset.x, offset.y * offset.y, offset.z * offset.z);
                temp.cross += new Vector3(offset.y * offset.z, offset.z * offset.x, offset.x * offset.y);
            });
            foreach (ref BoneData temp in boneTemp.AsSpan())
            {
                temp.diag /= temp.count;
                temp.cross /= temp.count;
            }

            // compute rotation from the principal axis
            foreach (ref BoneData temp in boneTemp.AsSpan())
            {
                if (temp.count >= CapsuleShadows.MINIMUM_VERTICES_FOR_PCA)
                {
                    Vector3 axis = new Vector3(1.0f, 1.0f, 1.0f) / Mathf.Sqrt(3.0f);
                    for (int i = 0; i < 100; ++i)
                    {
                        axis = new Vector3(
                            axis.x * temp.diag.x + axis.y * temp.cross.z + axis.z * temp.cross.y,
                            axis.x * temp.cross.z + axis.y * temp.diag.y + axis.z * temp.cross.x,
                            axis.x * temp.cross.y + axis.y * temp.cross.x + axis.z * temp.diag.z);
                        axis /= axis.magnitude;
                    }

                    temp.rotationInverse = Quaternion.FromToRotation(axis, Vector3.forward);
                }
                else
                {
                    temp.rotationInverse = Quaternion.identity;
                }

                temp.capsuleBounds = new Bounds(temp.rotationInverse * temp.mean, Vector3.zero);
            }

            // compute bounds in this rotated space
            vertexVisitor((boneIndex, localPosition) =>
            {
                ref BoneData temp = ref boneTemp[boneIndex];
                temp.capsuleBounds.Encapsulate(temp.rotationInverse * localPosition);
            });

            // convert to capsules at each bone
            for (int boneIndex = 0; boneIndex < boneCount; ++boneIndex)
            {
                if(bones[boneIndex] == capsule.gameObject.transform)
                {
                    ref readonly BoneData temp = ref boneTemp[boneIndex];

                    CapsuleParams param = capsuleShadows.m_KeepAxisAligned
                        ? CapsuleParams.FromOrientedBounds(Quaternion.identity, temp.boneBounds)
                        : CapsuleParams.FromOrientedBounds(Quaternion.Inverse(temp.rotationInverse),
                            temp.capsuleBounds);

                    capsule.SetOriginalParams(param);
                }
            }

        }
        // Creates CapsuleOccluders for each bone on a SkinnedMeshRenderer Component and adds the components to the bones GameObject.
        public static void GenerateCapsules(CapsuleShadows capsuleShadows, SkinnedMeshRenderer skinnedMesh)
        {
            Transform[] bones = skinnedMesh.bones;
            Mesh sharedMesh = skinnedMesh.sharedMesh;
            Matrix4x4[] bindposes = sharedMesh.bindposes;

            int boneCount = bones.Length;
            if (boneCount != bindposes.Length)
                throw new Exception("Bone count mismatch!");

            Vector3[] vertices = sharedMesh.vertices;
            var bonesPerVertex = sharedMesh.GetBonesPerVertex();
            var boneWeights = sharedMesh.GetAllBoneWeights();

            Action<Action<int, Vector3>> vertexVisitor = action =>
            {
                int boneWeightIndex = 0;
                for (int vertIndex = 0; vertIndex < sharedMesh.vertexCount; ++vertIndex)
                {
                    Vector3 bindPosition = vertices[vertIndex];
                    int vertBoneCount = bonesPerVertex[vertIndex];
                    for (int vertBoneIndex = 0; vertBoneIndex < vertBoneCount; ++vertBoneIndex)
                    {
                        BoneWeight1 bw = boneWeights[boneWeightIndex];
                        if (bw.weight > 0.5f)
                            action(bw.boneIndex, bindposes[bw.boneIndex].MultiplyPoint3x4(bindPosition));
                        ++boneWeightIndex;
                    }
                }
            };

            // compute the mean vertex position per bone
            BoneData[] boneTemp = new BoneData[boneCount];
            vertexVisitor((boneIndex, localPosition) =>
            {
                ref BoneData temp = ref boneTemp[boneIndex];
                temp.mean += localPosition;
                temp.count += 1;
            });
            foreach (ref BoneData temp in boneTemp.AsSpan())
            {
                temp.mean /= temp.count;
                temp.boneBounds = new Bounds(temp.mean, Vector3.zero);
            }

            // compute covariance
            vertexVisitor((boneIndex, localPosition) =>
            {
                ref BoneData temp = ref boneTemp[boneIndex];
                temp.boneBounds.Encapsulate(localPosition);
                Vector3 offset = localPosition - temp.mean;
                temp.diag += new Vector3(offset.x * offset.x, offset.y * offset.y, offset.z * offset.z);
                temp.cross += new Vector3(offset.y * offset.z, offset.z * offset.x, offset.x * offset.y);
            });
            foreach (ref BoneData temp in boneTemp.AsSpan())
            {
                temp.diag /= temp.count;
                temp.cross /= temp.count;
            }

            // compute rotation from the principal axis
            foreach (ref BoneData temp in boneTemp.AsSpan())
            {
                if (temp.count >= CapsuleShadows.MINIMUM_VERTICES_FOR_PCA)
                {
                    Vector3 axis = new Vector3(1.0f, 1.0f, 1.0f) / Mathf.Sqrt(3.0f);
                    for (int i = 0; i < 100; ++i)
                    {
                        axis = new Vector3(
                            axis.x * temp.diag.x + axis.y * temp.cross.z + axis.z * temp.cross.y,
                            axis.x * temp.cross.z + axis.y * temp.diag.y + axis.z * temp.cross.x,
                            axis.x * temp.cross.y + axis.y * temp.cross.x + axis.z * temp.diag.z);
                        axis /= axis.magnitude;
                    }

                    temp.rotationInverse = Quaternion.FromToRotation(axis, Vector3.forward);
                }
                else
                {
                    temp.rotationInverse = Quaternion.identity;
                }

                temp.capsuleBounds = new Bounds(temp.rotationInverse * temp.mean, Vector3.zero);
            }

            // compute bounds in this rotated space
            vertexVisitor((boneIndex, localPosition) =>
            {
                ref BoneData temp = ref boneTemp[boneIndex];
                temp.capsuleBounds.Encapsulate(temp.rotationInverse * localPosition);
            });

            // convert to capsules at each bone
            for (int boneIndex = 0; boneIndex < boneCount; ++boneIndex)
            {
                CapsuleOccluder capsule = bones[boneIndex].GetComponent<CapsuleOccluder>();
                if (capsule == null)
                {
                    ref readonly BoneData temp = ref boneTemp[boneIndex];
                    CapsuleParams param = capsuleShadows.m_KeepAxisAligned
                        ? CapsuleParams.FromOrientedBounds(Quaternion.identity, temp.boneBounds)
                        : CapsuleParams.FromOrientedBounds(Quaternion.Inverse(temp.rotationInverse),
                            temp.capsuleBounds);

                    param.diameter *= capsuleShadows.m_RadiusScale;
                    if (param.height < capsuleShadows.m_MinimumLength &&
                        param.diameter < capsuleShadows.m_MinimumLength)
                        continue;

                    capsule = Undo.AddComponent<CapsuleOccluder>(bones[boneIndex].gameObject);
                    capsule.SetOriginalParams(param);
                }
            }
        }

        public static List<CapsuleModel> GetAllModels(SkinnedMeshRenderer meshRenderer)
        {
            List<CapsuleModel> capsuleModels = new List<CapsuleModel>();
            AddModel(capsuleModels, meshRenderer.rootBone, 0,meshRenderer);
            return capsuleModels;
        }

        public static int AddModel(List<CapsuleModel> list, Transform transform, int id, SkinnedMeshRenderer renderer)
        {
            List<CapsuleModel> children = null;
            int childId = id;
            if (transform.childCount > 0)
            {
                children = new();
                for (int i = 0; i < transform.childCount; i++)
                {
                    childId = AddModel(children, transform.GetChild(i), childId + 1,renderer);
                }
            }

            CapsuleModel capsule = new CapsuleModel();
            capsule.m_Occluder = transform.GetComponent<CapsuleOccluder>();
            capsule.m_BoneTransform = transform;
            capsule.m_SkinnedMeshRenderer = renderer;
            capsule.m_SubItems = children;
            capsule.m_Sequence = id;

            if (capsule.m_Occluder != null)
            {
                capsule.m_Occluder.SetModel(capsule);
            }

            list.Add(capsule);

            return childId;
        }
    }

    public struct BoneData
     {
         public Vector3 mean;
         public uint count;
         public Bounds boneBounds;
         public Vector3 diag; // covariance xx, yy, zz
         public Vector3 cross; // covariance yz, zx, xy
         public Quaternion rotationInverse;
         public Bounds capsuleBounds; // in local space of capsule
     };
}
