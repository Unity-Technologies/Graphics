using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public enum PackingRules
    {
        Exact,
        Aggressive
    };

    public enum FieldPacking
    {
        NoPacking = 0,
        R11G11B10,
        PackedFloat,
        PackedUint
    }

    public enum FieldPrecision
    {
        Half,
        Real,
        Default
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum)]
    public class GenerateHLSL : System.Attribute
    {
        public PackingRules packingRules;
        public bool containsPackedFields;
        public bool needAccessors; // Whether or not to generate the accessors
        public bool needSetters; // Whether or not to generate setters
        public bool needParamDebug; // // Whether or not to generate define for each field of the struct + debug function (use in HDRenderPipeline)
        public int paramDefinesStart; // Start of the generated define
        public bool omitStructDeclaration; // Whether to skip "struct <name> {" etc

        public GenerateHLSL(PackingRules rules = PackingRules.Exact, bool needAccessors = true, bool needSetters = false, bool needParamDebug = false, int paramDefinesStart = 1, bool omitStructDeclaration = false, bool containsPackedFields = false)
        {
            packingRules = rules;
            this.needAccessors = needAccessors;
            this.needSetters = needSetters;
            this.needParamDebug = needParamDebug;
            this.paramDefinesStart = paramDefinesStart;
            this.omitStructDeclaration = omitStructDeclaration;
            this.containsPackedFields = containsPackedFields;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SurfaceDataAttributes : System.Attribute
    {
        public string[] displayNames;
        public bool isDirection;
        public bool sRGBDisplay;
        public FieldPrecision precision;

        public SurfaceDataAttributes(string displayName = "", bool isDirection = false, bool sRGBDisplay = false, FieldPrecision precision = FieldPrecision.Default)
        {
            displayNames = new string[1];
            displayNames[0] = displayName;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
            this.precision = precision;
        }

        // We allow users to add several names for one field, so user can override the auto behavior and do something else with the same data
        // typical example is normal that you want to draw in view space or world space. So user can override view space case and do the transform.
        public SurfaceDataAttributes(string[] displayName, bool isDirection = false, bool sRGBDisplay = false, FieldPrecision precision = FieldPrecision.Default)
        {
            displayNames = displayName;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
            this.precision = precision;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class HLSLArray : System.Attribute
    {
        public int  arraySize;
        public Type elementType;

        public HLSLArray(int arraySize, Type elementType)
        {
            this.arraySize = arraySize;
            this.elementType = elementType;
        }
    }


    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class PackingAttribute : System.Attribute
    {
        public string[] displayNames;
        public float[] range;
        public FieldPacking packingScheme;
        public int offsetInSource;
        public int sizeInBits;
        public bool isDirection;
        public bool sRGBDisplay;

        public PackingAttribute(string[] displayName, FieldPacking packingScheme = FieldPacking.NoPacking, int bitSize = 32, int offsetInSource = 0, float minValue = 0.0f, float maxValue = 1.0f, bool isDirection = false, bool sRGBDisplay = false)
        {
            displayNames = displayName;
            this.packingScheme = packingScheme;
            this.offsetInSource = offsetInSource;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
            this.sizeInBits = bitSize;
            this.range = new float[] { minValue, maxValue };
        }

        public PackingAttribute(string displayName = "", FieldPacking packingScheme = FieldPacking.NoPacking, int bitSize = 0, int offsetInSource = 0, float minValue = 0.0f, float maxValue = 1.0f, bool isDirection = false, bool sRGBDisplay = false)
        {
            displayNames = new string[1];
            displayNames[0] = displayName;
            this.packingScheme = packingScheme;
            this.offsetInSource = offsetInSource;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
            this.sizeInBits = bitSize;
            this.range = new float[] { minValue, maxValue };
        }
    }


}
