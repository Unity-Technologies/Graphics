
namespace UnityEngine.Experimental.Rendering
{
    public class ShadowUtilsConstants
    {
        // Matches ScriptableShadowsUtility.cpp
        public enum CubemapEdge
        {
            kCubeEdgePX_PY = 0,
            kCubeEdgePX_NY,
            kCubeEdgePX_PZ,
            kCubeEdgePX_NZ,

            kCubeEdgeNX_PY,
            kCubeEdgeNX_NY,
            kCubeEdgeNX_PZ,
            kCubeEdgeNX_NZ,

            kCubeEdgePY_PZ,
            kCubeEdgePY_NZ,
            kCubeEdgeNY_PZ,
            kCubeEdgeNY_NZ,
            kCubeEdge_Count
        };

        public static readonly CubemapEdge[,] kCubemapEdgesPerFace = new CubemapEdge[6,4]
        {
            { CubemapEdge.kCubeEdgePX_PY, CubemapEdge.kCubeEdgePX_NY, CubemapEdge.kCubeEdgePX_PZ, CubemapEdge.kCubeEdgePX_NZ }, // PX
            { CubemapEdge.kCubeEdgeNX_PY, CubemapEdge.kCubeEdgeNX_NY, CubemapEdge.kCubeEdgeNX_PZ, CubemapEdge.kCubeEdgeNX_NZ }, // NX
            { CubemapEdge.kCubeEdgePX_PY, CubemapEdge.kCubeEdgeNX_PY, CubemapEdge.kCubeEdgePY_PZ, CubemapEdge.kCubeEdgePY_NZ }, // PY
            { CubemapEdge.kCubeEdgePX_NY, CubemapEdge.kCubeEdgeNX_NY, CubemapEdge.kCubeEdgeNY_PZ, CubemapEdge.kCubeEdgeNY_NZ }, // NY
            { CubemapEdge.kCubeEdgePX_PZ, CubemapEdge.kCubeEdgeNX_PZ, CubemapEdge.kCubeEdgePY_PZ, CubemapEdge.kCubeEdgeNY_PZ }, // PZ
            { CubemapEdge.kCubeEdgePX_NZ, CubemapEdge.kCubeEdgeNX_NZ, CubemapEdge.kCubeEdgePY_NZ, CubemapEdge.kCubeEdgeNY_NZ }  // NZ
        };

        const float oneOverSqr2 = 0.70710678118654752440084436210485f;
        public static readonly Vector3[] kCubemapEdgeDirections = new Vector3[(int)CubemapEdge.kCubeEdge_Count]
        {
            new Vector3(  oneOverSqr2,  oneOverSqr2,            0 ),
            new Vector3(  oneOverSqr2, -oneOverSqr2,            0 ),
            new Vector3(  oneOverSqr2,            0,  oneOverSqr2 ),
            new Vector3(  oneOverSqr2,            0, -oneOverSqr2 ),

            new Vector3( -oneOverSqr2,  oneOverSqr2,            0 ),
            new Vector3( -oneOverSqr2, -oneOverSqr2,            0 ),
            new Vector3( -oneOverSqr2,            0,  oneOverSqr2 ),
            new Vector3( -oneOverSqr2,            0, -oneOverSqr2 ),

            new Vector3(            0,  oneOverSqr2,  oneOverSqr2 ),
            new Vector3(            0,  oneOverSqr2, -oneOverSqr2 ),
            new Vector3(            0, -oneOverSqr2,  oneOverSqr2 ),
            new Vector3(            0, -oneOverSqr2, -oneOverSqr2 )
        };

