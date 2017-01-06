using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;
using UnityEditor.Experimental.Rendering;

[TestFixture]
public class ShaderGeneratorTests
{
    struct FailureTypes
    {
        // Non-primitive type in nested struct
        internal struct NestedWithNonPrimitiveType
        {
            public struct Data
            {
                public string s;
            }
            public Data contents;
        }

        // Unsupported primitive type in nested struct
        internal struct UnsupportedPrimitiveType
        {
            public struct Data
            {
                public IntPtr intPtr;
            }
            public Data contents;
        }

        // Mixed types in nested struct
        internal struct MixedTypesInNestedStruct
        {
            public struct Data
            {
                public float f;
                public int i;
            }
            public Data contents;
        }

        // More than 4 primitive fields in nested struct
        internal struct TooManyFields
        {
            public struct Data
            {
                public float f1, f2, f3, f4, f5;
            }
            public Data contents;
        }

        // Merge failure due to incompatible types
        internal struct MergeIncompatibleTypes
        {
            public float f;
            public Vector2 v;
            public int i;
        }

        // Merge failure due to register boundary crossing
        internal struct MergeCrossBoundary
        {
            public Vector2 u;
            public Vector3 v;
        }
    }

    // @TODO:  should probably switch to exceptions...
    static bool HasErrorString(List<string> errors, string errorSubstring)
    {
        if (errors == null)
            return false;

        bool foundError = false;
        foreach (var error in errors)
        {
            if (error.IndexOf(errorSubstring) >= 0)
            {
                foundError = true;
                break;
            }
        }

        return foundError;
    }

    [Test(Description = "Disallow non-primitive types in nested structs")]
    public void Fail_NestedWithNonPrimitiveType()
    {
        string source;
        List<string> errors;

        bool success = CSharpToHLSL.GenerateHLSL(typeof(FailureTypes.NestedWithNonPrimitiveType), new GenerateHLSL(PackingRules.Exact), out source, out errors);
        Assert.IsFalse(success);
        Assert.IsTrue(HasErrorString(errors, "contains a non-primitive field type"));
    }

    [Test(Description = "Check for unsupported types in primitive structs")]
    public void Fail_UnsupportedPrimitiveType()
    {
        string source;
        List<string> errors;

        bool success = CSharpToHLSL.GenerateHLSL(typeof(FailureTypes.UnsupportedPrimitiveType), new GenerateHLSL(PackingRules.Exact), out source, out errors);
        Assert.IsFalse(success);
        Assert.IsTrue(HasErrorString(errors, "contains an unsupported field type"));
    }

    [Test(Description = "Disallow mixed types in nested structs")]
    public void Fail_MixedTypesInNestedStruct()
    {
        string source;
        List<string> errors;

        bool success = CSharpToHLSL.GenerateHLSL(typeof(FailureTypes.MixedTypesInNestedStruct), new GenerateHLSL(PackingRules.Exact), out source, out errors);
        Assert.IsFalse(success);
        Assert.IsTrue(HasErrorString(errors, "contains mixed basic types"));
    }

    [Test(Description = "Disallow more than 16 bytes worth of fields in nested structs")]
    public void Fail_TooManyFields()
    {
        string source;
        List<string> errors;

        bool success = CSharpToHLSL.GenerateHLSL(typeof(FailureTypes.TooManyFields), new GenerateHLSL(PackingRules.Exact), out source, out errors);
        Assert.IsFalse(success);
        Assert.IsTrue(HasErrorString(errors, "more than 4 fields"));
    }

    [Test(Description = "Disallow merging incompatible types when doing aggressive packing")]
    public void Fail_MergeIncompatibleTypes()
    {
        string source;
        List<string> errors;

        bool success = CSharpToHLSL.GenerateHLSL(typeof(FailureTypes.MergeIncompatibleTypes), new GenerateHLSL(PackingRules.Aggressive), out source, out errors);
        Assert.IsFalse(success);
        Assert.IsTrue(HasErrorString(errors, "incompatible types"));
    }

    [Test(Description = "Disallow placing fields across register boundaries when merging")]
    public void Fail_MergeCrossBoundary()
    {
        string source;
        List<string> errors;

        bool success = CSharpToHLSL.GenerateHLSL(typeof(FailureTypes.MergeCrossBoundary), new GenerateHLSL(PackingRules.Aggressive), out source, out errors);
        Assert.IsFalse(success);
        Assert.IsTrue(HasErrorString(errors, "cross register boundary"));
    }
}
