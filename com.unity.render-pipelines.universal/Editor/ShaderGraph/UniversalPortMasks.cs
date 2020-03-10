using UnityEditor.ShaderGraph;
using UnityEditor.Experimental.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalPortMasks
    {
        public static class Vertex
        {
            public static int[] PBR = new int[]
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId,
            };

            public static int[] Unlit = new int[]
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId,
            };

            public static int[] SpriteLit = new int[]
            {
                SpriteLitMasterNode.PositionSlotId,
                SpriteLitMasterNode.VertNormalSlotId,
                SpriteLitMasterNode.VertTangentSlotId,
            };

            public static int[] SpriteUnlit = new int[]
            {
                SpriteUnlitMasterNode.PositionSlotId,
                SpriteUnlitMasterNode.VertNormalSlotId,
                SpriteUnlitMasterNode.VertTangentSlotId
            };
        }

        public static class Pixel
        {
            public static int[] PBR = new int[]
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBRAlphaOnly = new int[]
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBRMeta = new int[]
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBR2D = new int[]
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            };

            public static int[] Unlit = new int[]
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] SpriteLit = new int[]
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.MaskSlotId,
            };

            public static int[] SpriteNormal = new int[]
            {
                SpriteLitMasterNode.ColorSlotId,
                SpriteLitMasterNode.NormalSlotId,
            };

            public static int[] SpriteUnlit = new int[]
            {
                SpriteUnlitMasterNode.ColorSlotId,
            };
        }
    }
}
