using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // Note: There is another SurfaceType in ShaderGraph (AlphaMode.cs) which conflicts in HDRP shader graph files
    public enum SurfaceType
    {
        Opaque,
        Transparent
    }

    // Enum values are hardcoded for retro-compatibility. Don't change them.
    public enum BlendMode
    {
        // Note: value is due to code change, don't change the value
        Alpha = 0,
        Premultiply = 4,
        Additive = 1
    }

    public enum DisplacementMode
    {
        None,
        Vertex,
        Pixel,
        Tessellation
    }

    public enum DoubleSidedNormalMode
    {
        Flip,
        Mirror,
        None
    }

    public enum TessellationMode
    {
        None,
        Phong
    }

    public enum MaterialId
    {
        LitSSS = 0,
        LitStandard = 1,
        LitAniso = 2,
        LitIridescence = 3,
        LitSpecular = 4,
        LitTranslucent = 5
    };

    public enum NormalMapSpace
    {
        TangentSpace,
        ObjectSpace,
    }

    public enum HeightmapMode
    {
        Parallax,
        Displacement,
    }

    public enum VertexColorMode
    {
        None,
        Multiply,
        Add
    }

    internal enum UVDetailMapping
    {
        UV0,
        UV1,
        UV2,
        UV3
    }
    
    internal enum UVBaseMapping
    {
        UV0,
        UV1,
        UV2,
        UV3,
        Planar,
        Triplanar
    }

    internal enum HeightmapParametrization
    {
        MinMax = 0,
        Amplitude = 1
    }

    internal enum TransparentCullMode
    {
        // Off is double sided and a different setting so we don't have it here
        Back = CullMode.Back,
        Front = CullMode.Front,
    }

    public static class MaterialExtension
    {
        public static SurfaceType   GetSurfaceType(this Material material)
        {
            return material.HasProperty(kSurfaceType) ? (SurfaceType)material.GetFloat(kSurfaceType) : SurfaceType.Opaque;
        }

        public static MaterialId    GetMaterialId(this Material material)
        {
            return material.HasProperty(kMaterialID) ? (MaterialId)material.GetFloat(kMaterialID) : MaterialId.LitStandard;
        }

        public static BlendMode     GetBlendMode(this Material material)
        {
            return material.HasProperty(kBlendMode) ? (BlendMode)material.GetFloat(kBlendMode) : BlendMode.Additive;
        }

        public static int           GetLayerCount(this Material material)
        {
            return material.HasProperty(kLayerCount) ? material.GetInt(kLayerCount) : 1;
        }

        public static bool          GetZWrite(this Material material)
        {
            return material.HasProperty(kZWrite) ? material.GetInt(kZWrite) == 1 : false;
        }

        public static CullMode      GetTransparentCullMode(this Material material)
        {
            return material.HasProperty(kTransparentCullMode) ? (CullMode)material.GetInt(kTransparentCullMode) : CullMode.Back;
        }

        public static CompareFunction   GetTransparentZTest(this Material material)
        {
            return material.HasProperty(kZTestTransparent) ? (CompareFunction)material.GetInt(kZTestTransparent) : CompareFunction.LessEqual;
        }
    }
}