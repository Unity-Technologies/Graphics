using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
//[ExecuteInEditMode]


public class SDFRayMarch
{
    //[Reload("Runtime/Shaders/RayMarch.compute")]
    //public ComputeShader rayMarchingCS;

    static readonly int MAX_OBJECTS_IN_SCENE = 50;
    static readonly int MAX_VOXELS_PER_OBJECT = 1024;

    // Out Data
    public static readonly int g_OutSdfData = Shader.PropertyToID("g_OutSdfData");
    public static readonly int g_DebugOutput = Shader.PropertyToID("g_DebugOutput");

    // In data
    public static readonly int _ObjectSDFData = Shader.PropertyToID("_ObjectSDFData");
    public static readonly int _ObjectHeaderData = Shader.PropertyToID("_ObjectHeaderData");
    public static readonly int _TileDataOffsetIntoObjHeader = Shader.PropertyToID("_TileDataOffsetIntoObjHeader");
    public static readonly int _TileDataHeader = Shader.PropertyToID("_TileDataHeader");
    
    public static ComputeBuffer outSdfData;
    struct OutSdfData
    {
        int objID;
        float t;
    };
    const int OutSdfDataSize = 8;

    struct TileDataHeader
    {
	    int  offset;
	    int  numObjects;
	    int  pad0;
	    int  pad1;

        public TileDataHeader(int _offset = 0, int _numObjects = 0)
        {
            offset = _offset;
            numObjects = _numObjects;
            pad0 = 0;
            pad1 = 0;
        }
    };
    const int TileDataHeaderSize = 16;
    public static ComputeBuffer tileDataHeaderBuffer;

    public static ComputeBuffer tileDataOffsetIntoObjHeaderBuffer;

    public struct ObjectHeader
    {
        internal Matrix4x4  worldToObjMatrix;
        internal int      objID;
        internal int      numEntries;
        internal int      startOffset;
        internal float    voxelSize;
        internal float    minExtentX;
        internal float    minExtentY;
        internal float    minExtentZ;
        internal float    pad0;
        internal float    maxExtentX;
        internal float    maxExtentY;
        internal float    maxExtentZ;
        internal float    pad1;
    };
    public const int ObjectHeaderDataSize = 112;
    public static ComputeBuffer objectHeaderDataBuffer;

    public static ComputeBuffer sdfDataBuffer;

    private static List<float> FillSdfDataBuffer()
    {
        List<float> objectSDFDataValues = new List<float>
        {
            0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
            0.0559017f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0559017f,
            0.1677051f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.1677051f,
            0.2795085f, 0.0559017f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0559017f, 0.2795085f,
            0.3913119f, 0.1677051f, 0.0f, 0.0f, 0.0f, 0.0f, 0.1677051f, 0.3913119f,
            0.5031153f, 0.2795085f, 0.0559017f, 0.0f, 0.0f, 0.0559017f, 0.2795085f, 0.5031153f,
            0.6149187f, 0.3913119f, 0.1677051f, 0.0f, 0.0f, 0.1677051f, 0.3913119f, 0.6149187f,
            0.7267221f, 0.5031153f, 0.2795085f, 0.0559017f, 0.0559017f, 0.2795085f, 0.5031153f, 0.7267221f,
        };

        return objectSDFDataValues;
    }

    private static List<ObjectHeader> FillObjectHeaderBuffer(int numObjects)
    {
        List<ObjectHeader> objectHeaderDataValues = new List<ObjectHeader>();
        for (int objID = 0; objID < numObjects; ++objID)
        {
            ObjectHeader objHeaderData = new ObjectHeader();
            objHeaderData.worldToObjMatrix = Matrix4x4.identity;
            objHeaderData.objID = objID;
            objHeaderData.numEntries = 64;
            objHeaderData.startOffset = 0;
            objHeaderData.voxelSize = 0.25f;
            objHeaderData.minExtentX = -1.0f;
            objHeaderData.minExtentY = -1.0f;
            objHeaderData.minExtentZ = 0.0f;
            objHeaderData.pad0 = 0;
            objHeaderData.maxExtentX = 1.0f;
            objHeaderData.maxExtentY = 1.0f;
            objHeaderData.maxExtentZ = 0.0f;
            objHeaderData.pad1 = 0;
            objectHeaderDataValues.Add(objHeaderData);
        }

        return objectHeaderDataValues;
    }

