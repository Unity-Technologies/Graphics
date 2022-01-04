using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public class CapsuleOccluderBuilder : ScriptableWizard
    {
        public SkinnedMeshRenderer skinnedMesh; // TODO: list?
        public bool keepAxisAligned = false;
        public float minimumLength = 0.15f;
        public float radiusScale = 0.8f;

        private const int MINIMUM_VERTICES_FOR_PCA = 16;

        [MenuItem("GameObject/Rendering/Capsule Occluders...")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<CapsuleOccluderBuilder>("Create Capsule Occluders");
        }

        void OnWizardUpdate()
        {
            isValid = (skinnedMesh != null);
        }

        private struct BoneData
        {
            public Vector3 mean;
            public uint count;
            public Bounds boneBounds;
            public Vector3 diag; // covariance xx, yy, zz
            public Vector3 cross; // covariance yz, zx, xy
            public Quaternion rotationInverse;
            public Bounds capsuleBounds; // in local space of capsule
        };

        private struct CapsuleParams
        {
            public Vector3 center;
            public Quaternion rotation;
            public float height;
            public float diameter;

            public static CapsuleParams FromOrientedBounds(Quaternion rotation, Bounds bounds)
            {
                Vector3 center = rotation * bounds.center;
                Vector3 size = bounds.size;

                float height = size.z;
                float diameter = Mathf.Max(size.x, size.y);
                Quaternion capsuleRotation = rotation;
                if (size.y > height)
                {
                    height = size.y;
                    diameter = Mathf.Max(size.z, size.x);
                    capsuleRotation = rotation * Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                }
                if (size.x > height)
                {
                    height = size.x;
                    diameter = Mathf.Max(size.y, size.z);
                    capsuleRotation = rotation * Quaternion.FromToRotation(Vector3.right, Vector3.forward);
                }

                return new CapsuleParams {
                    center = center,
                    rotation = capsuleRotation,
                    height = height,
                    diameter = diameter,
                };
            }
        };

        void OnWizardCreate()
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

            Action<Action<int, Vector3>> vertexVisitor = action => {
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
            vertexVisitor((boneIndex, localPosition) => {
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
            vertexVisitor((boneIndex, localPosition) => {
                ref BoneData temp = ref boneTemp[boneIndex];
                temp.boneBounds.Encapsulate(localPosition);
                Vector3 offset = localPosition - temp.mean;
                temp.diag += new Vector3(offset.x*offset.x, offset.y*offset.y, offset.z*offset.z);
                temp.cross += new Vector3(offset.y*offset.z, offset.z*offset.x, offset.x*offset.y);
            });
            foreach (ref BoneData temp in boneTemp.AsSpan())
            {
                temp.diag /= temp.count;
                temp.cross /= temp.count;
            }

            // compute rotation from the principal axis
            foreach (ref BoneData temp in boneTemp.AsSpan())
            {
                if (temp.count >= MINIMUM_VERTICES_FOR_PCA)
                {
                    Vector3 axis = new Vector3(1.0f, 1.0f, 1.0f)/Mathf.Sqrt(3.0f);
                    for (int i = 0; i < 100; ++i)
                    {
                        axis = new Vector3(
                            axis.x*temp.diag.x + axis.y*temp.cross.z + axis.z*temp.cross.y,
                            axis.x*temp.cross.z + axis.y*temp.diag.y + axis.z*temp.cross.x,
                            axis.x*temp.cross.y + axis.y*temp.cross.x + axis.z*temp.diag.z);
                        axis /= axis.magnitude;
                    }
                    temp.rotationInverse = Quaternion.FromToRotation(axis, Vector3.forward);
                }
                else
                {
                    temp.rotationInverse =  Quaternion.identity;
                }
                temp.capsuleBounds = new Bounds(temp.rotationInverse * temp.mean, Vector3.zero);
            }

            // compute bounds in this rotated space
            vertexVisitor((boneIndex, localPosition) => {
                ref BoneData temp = ref boneTemp[boneIndex];
                temp.capsuleBounds.Encapsulate(temp.rotationInverse * localPosition);
            });

            // convert to capsules at each bone
            for (int boneIndex = 0; boneIndex < boneCount; ++boneIndex)
            {
                CapsuleOccluder capsule = bones[boneIndex].GetComponent<CapsuleOccluder>();
                if (capsule != null)
                {
                    Undo.DestroyObjectImmediate(capsule);
                }

                ref readonly BoneData temp = ref boneTemp[boneIndex];
                CapsuleParams param = keepAxisAligned
                    ? CapsuleParams.FromOrientedBounds(Quaternion.identity, temp.boneBounds)
                    : CapsuleParams.FromOrientedBounds(Quaternion.Inverse(temp.rotationInverse), temp.capsuleBounds);

                param.diameter *= radiusScale;
                if (param.height < minimumLength && param.diameter < minimumLength)
                    continue;

                capsule = Undo.AddComponent<CapsuleOccluder>(bones[boneIndex].gameObject);
                capsule.center = param.center;
                capsule.rotation = param.rotation;
                capsule.height = param.height;
                capsule.radius = 0.5f * param.diameter;
            }
        }
    }
}
