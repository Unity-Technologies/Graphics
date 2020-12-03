using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Packing Rules for structs.
    /// </summary>
    public enum PackingRules
    {
        /// <summary>
        /// Exact Packing
        /// </summary>
        Exact,
        /// <summary>
        /// Aggressive Packing
        /// </summary>
        Aggressive
    };

    /// <summary>
    /// Field packing scheme.
    /// </summary>
    public enum FieldPacking
    {
        /// <summary>
        /// No Packing
        /// </summary>
        NoPacking = 0,
        /// <summary>
        /// R11G11B10 Packing
        /// </summary>
        R11G11B10,
        /// <summary>
        /// Packed Float
        /// </summary>
        PackedFloat,
        /// <summary>
        /// Packed UInt
        /// </summary>
        PackedUint
    }

    /// <summary>
    /// Field Precision
    /// </summary>
    public enum FieldPrecision
    {
        /// <summary>
        /// Half Precision
        /// </summary>
        Half,
        /// <summary>
        /// Real Precision
        /// </summary>
        Real,
        /// <summary>
        /// Default Precision
        /// </summary>
        Default
    }

    /// <summary>
    /// Attribute specifying that HLSL code should be generated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum)]
    public class GenerateHLSL : System.Attribute
    {
        /// <summary>
        /// Packing rules for the struct.
        /// </summary>
        public PackingRules packingRules;
        /// <summary>
        /// Structure contains packed fields.
        /// </summary>
        public bool containsPackedFields;
        /// <summary>
        /// Structure needs generated accessors.
        /// </summary>
        public bool needAccessors;
        /// <summary>
        /// Structure needs generated setters.
        /// </summary>
        public bool needSetters;
        /// <summary>
        /// Structure needs generated debug defines and functions.
        /// </summary>
        public bool needParamDebug;
        /// <summary>
        /// Start value of generated defines.
        /// </summary>
        public int paramDefinesStart;
        /// <summary>
        /// Generate structure declaration or not.
        /// </summary>
        public bool omitStructDeclaration;
        /// <summary>
        /// Generate constant buffer declaration or not.
        /// </summary>
        public bool generateCBuffer;
        /// <summary>
        /// If specified, when generating a constant buffer, use this explicit register.
        /// </summary>
        public int constantRegister;
        /// <summary>
        /// Path of the generated file
        /// </summary>
        public string sourcePath;

        /// <summary>
        /// GenerateHLSL attribute constructor.
        /// </summary>
        /// <param name="rules">Packing rules.</param>
        /// <param name="needAccessors">Need accessors.</param>
        /// <param name="needSetters">Need setters.</param>
        /// <param name="needParamDebug">Need debug defines.</param>
        /// <param name="paramDefinesStart">Start value of debug defines.</param>
        /// <param name="omitStructDeclaration">Omit structure declaration.</param>
        /// <param name="containsPackedFields">Contains packed fields.</param>
        /// <param name="generateCBuffer">Generate a constant buffer.</param>
        /// <param name="constantRegister">When generating a constant buffer, specify the optional constant register.</param>
        /// <param name="sourcePath">Location of the source file defining the C# type. (Automatically filled by compiler)</param>
        public GenerateHLSL(PackingRules rules = PackingRules.Exact, bool needAccessors = true, bool needSetters = false, bool needParamDebug = false, int paramDefinesStart = 1,
                            bool omitStructDeclaration = false, bool containsPackedFields = false, bool generateCBuffer = false, int constantRegister = -1,
                            [CallerFilePath] string sourcePath = null)
        {
            this.sourcePath = sourcePath;
            packingRules = rules;
            this.needAccessors = needAccessors;
            this.needSetters = needSetters;
            this.needParamDebug = needParamDebug;
            this.paramDefinesStart = paramDefinesStart;
            this.omitStructDeclaration = omitStructDeclaration;
            this.containsPackedFields = containsPackedFields;
            this.generateCBuffer = generateCBuffer;
            this.constantRegister = constantRegister;
        }
    }

    /// <summary>
    /// Attribute specifying the parameters of a surface data field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SurfaceDataAttributes : System.Attribute
    {
        /// <summary>
        /// Display names overrides for the field.
        /// </summary>
        public string[] displayNames;
        /// <summary>
        /// True if the field is a direction.
        /// </summary>
        public bool isDirection;
        /// <summary>
        /// True if the field is an sRGB value.
        /// </summary>
        public bool sRGBDisplay;
        /// <summary>
        /// Field precision.
        /// </summary>
        public FieldPrecision precision;
        /// <summary>
        /// Field is a normalized vector.
        /// </summary>
        public bool checkIsNormalized;

        /// <summary>
        /// SurfaceDataAttributes constructor.
        /// </summary>
        /// <param name="displayName">Display name.</param>
        /// <param name="isDirection">Field is a direction.</param>
        /// <param name="sRGBDisplay">Field is an sRGB value.</param>
        /// <param name="precision">Field precision.</param>
        public SurfaceDataAttributes(string displayName = "", bool isDirection = false, bool sRGBDisplay = false, FieldPrecision precision = FieldPrecision.Default, bool checkIsNormalized = false)
        {
            displayNames = new string[1];
            displayNames[0] = displayName;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
            this.precision = precision;
            this.checkIsNormalized = checkIsNormalized;
        }

        // We allow users to add several names for one field, so user can override the auto behavior and do something else with the same data
        // typical example is normal that you want to draw in view space or world space. So user can override view space case and do the transform.
        /// <summary>
        /// SurfaceDataAttributes constructor.
        /// </summary>
        /// <param name="displayNames">List of names for the field.</param>
        /// <param name="isDirection">Field is a direction.</param>
        /// <param name="sRGBDisplay">Field is an sRGB value.</param>
        /// <param name="precision">Field precision.</param>
        public SurfaceDataAttributes(string[] displayNames, bool isDirection = false, bool sRGBDisplay = false, bool checkIsNormalized = false, FieldPrecision precision = FieldPrecision.Default)
        {
            this.displayNames = displayNames;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
            this.precision = precision;
            this.checkIsNormalized = checkIsNormalized;
        }
    }

    /// <summary>
    /// Attribute defining an HLSL array.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HLSLArray : System.Attribute
    {
        /// <summary>
        /// Size of the array.
        /// </summary>
        public int arraySize;
        /// <summary>
        /// Type of the array elements.
        /// </summary>
        public Type elementType;

        /// <summary>
        /// HLSLSArray constructor.
        /// </summary>
        /// <param name="arraySize">Size of the array.</param>
        /// <param name="elementType">Type of the array elements.</param>
        public HLSLArray(int arraySize, Type elementType)
        {
            this.arraySize = arraySize;
            this.elementType = elementType;
        }
    }

    /// <summary>
    /// Attribute defining packing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class PackingAttribute : System.Attribute
    {
        /// <summary>
        /// Display names.
        /// </summary>
        public string[] displayNames;
        /// <summary>
        /// Minimum and Maximum value.
        /// </summary>
        public float[] range;
        /// <summary>
        /// Packing scheme.
        /// </summary>
        public FieldPacking packingScheme;
        /// <summary>
        /// Offset in source.
        /// </summary>
        public int offsetInSource;
        /// <summary>
        /// Size in bits.
        /// </summary>
        public int sizeInBits;
        /// <summary>
        /// True if the field is a direction.
        /// </summary>
        public bool isDirection;
        /// <summary>
        /// True if the field is an sRGB value.
        /// </summary>
        public bool sRGBDisplay;
        /// <summary>
        /// True if the field is an sRGB value.
        /// </summary>
        public bool checkIsNormalized;

        /// <summary>
        /// Packing Attribute constructor.
        /// </summary>
        /// <param name="displayNames">Display names.</param>
        /// <param name="packingScheme">Packing scheme.</param>
        /// <param name="bitSize">Size in bits.</param>
        /// <param name="offsetInSource">Offset in source.</param>
        /// <param name="minValue">Minimum value.</param>
        /// <param name="maxValue">Maximum value.</param>
        /// <param name="isDirection">Field is a direction.</param>
        /// <param name="sRGBDisplay">Field is an sRGB value.</param>
        public PackingAttribute(string[] displayNames, FieldPacking packingScheme = FieldPacking.NoPacking, int bitSize = 32, int offsetInSource = 0, float minValue = 0.0f, float maxValue = 1.0f, bool isDirection = false, bool sRGBDisplay = false, bool checkIsNormalized = false)
        {
            this.displayNames = displayNames;
            this.packingScheme = packingScheme;
            this.offsetInSource = offsetInSource;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
            this.checkIsNormalized = checkIsNormalized;
            this.sizeInBits = bitSize;
            this.range = new float[] { minValue, maxValue };
        }

        /// <summary>
        /// Packing Attribute constructor.
        /// </summary>
        /// <param name="displayName">Display name.</param>
        /// <param name="packingScheme">Packing scheme.</param>
        /// <param name="bitSize">Size in bits.</param>
        /// <param name="offsetInSource">Offset in source.</param>
        /// <param name="minValue">Minimum value.</param>
        /// <param name="maxValue">Maximum value.</param>
        /// <param name="isDirection">Field is a direction.</param>
        /// <param name="sRGBDisplay">Field is an sRGB value.</param>
        public PackingAttribute(string displayName = "", FieldPacking packingScheme = FieldPacking.NoPacking, int bitSize = 0, int offsetInSource = 0, float minValue = 0.0f, float maxValue = 1.0f, bool isDirection = false, bool sRGBDisplay = false, bool checkIsNormalized = false)
        {
            displayNames = new string[1];
            displayNames[0] = displayName;
            this.packingScheme = packingScheme;
            this.offsetInSource = offsetInSource;
            this.isDirection = isDirection;
            this.sRGBDisplay = sRGBDisplay;
            this.checkIsNormalized = checkIsNormalized;
            this.sizeInBits = bitSize;
            this.range = new float[] { minValue, maxValue };
        }
    }

    /// <summary>
    /// This type needs to be used when generating unsigned integer arrays for constant buffers.
    /// </summary>
    public struct ShaderGenUInt4
    {
    }
}
