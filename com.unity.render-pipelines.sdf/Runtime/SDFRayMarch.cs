using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.SDFRP;
//[ExecuteInEditMode]


public class SDFRayMarch
{
    //[Reload("Runtime/Shaders/RayMarch.compute")]
    //public ComputeShader rayMarchingCS;

    public static readonly int MAX_OBJECTS_IN_SCENE = 50;
    public static readonly int MAX_VOXELS_PER_OBJECT = 1024;

    // Out Data
    public static readonly int g_OutSdfData = Shader.PropertyToID("g_OutSdfData");
    public static readonly int g_DebugOutput = Shader.PropertyToID("g_DebugOutput");
    public static readonly int debugLayerId = Shader.PropertyToID("DebugLayer");
    public static readonly int LightingOutput = Shader.PropertyToID("LightingOutput");

    // In data
    public static readonly int _ObjectSDFData = Shader.PropertyToID("_ObjectSDFData");
    public static readonly int _ObjectHeaderData = Shader.PropertyToID("_ObjectHeaderData");
    public static readonly int _TileDataOffsetIntoObjHeader = Shader.PropertyToID("_TileDataOffsetIntoObjHeader");
    public static readonly int _TileDataHeader = Shader.PropertyToID("_TileDataHeader");
    public static readonly int _Normals = Shader.PropertyToID("_Normals");

    struct IntersectionData
    {
        float t;
        Vector3 normal;
        Vector3 position;
        float pad0;
        Vector3 raydir;
        float pad1;
        Vector4 color;
    };

    struct OutSdfData
    {
        IntersectionData data;
        int objID;
    };
    const int OutSdfDataSize = 60;

    public ComputeBuffer outSdfData;
    public RenderTexture lightingOutput;

    private RenderTexture debugOutput;

    private int resolutionX;
    private int resolutionY;

    public SDFRayMarch(Rect pixelRect)
    {
        resolutionX = (int)pixelRect.width;
        resolutionY = (int)pixelRect.height;
        outSdfData = new ComputeBuffer(resolutionX * resolutionY, OutSdfDataSize, ComputeBufferType.Default);
        debugOutput = new RenderTexture(resolutionX, resolutionY, 0, RenderTextureFormat.ARGBHalf);
        debugOutput.enableRandomWrite = true;
        debugOutput.Create();

        lightingOutput = new RenderTexture(resolutionX, resolutionY, 0, RenderTextureFormat.ARGBHalf);
        lightingOutput.enableRandomWrite = true;
        lightingOutput.Create();
    }

    public static List<SDFSceneData.TileDataHeader> FillTileDataHeaderBuffer(int numTiles)
    {
        List<SDFSceneData.TileDataHeader> tileDataHeaderValues = new List<SDFSceneData.TileDataHeader>();
        for (int tileID = 0; tileID < numTiles; ++tileID)
            tileDataHeaderValues.Add(new SDFSceneData.TileDataHeader(tileID, 1));

        return tileDataHeaderValues;
    }

    public static List<int> FillTileDataOffsetBuffer(int numTiles, List<int> numEntriesEachTile)
    {
        List<int> tileDataOffsetIntoObjHeaderValues = new List<int>();
        for (int tileID = 0; tileID < numTiles; ++tileID)
            for (int offset = 0; offset < numEntriesEachTile[tileID]; ++offset)
                tileDataOffsetIntoObjHeaderValues.Add(0);

        return tileDataOffsetIntoObjHeaderValues;
    }

    public void RayMarch(CommandBuffer cmd, ComputeShader rayMarchingCS, SDFSceneData sdfSceneData, SDFRenderPipelineAsset.DebugOutputType debugLayerType)
    {
        //rayMarchingCS = defaultResources.shaders.copyChannelCS;
        int rayMarchKernel = rayMarchingCS.FindKernel("RayMarchKernel");

        int numTilesX = (resolutionX + (SDFSceneData.TileSize - 1)) / SDFSceneData.TileSize;
        int numTilesY = (resolutionY + (SDFSceneData.TileSize - 1)) / SDFSceneData.TileSize;
        int numTiles = numTilesX * numTilesY;

        // _ObjectSDFData
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _ObjectSDFData, sdfSceneData.sdfDataComputeBuffer);

