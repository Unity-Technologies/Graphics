using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public class CapsuleShadowBuilder : ScriptableWizard
    {
        public SkinnedMeshRenderer skinnedMesh; // TODO: list?
        public float minimumLength = 0.15f;
        public float radiusScale = 0.8f;

        [MenuItem("GameObject/Rendering/Capsule Shadows...")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<CapsuleShadowBuilder>("Create Capsule Shadows");
        }

        void OnWizardUpdate()
        {
            isValid = (skinnedMesh != null);
        }

        void OnWizardCreate()
        {
            Transform[] bones = skinnedMesh.bones;
            Mesh sharedMesh = skinnedMesh.sharedMesh;
            Matrix4x4[] bindposes = sharedMesh.bindposes;

            int boneCount = bones.Length;
            if (boneCount != bindposes.Length)
                throw new Exception("Bone count mismatch!");

            // gather up local space bounds for each bone, using vertices with >50% weight
            Bounds[] bounds = new Bounds[boneCount];
            bool[] boundsValid = new bool[boneCount];
            var bonesPerVertex = sharedMesh.GetBonesPerVertex();
            var boneWeights = sharedMesh.GetAllBoneWeights();
            int boneWeightIndex = 0;
            for (int vertIndex = 0; vertIndex < sharedMesh.vertexCount; ++vertIndex)
            {
                Vector3 bindPosition = sharedMesh.vertices[vertIndex];
                int vertBoneCount = bonesPerVertex[vertIndex];
                for (int vertBoneIndex = 0; vertBoneIndex < vertBoneCount; ++vertBoneIndex)
                {
                    BoneWeight1 bw = boneWeights[boneWeightIndex];
                    if (bw.weight > 0.5f)
                    {
                        Vector3 localPosition = bindposes[bw.boneIndex].MultiplyPoint3x4(bindPosition);
                        if (boundsValid[bw.boneIndex])
                        {
                            bounds[bw.boneIndex].Encapsulate(localPosition);
                        }
                        else
                        {
                            boundsValid[bw.boneIndex] = true;
                            bounds[bw.boneIndex] = new Bounds(localPosition, Vector3.zero);
                        }
                    }
                    ++boneWeightIndex;
                }
            }

            // convert to capsules at each bone
            for (int boneIndex = 0; boneIndex < boneCount; ++boneIndex)
            {
                if (!boundsValid[boneIndex])
                    continue;

                CapsuleOccluder capsule = bones[boneIndex].GetComponent<CapsuleOccluder>();
                if (capsule != null)
                {
                    Undo.DestroyObjectImmediate(capsule);
                }

                Vector3 size = bounds[boneIndex].size;

                float height = size.x;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.right, Vector3.forward);
                if (size.y > height)
                {
                    height = size.y;
                    rotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                }
                if (size.z > height)
                {
                    height = size.z;
                    rotation = Quaternion.identity;
                }
                if (height < minimumLength)
                    continue;

                float radius = 0.0f;
                for (int i = 0; i < 3; ++i)
                {
                    float d = size[i];
                    if (d < height) { 
                        radius = Mathf.Max(radius, radiusScale * 0.5f * d);
                    }
                }

                capsule = Undo.AddComponent<CapsuleOccluder>(bones[boneIndex].gameObject);
                capsule.center = bounds[boneIndex].center;
                capsule.rotation = rotation;
                capsule.height = height;
                capsule.radius = radius;
            }
        }
    }
}
