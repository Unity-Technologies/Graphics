
using System;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class BaseLightFunction
    {
        public const string kNormalSlotName = "Normal";
        public const int NormalSlotId = 1;

        public virtual string lightFunctionName
        {
            get { return ""; }
        }

        public virtual string surfaceOutputStructureName
        {
            get { return ""; }
        }

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