        // _ObjectHeaderData
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _ObjectHeaderData, sdfSceneData.objectHeaderComputeBuffer);

        // Tile Data Header 
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _TileDataHeader, sdfSceneData.tileHeaderComputeBuffer);

        //_TileDataOffsetIntoObjHeader
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _TileDataOffsetIntoObjHeader, sdfSceneData.tileOffsetsComputeBuffer);

        // _Normals
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _Normals, sdfSceneData.normalsComputeBuffer);

        // Dispatch parameters
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, g_OutSdfData, outSdfData);

        #region DEBUG_ONLY
        rayMarchingCS.SetTexture(rayMarchKernel, g_DebugOutput, debugOutput);
        cmd.SetComputeFloatParam(rayMarchingCS, debugLayerId, (float) debugLayerType);
        #endregion

        rayMarchingCS.SetTexture(rayMarchKernel, LightingOutput, lightingOutput);
        // TODO - we could remove dispatch for tiles that don't have any objects - but that will require compaction of tiledataheader
        cmd.DispatchCompute(rayMarchingCS, rayMarchKernel, numTilesX, numTilesY, 1);

#region DEBUG_ONLY
        if (debugLayerType != SDFRenderPipelineAsset.DebugOutputType.None)
        {
            cmd.Blit(debugOutput, BuiltinRenderTextureType.CurrentActive);
        }
        else
#endregion
        {
            cmd.Blit(lightingOutput, BuiltinRenderTextureType.CurrentActive);
        }
    }

    public void RayMarchRSM(CommandBuffer cmd, ComputeShader rayMarchingCS, SDFSceneData sdfSceneData, SDFGenerateRSMData generateRSMData)
    {
        //rayMarchingCS = defaultResources.shaders.copyChannelCS;
        int rayMarchKernel = rayMarchingCS.FindKernel("RayMarchKernel");

        int numTilesX = (resolutionX + (SDFSceneData.TileSize - 1)) / SDFSceneData.TileSize;
        int numTilesY = (resolutionY + (SDFSceneData.TileSize - 1)) / SDFSceneData.TileSize;
        int numTiles = numTilesX * numTilesY;

        // _ObjectSDFData
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _ObjectSDFData, sdfSceneData.sdfDataComputeBuffer);

        // _ObjectHeaderData
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _ObjectHeaderData, sdfSceneData.objectHeaderComputeBuffer);

        // Tile Data Header 
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _TileDataHeader, sdfSceneData.tileHeaderComputeBuffer);

        //_TileDataOffsetIntoObjHeader
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _TileDataOffsetIntoObjHeader, sdfSceneData.tileOffsetsComputeBuffer);

        // _Normals
        cmd.SetComputeBufferParam(rayMarchingCS, rayMarchKernel, _Normals, sdfSceneData.normalsComputeBuffer);

        cmd.DispatchCompute(rayMarchingCS, rayMarchKernel, numTilesX, numTilesY, 1);

        #region DEBUG_ONLY
        cmd.Blit(generateRSMData.m_RSMNormalTexture, BuiltinRenderTextureType.CurrentActive);
        #endregion
    }

    public void RayMarchUpdateGIProbe(CommandBuffer cmd, ComputeShader gatherIrradianceCS, int probeResolution) // TODO - more parameters are needed to take in object data
    {
        int kernelIndex = gatherIrradianceCS.FindKernel("GatherIrradiance");

        // TODO - set buffer data

        cmd.DispatchCompute(gatherIrradianceCS, kernelIndex, probeResolution / 8, probeResolution / 8, 1); // [numthreads(8,8,1)]
    }

    public void RayMarchGIShading(CommandBuffer cmd, ComputeShader giShadingCS, Camera camera) // TODO - more parameters are needed to take in object data
    {
        int kernelIndex = giShadingCS.FindKernel("CompositeGI");

        // TODO - set buffer data
        cmd.DispatchCompute(giShadingCS, kernelIndex, resolutionX / 8, resolutionY / 8, 1); // [numthreads(8,8,1)]
    }
}
