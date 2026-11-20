using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Sets URP's global shader variables the way URP does, for tests drawing outside the render pipeline.
public sealed class UniversalGlobalShaderVariablesForTests : IDisposable
{
    readonly GlobalShaderVariablesUploader m_Uploader =
        new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.Base | GlobalShaderVariablesGroup.Only3D);

    public UniversalGlobalShaderVariablesForTests()
    {
        // A test can run before the first frame of the domain.
        UniversalRenderPipeline.RefreshGlobalConstantBufferFlag();
        m_Uploader.useConstantBuffer = UniversalRenderPipeline.useGlobalConstantBuffer;
        m_Uploader.BeginFill();
    }

    public Vector4 screenParams { set => m_Uploader._ScreenParams = value; }
    public Vector4 scaledScreenParams { set => m_Uploader._ScaledScreenParams = value; }
    public Vector4 screenSize { set => m_Uploader._ScreenSize = value; }
    public Vector4 zBufferParams { set => m_Uploader._ZBufferParams = value; }
    public Vector4 rtHandleScale { set => m_Uploader._RTHandleScale = value; }

    public Vector4 mainLightShadowmapSize { set => m_Uploader._MainLightShadowmapSize = value; }
    public Vector4 mainLightShadowOffset0 { set => m_Uploader._MainLightShadowOffset0 = value; }
    public Vector4 mainLightShadowOffset1 { set => m_Uploader._MainLightShadowOffset1 = value; }
    public Vector4 cascadeShadowSplitSpheres0 { set => m_Uploader._CascadeShadowSplitSpheres0 = value; }
    public Vector4 cascadeShadowSplitSpheres1 { set => m_Uploader._CascadeShadowSplitSpheres1 = value; }
    public Vector4 cascadeShadowSplitSpheres2 { set => m_Uploader._CascadeShadowSplitSpheres2 = value; }
    public Vector4 cascadeShadowSplitSpheres3 { set => m_Uploader._CascadeShadowSplitSpheres3 = value; }
    public Vector4 cascadeShadowSplitSphereRadii { set => m_Uploader._CascadeShadowSplitSphereRadii = value; }

    public void SetMainLightWorldToShadow(Matrix4x4[] matrices)
    {
        if (matrices == null || matrices.Length != MainLightShadowMatrices.count)
            throw new ArgumentException($"Expected {MainLightShadowMatrices.count} matrices.", nameof(matrices));

        var shadowMatrices = new MainLightShadowMatrices
        {
            cascade0 = matrices[0],
            cascade1 = matrices[1],
            cascade2 = matrices[2],
            cascade3 = matrices[3],
            cascade4 = matrices[4],
        };
        m_Uploader.SetMainLightWorldToShadow(in shadowMatrices);
    }

    public Vector4 mainLightPCSSScalars { set => m_Uploader._MainLightPCSSScalars = value; }

    public void SetMainLightPCSSParams0(Vector4[] vectors)
    {
        var cascadeVectors = ToCascadeVectors(vectors);
        m_Uploader.SetMainLightPCSSParams0(in cascadeVectors);
    }

    public void SetMainLightPCSSParams1(Vector4[] vectors)
    {
        var cascadeVectors = ToCascadeVectors(vectors);
        m_Uploader.SetMainLightPCSSParams1(in cascadeVectors);
    }

    public void SetMainLightPCSSAtlas(Vector4[] vectors)
    {
        var cascadeVectors = ToCascadeVectors(vectors);
        m_Uploader.SetMainLightPCSSAtlas(in cascadeVectors);
    }

    static MainLightShadowCascadeVectors ToCascadeVectors(Vector4[] vectors)
    {
        if (vectors == null || vectors.Length != MainLightShadowCascadeVectors.count)
            throw new ArgumentException($"Expected {MainLightShadowCascadeVectors.count} vectors.", nameof(vectors));

        return new MainLightShadowCascadeVectors
        {
            cascade0 = vectors[0],
            cascade1 = vectors[1],
            cascade2 = vectors[2],
            cascade3 = vectors[3],
            cascade4 = vectors[4],
        };
    }

    public void Push(CommandBuffer cmd)
    {
        m_Uploader.EndFill();
        m_Uploader.PushDefault(CommandBufferHelpers.GetUnsafeCommandBuffer(cmd));
        m_Uploader.BeginFill();
    }

    public void Dispose()
    {
        m_Uploader.Release();
    }
}
