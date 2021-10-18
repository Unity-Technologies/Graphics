using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;

namespace UnityEngine.Rendering
{
    public struct AddedMetadataDesc
    {
        public int name;
        public int sizeInVec4s;//size in vec4s
    }

    public struct BRGInternalSRPConfig
    {
        public Mesh overrideMesh;
        public NativeArray<AddedMetadataDesc> metadatas;
        public Material overrideMaterial;

        public static BRGInternalSRPConfig NewDefault()
        {
            return new BRGInternalSRPConfig()
            {
                overrideMesh = null,
                overrideMaterial = null
            };
        }
    }

    public struct AddedRendererInformation
    {
        public int instanceIndex;
        public MeshFilter meshFilter;
    }

    public struct AddRendererParameters
    {
        public List<MeshRenderer> addedRenderers;
        public List<AddedRendererInformation> addedRenderersInfo;

        public NativeArray<Vector4> instanceBuffer;
        public int instanceBufferOffset;
    }

    public struct SubmeshIndexForOverridesParams
    {
        public int instanceBufferOffset;
        public NativeArray<Vector4> instanceBuffer;
        public MeshRenderer renderer;
        public AddedRendererInformation rendererInfo;
    }

    public interface IBRGCallbacks
    {
        public BRGInternalSRPConfig GetSRPConfig();
        public void OnAddRenderers(AddRendererParameters parameters);
        public void OnRemoveRenderers(List<MeshRenderer> renderers);
        public int OnSubmeshIndexForOverrides(SubmeshIndexForOverridesParams parameters);
    }
}
