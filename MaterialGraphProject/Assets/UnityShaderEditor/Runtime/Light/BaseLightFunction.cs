using System;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class BaseLightFunction
    {
        public const string kNormalSlotName = "Normal";
        public const int NormalSlotId = 1;

        public abstract string lightFunctionName { get; }

        public abstract string surfaceOutputStructureName { get; }

        public virtual void GenerateLightFunctionBody(ShaderGenerator visitor) {}

        public virtual void GenerateLightFunctionName(ShaderGenerator visitor)
        {
            visitor.AddPragmaChunk(lightFunctionName);
        }

        public virtual void GenerateSurfaceOutputStructureName(ShaderGenerator visitor)
        {
            visitor.AddPragmaChunk(surfaceOutputStructureName);
        }

        public abstract void DoSlotsForConfiguration(PixelShaderNode node);

        public virtual int GetFirstPassSlotId()
        {
            return NormalSlotId;
        }
    }
}
