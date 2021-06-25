using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class SDFSceneData : IDisposable
{
    public const int TileSize = 8;

    public struct TileDataHeader
    {
        public int offset;
        public int numObjects;
        int pad0;
        int pad1;

        public TileDataHeader(int _offset = 0, int _numObjects = 0)
        {
            offset = _offset;
            numObjects = _numObjects;
            pad0 = 0;
            pad1 = 0;
        }
    };
    const int TileDataHeaderSize = 16;

    public struct ObjectHeader
    {
        internal Matrix4x4 worldToObjMatrix;
        internal Matrix4x4 objToWorldMatrix;
        internal Vector4 color;
        internal int objID;
        internal int numEntries;
        internal int startOffset;
        internal int normalsOffset;
        internal float minExtentX;
        internal float minExtentY;
        internal float minExtentZ;
        internal float pad0;
        internal float maxExtentX;
        internal float maxExtentY;
        internal float maxExtentZ;
        internal float pad1;
        internal int voxelDimensionsX;
        internal int voxelDimensionsY;
        internal int voxelDimensionsZ;
        internal float voxelSize;
    };
    const int ObjectHeaderDataSize = 208;
    public int numTilesX;
    public int numTilesY;

    public SDFSceneData(int[] SDFObjectIDs, int sdfDataSize, int normalsSize, Rect pixelRect, Light directionalLight)
    {
        // Compute buffers
        this.objectHeaderComputeBuffer = new ComputeBuffer(SDFObjectIDs.Length, ObjectHeaderDataSize, ComputeBufferType.Default);
        this.sdfDataComputeBuffer = new ComputeBuffer(sdfDataSize, sizeof(float), ComputeBufferType.Default);
        this.normalsComputeBuffer = new ComputeBuffer(normalsSize, 3 * sizeof(float), ComputeBufferType.Default);

        // CPU-side data
        this.objectHeaders = new SDFSceneData.ObjectHeader[SDFObjectIDs.Length];
        this.sdfData = new float[sdfDataSize];
        this.normals = new Vector3[normalsSize];

        // TODO: set actual tile data
        numTilesX = ((int)pixelRect.width + (TileSize - 1)) / TileSize;
        numTilesY = ((int)pixelRect.height + (TileSize - 1)) / TileSize;
        int numTiles = numTilesX * numTilesY;
        this.tileHeaderComputeBuffer = new ComputeBuffer(numTiles, TileDataHeaderSize, ComputeBufferType.Default);
        int tileDataOffsetIntoObjHeaderSize = 4;
        this.tileHeaders = new SDFSceneData.TileDataHeader[numTiles];
        // this.tileHeaders = SDFRayMarch.FillTileDataHeaderBuffer(numTiles).ToArray();
        // SetTileHeaderData();
        this.tileFlagsComputeBuffer  = new ComputeBuffer(numTiles * SDFRayMarch.MAX_OBJECTS_IN_SCENE, tileDataOffsetIntoObjHeaderSize, ComputeBufferType.Default);
        this.tileOffsetsComputeBuffer  = new ComputeBuffer(numTiles * SDFRayMarch.MAX_OBJECTS_IN_SCENE, tileDataOffsetIntoObjHeaderSize, ComputeBufferType.Default);
        // List<int> numEntriesEachTile = new List<int>();
        // for (int tileID = 0; tileID < numTiles; ++tileID)
        //     numEntriesEachTile.Add(1);
        this.tileDataOffsetIntoObjHeaderValues = new int[numTiles * SDFRayMarch.MAX_OBJECTS_IN_SCENE];
        // this.tileDataOffsetIntoObjHeaderValues = SDFRayMarch.FillTileDataOffsetBuffer(numTiles, numEntriesEachTile).ToArray();
        // SetTileOffsetIntoObjHeaderData();

        this.SDFObjectIDs = SDFObjectIDs;
        this.directionalLight = directionalLight;
    }

    public void UpdateObjectHeaderComputeBuffer() => objectHeaderComputeBuffer.SetData(objectHeaders);
    public void UpdateSDFComputeBuffer() => sdfDataComputeBuffer.SetData(sdfData);
    public void UpdateTileHeaderComputeBuffer() => tileHeaderComputeBuffer.SetData(tileHeaders);
    public void UpdateTileOffsetIntoObjHeaderComputeBuffer() => tileOffsetsComputeBuffer.SetData(tileDataOffsetIntoObjHeaderValues);
    public void UpdateNormalsComputeBuffer() => normalsComputeBuffer.SetData(normals);

    public void Dispose() { Dispose(true); }
    protected virtual void Dispose(bool disposing)
    {
        objectHeaderComputeBuffer.Release();
        sdfDataComputeBuffer.Release();
        tileHeaderComputeBuffer.Release();
        tileOffsetsComputeBuffer.Release();
    }

    public ComputeBuffer objectHeaderComputeBuffer;
    public ComputeBuffer sdfDataComputeBuffer;
    public ComputeBuffer tileFlagsComputeBuffer;
    public ComputeBuffer tileHeaderComputeBuffer;
    public ComputeBuffer tileOffsetsComputeBuffer;
    public ComputeBuffer normalsComputeBuffer;

    public SDFSceneData.ObjectHeader[] objectHeaders;
    public float[] sdfData;
    public SDFSceneData.TileDataHeader[] tileHeaders;
    public int[] tileDataOffsetIntoObjHeaderValues;
    public Vector3[] normals;

    public int[] SDFObjectIDs;

    public Light directionalLight;
}

public class SDFLightData
{
    public static readonly int LightColorId = Shader.PropertyToID("LightColor");
    public static readonly int LightDirId = Shader.PropertyToID("LightDirection");

    public Vector4 color;
    public Vector3 direction;

    public void InitializeLightData(Light light)
    {
        color = light.color;
        direction = light.gameObject.transform.forward;
    }

    public void UpdateComputeShaderVariables(CommandBuffer cmd, ComputeShader computeShader)
    {
        cmd.SetComputeVectorParam(computeShader, LightColorId, color);
        cmd.SetComputeVectorParam(computeShader, LightDirId, direction);

    }
}