    private static List<TileDataHeader> FillTileDataHeaderBuffer(int numTiles)
    {
        List<TileDataHeader> tileDataHeaderValues = new List<TileDataHeader>();
        for (int tileID = 0; tileID < numTiles; ++tileID)
            tileDataHeaderValues.Add(new TileDataHeader(tileID, 1));

        return tileDataHeaderValues;
    }

    private static List<int> FillTileDataOffsetBuffer(int numTiles, List<int> numEntriesEachTile)
    {
        List<int> tileDataOffsetIntoObjHeaderValues = new List<int>();
        for (int tileID = 0; tileID < numTiles; ++tileID)
            for (int offset = 0; offset < numEntriesEachTile[tileID]; ++offset)
                tileDataOffsetIntoObjHeaderValues.Add(0);

        return tileDataOffsetIntoObjHeaderValues;
    }

    public static void RayMarch(CommandBuffer cmd, ComputeShader rayMarchingCS)
    {
        //rayMarchingCS = defaultResources.shaders.copyChannelCS;
        int resolutionX = 640;
        int resolutionY = 480;
        int rayMarchKernel = rayMarchingCS.FindKernel("RayMarchKernel");
        const int TileSize = 8;
        int numTilesX = (resolutionX + (TileSize - 1)) / TileSize;
        int numTilesY = (resolutionY + (TileSize - 1)) / TileSize;
        int numTiles = numTilesX * numTilesY;

        // _ObjectSDFData
        int objectSDFDataSize = 4;
        sdfDataBuffer = new ComputeBuffer(MAX_OBJECTS_IN_SCENE * MAX_VOXELS_PER_OBJECT, objectSDFDataSize, ComputeBufferType.Default);
        List<float> objectSDFDataValues = FillSdfDataBuffer();
        sdfDataBuffer.SetData(objectSDFDataValues);
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _ObjectSDFData, sdfDataBuffer);
        
        // _ObjectHeaderData
        objectHeaderDataBuffer = new ComputeBuffer(MAX_OBJECTS_IN_SCENE, ObjectHeaderDataSize, ComputeBufferType.Default);
        // TODO - Currently assume 1 object in scene
        const int numObjects = 1;
        List<ObjectHeader> objectHeaderDataValues = FillObjectHeaderBuffer(numObjects);
        objectHeaderDataBuffer.SetData(objectHeaderDataValues);
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _ObjectHeaderData, objectHeaderDataBuffer);

        // Tile Data Header 
        tileDataHeaderBuffer = new ComputeBuffer(numTiles, TileDataHeaderSize, ComputeBufferType.Default);
        List<TileDataHeader> tileDataHeaderValues = FillTileDataHeaderBuffer(numTiles);
        tileDataHeaderBuffer.SetData(tileDataHeaderValues);
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _TileDataHeader, tileDataHeaderBuffer);

        //_TileDataOffsetIntoObjHeader
        int tileDataOffsetIntoObjHeaderSize = 4;
        tileDataOffsetIntoObjHeaderBuffer = new ComputeBuffer(numTiles * MAX_OBJECTS_IN_SCENE, tileDataOffsetIntoObjHeaderSize, ComputeBufferType.Default);
        List<int> numEntriesEachTile = new List<int>();
        for (int tileID = 0; tileID < numTiles; ++tileID)
            numEntriesEachTile.Add(1);
        List<int> tileDataOffsetIntoObjHeaderValues = FillTileDataOffsetBuffer(numTiles, numEntriesEachTile);
        tileDataOffsetIntoObjHeaderBuffer.SetData(tileDataOffsetIntoObjHeaderValues);
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _TileDataOffsetIntoObjHeader, tileDataOffsetIntoObjHeaderBuffer);

        // Dispatch parameters
        outSdfData = new ComputeBuffer(resolutionX * resolutionY, OutSdfDataSize, ComputeBufferType.Default);
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, g_OutSdfData, outSdfData);

        // TODO - we could remove dispatch for tiles that don't have any objects - but that will require compaction of tiledataheader
        cmd.DispatchCompute(rayMarchingCS, rayMarchKernel, numTilesX, numTilesY, 1);
    }

    public static void RayMarchForRealsies(CommandBuffer cmd, ComputeShader rayMarchingCS, Rect pixelRect, ComputeBuffer sdfDataBuffer, ComputeBuffer objectHeaderDataBuffer, Camera camera)
    {
        //rayMarchingCS = defaultResources.shaders.copyChannelCS;
        int resolutionX = (int)pixelRect.width;
        int resolutionY = (int)pixelRect.height;
        int rayMarchKernel = rayMarchingCS.FindKernel("RayMarchKernel");
        const int TileSize = 8;
        int numTilesX = (resolutionX + (TileSize - 1)) / TileSize;
        int numTilesY = (resolutionY + (TileSize - 1)) / TileSize;
        int numTiles = numTilesX * numTilesY;

        // _ObjectSDFData
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _ObjectSDFData, sdfDataBuffer);

        // _ObjectHeaderData
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _ObjectHeaderData, objectHeaderDataBuffer);

