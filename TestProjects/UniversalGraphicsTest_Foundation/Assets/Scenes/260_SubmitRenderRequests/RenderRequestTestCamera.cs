// #define DEBUG_GPU_CAPTURE

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class RenderRequestTestCamera : MonoBehaviour
{
    public Camera.RenderRequestMode debugMode = Camera.RenderRequestMode.None;
    public bool debugRenderEachFrame = false;

    private Camera m_Camera;
    private RenderRequestTestTarget[] m_Targets;
    private Dictionary<RenderRequestTestTarget, bool> m_Failed;

    private List<Camera.RenderRequest> m_RenderRequests;
    private const int kRenderTargetDimension = 1024;
    private const GraphicsFormat kVectorFormat = GraphicsFormat.R32G32B32A32_SFloat;
    private const GraphicsFormat kIdFormat = GraphicsFormat.R8G8B8A8_UNorm;
    private const float kVectorTolerance = 0.1f;

    private int2 RenderTargetDimension => new int2(m_Camera.pixelWidth, m_Camera.pixelHeight);

    private RenderTexture MakeRenderTarget(GraphicsFormat fmt) =>
        new RenderTexture(RenderTargetDimension.x, RenderTargetDimension.y,
            fmt,
            GraphicsFormat.D32_SFloat);

    private int ReadbackDataIndex(float3 viewportSpace)
    {
        float2 uv = viewportSpace.xy;
        float2 px = math.saturate(uv) * RenderTargetDimension;
        int2 xy = (int2)px;
        return xy.y * RenderTargetDimension.x + xy.x;
    }

    private static float3 UnpackNormal(float3 packedNormal)
    {
        return packedNormal.xyz * 2.0f - 1.0f;
    }

    private void Start()
    {
#if DEBUG_GPU_CAPTURE
        ExternalGPUProfiler.BeginGPUCapture();
#endif

        m_Camera = GetComponent<Camera>();
        m_Targets = FindObjectsOfType<RenderRequestTestTarget>();
        m_RenderRequests = new List<Camera.RenderRequest>();
        m_Failed = new Dictionary<RenderRequestTestTarget, bool>();

        Camera.RenderRequest requestPosition = new Camera.RenderRequest(
            Camera.RenderRequestMode.WorldPosition,
            MakeRenderTarget(kVectorFormat));
        m_RenderRequests.Add(requestPosition);

        Camera.RenderRequest requestNormal = new Camera.RenderRequest(
            Camera.RenderRequestMode.Normal,
            MakeRenderTarget(kVectorFormat));
        m_RenderRequests.Add(requestNormal);

        Camera.RenderRequest requestObjectId = new Camera.RenderRequest(
            Camera.RenderRequestMode.ObjectId,
            MakeRenderTarget(kIdFormat));
        m_RenderRequests.Add(requestObjectId);

        m_Camera.SubmitRenderRequests(m_RenderRequests);

        var readbackPosition = AsyncGPUReadback.Request(requestPosition.result);
        var readbackNormal = AsyncGPUReadback.Request(requestNormal.result);
        var readbackObjectId = AsyncGPUReadback.Request(requestObjectId.result);

        AsyncGPUReadback.WaitAllRequests();

#if DEBUG_GPU_CAPTURE
        ExternalGPUProfiler.EndGPUCapture();
#endif

        ValidatePosition(readbackPosition);
        ValidateNormal(readbackNormal);
        ValidateObjectId(readbackObjectId);

        UpdateTestText();
    }

    private void UpdateTestText()
    {
        foreach (var t in m_Targets)
        {
            t.SetTestText(m_Camera, m_Failed.ContainsKey(t) ? "FAILED" : "OK");
        }
    }

    private void OnGUI()
    {
        if (debugMode == Camera.RenderRequestMode.None)
            return;

        foreach (var r in m_RenderRequests)
        {
            if (r.mode == debugMode)
            {
                if (debugRenderEachFrame)
                    m_Camera.SubmitRenderRequests(new List<Camera.RenderRequest> { r });

                GUI.DrawTexture(m_Camera.pixelRect, r.result, ScaleMode.ScaleToFit, false);
                return;
            }
        }
    }

    private void Validate(RenderRequestTestTarget t, float3 actual, float3 expected, Camera.RenderRequestMode mode)
    {
        float diff = math.length(actual - expected);
        if (diff > kVectorTolerance)
        {
            m_Failed[t] = true;
            Debug.LogError($"Mode {mode} failed for {t}: {actual} vs {expected}, diff {diff}");
        }
    }

    private void Validate(RenderRequestTestTarget t, int actual, int expected, Camera.RenderRequestMode mode)
    {
        if (actual != expected)
        {
            m_Failed[t] = true;
            Debug.LogError($"Mode {mode} failed for {t}: {actual} vs {expected}");
        }
    }

    private void ValidatePosition(AsyncGPUReadbackRequest readbackPosition)
    {
        var positions = readbackPosition.GetData<float4>();

        foreach (var t in m_Targets)
        {
            float3 viewportSpacePos = t.ViewportSpacePosition(m_Camera);
            var actualPosition = positions[ReadbackDataIndex(viewportSpacePos)].xyz;
            var expectedPosition = t.ExpectedWorldPosition(m_Camera);
            Validate(t, actualPosition, expectedPosition, Camera.RenderRequestMode.WorldPosition);
        }
    }

    private void ValidateNormal(AsyncGPUReadbackRequest readbackNormal)
    {
        var normals = readbackNormal.GetData<float4>();

        foreach (var t in m_Targets)
        {
            float3 viewportSpacePos = t.ViewportSpacePosition(m_Camera);
            var actualNormal = UnpackNormal(normals[ReadbackDataIndex(viewportSpacePos)].xyz);
            var expectedNormal = t.ExpectedWorldNormal(m_Camera);
            Validate(t, actualNormal, expectedNormal, Camera.RenderRequestMode.Normal);
        }
    }

    private void ValidateObjectId(AsyncGPUReadbackRequest readbackObjectId)
    {
        var objectIds = readbackObjectId.GetData<int>();

        foreach (var t in m_Targets)
        {
            float3 viewportSpacePos = t.ViewportSpacePosition(m_Camera);
            var actualId = objectIds[ReadbackDataIndex(viewportSpacePos)];
            var expectedId = t.ExpectedObjectId();
            Validate(t, actualId, expectedId, Camera.RenderRequestMode.ObjectId);
        }
    }

    private void OnDisable()
    {
        Dispose();
    }

    private void Dispose()
    {
        foreach (var rr in m_RenderRequests)
            rr.result.Release();
    }
}
