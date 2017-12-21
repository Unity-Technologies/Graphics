using System;

namespace UnityEngine.Experimental.Rendering
{
    public enum PackingRules
    {
        Exact,
        Aggressive
    };

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum)]
    public class GenerateHLSL : System.Attribute
    {
        public PackingRules packingRules;
        public bool needAccessors; // Whether or not to generate the accessors
        public bool needParamDebug; // // Whether or not to generate define for each field of the struct + debug function (use in HDRenderPipeline)
        public int paramDefinesStart; // Start of the generated define

        public GenerateHLSL(PackingRules rules = PackingRules.Exact, bool needAccessors = true, bool needParamDebug = false, int paramDefinesStart = 1)
        {
            packingRules = rules;
            this.needAccessors = needAccessors;
            this.needParamDebug = needParamDebug;
            this.paramDefinesStart = paramDefinesStart;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SurfaceDataAttributes : System.Attribute
    {
        public string displayName;
        public bool isDirection;
        public bool sRGBDisplay;

        public SurfaceDataAttributes(string displayName = "", bool isDirection = false, bool sRGBDisplay = false)
        {
            this.displayName = displayName;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
        }
    }
}
