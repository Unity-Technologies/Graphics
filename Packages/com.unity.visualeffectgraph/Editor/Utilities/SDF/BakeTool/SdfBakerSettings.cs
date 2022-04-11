using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.VFX.SDF;

namespace UnityEditor.VFX.SDF
{
    [Serializable]
    class SdfBakerSettings : ScriptableObject
    {
        [SerializeField]
        internal int m_MaxResolution = 64;
        [SerializeField]
        internal Vector3 m_BoxCenter = Vector3.zero;
        [SerializeField]
        internal Vector3 m_BoxSizeReference = Vector3.one;
        [SerializeField]
        internal Vector3Int m_FitPaddingVoxel = Vector3Int.one;
        [SerializeField]
        internal int m_SignPassesCount = 1;
        [SerializeField]
        internal float m_InOutThreshold = 0.5f;
        [SerializeField]
        internal float m_SurfaceOffset = 0.0f;
        [SerializeField]
        internal ModelSource m_ModelSource = ModelSource.Mesh;

        [SerializeField]
        internal Mesh m_SelectedMesh;
        [SerializeField]
        internal GameObject m_MeshPrefab;
        [SerializeField]
        internal Mesh m_Mesh;
        [SerializeField]
        internal bool m_LiftSizeLimit;

        [SerializeField]
        internal PreviewChoice m_PreviewObject = PreviewChoice.MeshAndTexture;


        [OnOpenAsset]
        internal static bool OpenBakeTool(int instanceID, int line)
        {
            SdfBakerSettings settings = EditorUtility.InstanceIDToObject(instanceID) as SdfBakerSettings;
            if (settings != null)
            {
                SDFBakeTool window = EditorWindow.GetWindow<SDFBakeTool>();
                window.Show();
                window.LoadSettings(settings);
                return true;
            }
            return false;
        }

        void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
        }

        internal void ResetToDefault()
        {
            m_InOutThreshold = 0.5f;
            m_MaxResolution = 64;
            m_BoxSizeReference = Vector3.one;
            m_BoxCenter = Vector3.zero;
            m_SignPassesCount = 1;
            m_FitPaddingVoxel = Vector3Int.one;
            m_ModelSource = ModelSource.Mesh;
            m_PreviewObject = PreviewChoice.MeshAndTexture;
            m_MeshPrefab = null;
            m_SelectedMesh = null;
            m_Mesh = null;
            m_LiftSizeLimit = false;
        }

        internal void ApplySelectedMesh()
        {
            m_Mesh = m_SelectedMesh;
        }

        internal void BuildMeshFromPrefab()
        {
            List<Mesh> meshes = new List<Mesh>();
            List<Matrix4x4> transforms = new List<Matrix4x4>();
            if (m_MeshPrefab != null)
            {
                CollectMeshesAndTransforms(m_MeshPrefab, ref meshes, ref transforms);
                if (meshes.Count > 0)
                {
                    m_Mesh = InitMeshFromList(meshes, transforms);
                    m_Mesh.name = m_MeshPrefab.name;
                }
                else
                {
                    m_Mesh = null;
                }
            }
            else
            {
                m_Mesh = null;
            }
        }

        internal void CollectMeshesAndTransforms(GameObject prefab, ref List<Mesh> meshes,
            ref List<Matrix4x4> transforms)
        {
            Mesh currentMesh = null;
            Matrix4x4 currentTransform = Matrix4x4.zero;
            MeshFilter currentMeshFilter = prefab.GetComponent<MeshFilter>();
            SkinnedMeshRenderer currentSkinnedMeshRenderer = prefab.GetComponent<SkinnedMeshRenderer>();
            if (currentMeshFilter)
            {
                currentMesh = currentMeshFilter.sharedMesh;
                currentTransform = prefab.transform.localToWorldMatrix;
            }
            else if (currentSkinnedMeshRenderer)
            {
                currentMesh = new Mesh();
                currentSkinnedMeshRenderer.BakeMesh(currentMesh);
                currentTransform = currentSkinnedMeshRenderer.localToWorldMatrix;
            }

            int nChildren = prefab.transform.childCount;
            if (prefab.activeSelf)
            {
                if (currentMesh != null)
                {
                    meshes.Add(currentMesh);
                    transforms.Add(currentTransform);
                }

                for (var i = 0; i < nChildren; i++)
                {
                    GameObject childObj = prefab.transform.GetChild(i).gameObject;
                    CollectMeshesAndTransforms(childObj, ref meshes, ref transforms);
                }
            }
        }

        internal Mesh InitMeshFromList(List<Mesh> meshes, List<Matrix4x4> transforms)
        {
            int nMeshes = meshes.Count;
            if (nMeshes != transforms.Count)
                throw new ArgumentException("The number of meshes must be the same as the number of transforms");
            List<CombineInstance> combine = new List<CombineInstance>();
            for (var i = 0; i < nMeshes; i++)
            {
                Mesh mesh = meshes[i];
                for (int j = 0; j < mesh.subMeshCount; j++)
                {
                    CombineInstance comb = new CombineInstance();
                    comb.mesh = meshes[i];
                    comb.subMeshIndex = j;
                    comb.transform = transforms[i];
                    combine.Add(comb);
                }
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combine.ToArray());
            return combinedMesh;
        }
    }
    internal enum PreviewChoice
    {
        None = 0,
        Mesh = 1 << 0,
        Texture = 1 << 1,
        MeshAndTexture = Texture | Mesh,
    }

    internal enum ModelSource
    {
        Mesh,
        MeshPrefab,
    }
}

[CustomEditor(typeof(SdfBakerSettings))]
class SdfBakerSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //Left blank intentionally
    }
}