#region TODO
        // Tile Data Header 
        tileDataHeaderBuffer = new ComputeBuffer(numTiles, TileDataHeaderSize, ComputeBufferType.Default);
        List<TileDataHeader> tileDataHeaderValues = FillTileDataHeaderBuffer(numTiles);
        tileDataHeaderBuffer.SetData(tileDataHeaderValues);
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _TileDataHeader, tileDataHeaderBuffer);

        //_TileDataOffsetIntoObjHeader
        int tileDataOffsetIntoObjHeaderSize = 4;
        tileDataOffsetIntoObjHeaderBuffer = new ComputeBuffer(numTiles * MAX_OBJECTS_IN_SCENE, tileDataOffsetIntoObjHeaderSize, ComputeBufferType.Default);
        List<int> numEntriesEachTile = new List<int>();
        for (int tileID = 0; tileID < numTiles; ++tileID)
            numEntriesEachTile.Add(1);
        List<int> tileDataOffsetIntoObjHeaderValues = FillTileDataOffsetBuffer(numTiles, numEntriesEachTile);
        tileDataOffsetIntoObjHeaderBuffer.SetData(tileDataOffsetIntoObjHeaderValues);
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _TileDataOffsetIntoObjHeader, tileDataOffsetIntoObjHeaderBuffer);
#endregion
        // Dispatch parameters
        outSdfData = new ComputeBuffer(resolutionX * resolutionY, OutSdfDataSize, ComputeBufferType.Default);
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, g_OutSdfData, outSdfData);
#region DEBUG_ONLY
        RenderTexture tex = new RenderTexture(resolutionX, resolutionY, 24, RenderTextureFormat.ARGBHalf);
        tex.enableRandomWrite = true;
        tex.Create();
        rayMarchingCS.SetTexture(rayMarchKernel, g_DebugOutput, tex);
        #endregion

        // TODO - we could remove dispatch for tiles that don't have any objects - but that will require compaction of tiledataheader
        cmd.DispatchCompute(rayMarchingCS, rayMarchKernel, numTilesX, numTilesY, 1);

        #region DEBUG_ONLY
        // cmd.Blit(tex, null);
        cmd.SetRenderTarget(camera.activeTexture);
        cmd.Blit(tex, BuiltinRenderTextureType.CurrentActive);
        //cmd.Blit(tex, BuiltinRenderTextureType.CameraTarget);
        //tex.Release();

        #endregion
    }

    public static void RayMarchUpdateGIProbe(CommandBuffer cmd, ComputeShader gatherIrradianceCS, int probeResolution) // TODO - more parameters are needed to take in object data
    {
        int kernelIndex = gatherIrradianceCS.FindKernel("GatherIrradiance");

        // TODO - set buffer data

        cmd.DispatchCompute(gatherIrradianceCS, kernelIndex, probeResolution / 8, probeResolution / 8, 1); // [numthreads(8,8,1)]
    }

    public static void RayMarchGIShading(CommandBuffer cmd, ComputeShader giShadingCS, Camera camera, RenderTexture mockRT) // TODO - more parameters are needed to take in object data
    {
        int kernelIndex = giShadingCS.FindKernel("CompositeGI");

        // TODO - set buffer data

        cmd.DispatchCompute(giShadingCS, kernelIndex, (int)Mathf.Ceil(camera.pixelRect.width / 8), (int)Mathf.Ceil(camera.pixelRect.height / 8), 1); // [numthreads(8,8,1)]

        #region DEBUG_ONLY
        //cmd.SetRenderTarget(camera.activeTexture);
        //cmd.Blit(mockRT, BuiltinRenderTextureType.CurrentActive);
        #endregion
    }
}