        // Cubemap faces with flipped z coordinate.
        // These matrices do NOT match what we have in Skybox.cpp.
        // The C++ runtime flips y as well and requires patching up
        // the culling state. Using these matrices keeps the winding 
        // order, but may need some special treatment if rendering
        // into an actual cubemap.
        public static readonly Matrix4x4[] kCubemapFaces = new Matrix4x4[]
        {
            new Matrix4x4( // pos X
            new Vector4(  0.0f,  0.0f, -1.0f,  0.0f ),
            new Vector4(  0.0f,  1.0f,  0.0f,  0.0f ),
            new Vector4( -1.0f,  0.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f,  0.0f,  1.0f ) ),
            new Matrix4x4( // neg x
            new Vector4(  0.0f,  0.0f,  1.0f,  0.0f ),
            new Vector4(  0.0f,  1.0f,  0.0f,  0.0f ),
            new Vector4(  1.0f,  0.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f,  0.0f,  1.0f ) ),
            new Matrix4x4( // pos y
            new Vector4(  1.0f,  0.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f, -1.0f,  0.0f ),
            new Vector4(  0.0f, -1.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f,  0.0f,  1.0f ) ),
            new Matrix4x4( // neg y
            new Vector4(  1.0f,  0.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f,  1.0f,  0.0f ),
            new Vector4(  0.0f,  1.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f,  0.0f,  1.0f ) ),
            new Matrix4x4( // pos z
            new Vector4(  1.0f,  0.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  1.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f, -1.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f,  0.0f,  1.0f ) ),
            new Matrix4x4( // neg z
            new Vector4( -1.0f,  0.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  1.0f,  0.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f,  1.0f,  0.0f ),
            new Vector4(  0.0f,  0.0f,  0.0f,  1.0f ) )
        };



    }
    public class ShadowUtils
    {
        public static Matrix4x4 ExtractSpotLightMatrix( VisibleLight vl, out Matrix4x4 view, out Matrix4x4 proj, out Vector4 lightDir, out ShadowSplitData splitData )
        {
            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set( 0.0f, 0.0f, 0.0f, float.NegativeInfinity );
            splitData.cullingPlaneCount = 0;
            // get lightDir
            lightDir = vl.light.transform.forward;
            // calculate view
            Matrix4x4 scaleMatrix = Matrix4x4.identity;
            scaleMatrix.m22 = -1.0f;
            view = scaleMatrix * vl.localToWorld.inverse;
            // following code is from SharedLightData::GetNearPlaneMinBound
            float percentageBound = 0.01f * vl.light.range;
            float fixedBound = 0.1f;
            float nearmin = fixedBound <= percentageBound ? fixedBound : percentageBound;
            // calculate projection
            float zfar = vl.range;
            float znear = vl.light.shadowNearPlane >= nearmin ? vl.light.shadowNearPlane : nearmin;
            float fov = vl.spotAngle;
            proj = Matrix4x4.Perspective(fov, 1.0f, znear, zfar);
            // and the compound
            return proj * view;
        }


        public static Matrix4x4 ExtractPointLightMatrix( VisibleLight vl, uint faceIdx, float fovBias, out Matrix4x4 view, out Matrix4x4 proj, out Vector4 lightDir, out ShadowSplitData splitData )
        {
            Debug.Assert( faceIdx <= (uint) CubemapFace.NegativeZ, "Tried to extract cubemap face " + faceIdx + "." );

            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set( 0.0f, 0.0f, 0.0f, float.NegativeInfinity );
            splitData.cullingPlaneCount = 4;
            // get lightDir
            lightDir = vl.light.transform.forward;
            // calculate the view matrices
            Vector3 lpos = vl.light.transform.position;
            view = ShadowUtilsConstants.kCubemapFaces[faceIdx];
            Vector3 inverted_viewpos = ShadowUtilsConstants.kCubemapFaces[faceIdx].MultiplyPoint( -lpos );
            view.SetColumn( 3, new Vector4( inverted_viewpos.x, inverted_viewpos.y, inverted_viewpos.z, 1.0f ) );

            for( int i = 0; i < 4; ++i )
            {
                ShadowUtilsConstants.CubemapEdge cubemapEdge = ShadowUtilsConstants.kCubemapEdgesPerFace[faceIdx,i];
                Vector3 cullingPlaneDirection = ShadowUtilsConstants.kCubemapEdgeDirections[(int)cubemapEdge];
                splitData.SetCullingPlane( i, new Plane( cullingPlaneDirection, lpos ) );
            }
            // following code is from SharedLightData::GetNearPlaneMinBound
            float percentageBound = 0.01f * vl.light.range;
            float fixedBound = 0.1f;
            float nearmin = fixedBound <= percentageBound ? fixedBound : percentageBound;
            // calculate projection
            float farPlane = vl.range;
            float nearPlane = vl.light.shadowNearPlane >= nearmin ? vl.light.shadowNearPlane : nearmin;
            proj = Matrix4x4.Perspective( 90.0f + fovBias, 1.0f, nearPlane, farPlane );
            // and the compound
            return proj * view;
        }

        public static Matrix4x4 ExtractDirectionalLightMatrix( VisibleLight vl, uint cascadeIdx, int cascadeCount, float[] splitRatio, float nearPlaneOffset, uint width, uint height, out Matrix4x4 view, out Matrix4x4 proj, out Vector4 lightDir, out ShadowSplitData splitData, CullResults cullResults, int lightIndex )
        {
            Debug.Assert( width == height, "Currently the cascaded shadow mapping code requires square cascades." );
            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;
            // get lightDir
            lightDir = vl.light.transform.forward;
            // TODO: At some point this logic should be moved to C#, then the parameters cullResults and lightIndex can be removed as well
            //       For directional lights shadow data is extracted from the cullResults, so that needs to be somehow provided here.
            //       Check ScriptableShadowsUtility.cpp ComputeDirectionalShadowMatricesAndCullingPrimitives(...) for details.
            Vector3 ratios = new Vector3();
            for( int i = 0, cnt = splitRatio.Length < 3 ? splitRatio.Length : 3; i < cnt; i++ )
                ratios[i] = splitRatio[i];
            cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives( lightIndex, (int) cascadeIdx, cascadeCount, ratios, (int) width, nearPlaneOffset, out view, out proj, out splitData );
            // and the compound
            return proj * view;
        }

