using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.UIGen;
using static UnityEngine.Rendering.UIGen.UIDefinition;

public class zzzTest : MonoBehaviour
{
    public int test = 32;

    [MenuItem("CONTEXT/zzzTest/Test Gen XML")]
    static void TestGenXML()
    {
        var gen = new IntUIPropertyGenerator();
        var property = new Property() { propertyPath = "test" };
        bool success = gen.Generate(property, out var unused, out var error);
    }
}
