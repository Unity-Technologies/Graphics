using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

namespace UnityEngine.Rendering.MeshDecal
{
    [RequireComponent(typeof(MeshFilter), (typeof(MeshRenderer)))]
    [ExecuteAlways]
    public partial class MeshDecalProjector : DecalBase
    {
        public float radius => m_Size.magnitude * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));

        public bool useManualMeshFilters = false;
        public List<MeshFilter> manualMeshFilters;

        public float offset = 0.001f;

        public Sprite sprite;

        public Texture2D albedo;
        public Texture2D normal;

        [SerializeField, HideInInspector] private Material decalBaseMaterial;

        private Rect m_uvRect => new Rect(m_UVBias, m_UVScale);

        [Range(0f, 1f)]
        public float normalBlend = 1f;

        public bool realtimeUpdate = false;

        List<MeshFilter> meshFilters = new List<MeshFilter>();
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        MaterialPropertyBlock propertyBlock;
        Mesh mesh;

        [Header("Debug")]
        public bool drawSourceMeshes = false;

        private void Update()
        {
            if (realtimeUpdate && transform.hasChanged)
                GenerateDecal();
        }

        [ContextMenu("Generate Decal")]
        public void GenerateDecal()
        {
            // Get the MeshFilters list that the decal volume intersect
            if (useManualMeshFilters)
                meshFilters = manualMeshFilters;
            else
                GetMeshes();

            // Generate the decal mesh from the filtered objects
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = m_Material;

            if (mesh == null) mesh = new Mesh();
            mesh.name = "Decal Mesh";
            meshFilter.sharedMesh = mesh;

            GenerateDecalMesh();
        }

        private void OnEnable()
        {
            MeshDecalProjectorsManager.RegisterDecalProjector(this);
        }

        private void OnDisable()
        {
            MeshDecalProjectorsManager.UnregisterDecalProjector(this);
        }

        public void SetAtlasData(Texture atlasTexture)
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            propertyBlock.SetTexture("_DecalAtlas", atlasTexture);

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        public void SetAtlasData(string propertyName, Vector4 posSize)
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            propertyBlock.SetVector(propertyName, posSize);

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        void OnDrawGizmosSelected()
        {
            if (drawSourceMeshes &&
                !useManualMeshFilters && meshFilters != null && meshFilters.Count != 0 ||
                useManualMeshFilters && manualMeshFilters != null && manualMeshFilters.Count != 0
                )
            {
                Gizmos.color = new Color(1, 1, 1, 0.03f);
                foreach (var filter in (useManualMeshFilters ? manualMeshFilters : meshFilters))
                {
                    Gizmos.matrix = filter.transform.localToWorldMatrix;
                    Gizmos.DrawWireMesh(filter.sharedMesh);
                }
            }

            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, m_Size);

            // Debug Normals
            /*
            Gizmos.color = Color.blue;
            for (int i=0; i<mesh.vertexCount; i++)
            {
                Gizmos.DrawRay(mesh.vertices[i], mesh.normals[i] * 0.1f);
            }
            //*/
        }
    }
}