        public static bool MapLightType( LightType lt,  out HDPipeline.GPULightType gputype, out GPUShadowType shadowtype )
        {
            switch( lt )
            {
            case LightType.Spot         : gputype = HDPipeline.GPULightType.Spot;          shadowtype = GPUShadowType.Spot;        return true;
            case LightType.Directional  : gputype = HDPipeline.GPULightType.Directional;   shadowtype = GPUShadowType.Directional; return true;
            case LightType.Point        : gputype = HDPipeline.GPULightType.Point;         shadowtype = GPUShadowType.Point;       return true;
            default:
            case LightType.Area         : gputype = HDPipeline.GPULightType.Rectangle; shadowtype = GPUShadowType.Unknown; return false;   // area lights by themselves can't be mapped to any GPU type
            }
        }
        public static bool MapLightType(LightArchetype la, LightType lt, out GPUShadowType shadowtype)
        {
            switch (la)
            {
                case LightArchetype.Punctual:
                    return MapLightType(lt, out shadowtype);
                case LightArchetype.Area:
                    shadowtype = GPUShadowType.Unknown;
                    return true;
                case LightArchetype.Projector:
                    shadowtype = GPUShadowType.Unknown;
                    return true;
                default:
                    shadowtype = GPUShadowType.Unknown;
                    return false; // <- probably not what you want
            }

        }
        public static bool MapLightType(LightType lt, out GPUShadowType shadowtype)
        {
            switch (lt)
            {
                case LightType.Spot:
                    shadowtype = GPUShadowType.Spot;
                    return true;
                case LightType.Directional:
                    shadowtype = GPUShadowType.Directional;
                    return true;
                case LightType.Point:
                    shadowtype = GPUShadowType.Point;
                    return true;
                default:
                case LightType.Area:
                    shadowtype = GPUShadowType.Unknown;
                    return false;   // area lights by themselves can't be mapped to any GPU type
            }
        }

        public static GPUShadowAlgorithm Pack( ShadowAlgorithm algo, ShadowVariant vari, ShadowPrecision prec )
        {
            int precshift = ShadowConstants.Bits.k_ShadowVariant + ShadowConstants.Bits.k_ShadowAlgorithm;
            int algoshift = ShadowConstants.Bits.k_ShadowVariant;
            return (GPUShadowAlgorithm) ( (int) prec << precshift | ((int) algo << algoshift) | (int)vari);
        }
        public static ShadowAlgorithm ExtractAlgorithm( GPUShadowAlgorithm gpuAlgo ) { return (ShadowAlgorithm) ( ShadowConstants.Masks.k_ShadowAlgorithm & ((int)gpuAlgo >> ShadowConstants.Bits.k_ShadowVariant) ); }
        public static ShadowVariant   ExtractVariant(   GPUShadowAlgorithm gpuAlgo ) { return (ShadowVariant  ) ( ShadowConstants.Masks.k_ShadowVariant   & (int)gpuAlgo                                                     ); }
        public static ShadowPrecision ExtractPrecision( GPUShadowAlgorithm gpuAlgo ) { return (ShadowPrecision) ( ShadowConstants.Masks.k_ShadowPrecision & ((int)gpuAlgo >> (ShadowConstants.Bits.k_ShadowVariant + ShadowConstants.Bits.k_ShadowAlgorithm)) ); }
        public static void Unpack( GPUShadowAlgorithm gpuAlgo, out ShadowAlgorithm algo, out ShadowVariant vari, out ShadowPrecision prec )
        {
            algo = ExtractAlgorithm( gpuAlgo );
            vari = ExtractVariant( gpuAlgo );
            prec = ExtractPrecision( gpuAlgo );
        }
        public static GPUShadowAlgorithm ClearPrecision( GPUShadowAlgorithm gpuAlgo )
        {
            var algo = ExtractAlgorithm( gpuAlgo );
            var vari = ExtractVariant( gpuAlgo );
            return Pack( algo, vari, ShadowPrecision.Low );
        }
        public static float Asfloat( uint val ) { return System.BitConverter.ToSingle( System.BitConverter.GetBytes( val ), 0 ); }
        public static float Asfloat( int val )  { return System.BitConverter.ToSingle( System.BitConverter.GetBytes( val ), 0 ); }
        public static int Asint( float val )    { return System.BitConverter.ToInt32( System.BitConverter.GetBytes( val ), 0 ); }
        public static uint Asuint( float val )  { return System.BitConverter.ToUInt32( System.BitConverter.GetBytes( val ), 0 ); }
    }
} // end of namespace UnityEngine.Experimental.ScriptableRenderLoop
