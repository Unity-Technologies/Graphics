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
        if (!Property.New(PropertyPath.FromUnsafe("test"), typeof(int), out var property, out var error))
        {
            Debug.LogException(error);
            return;
        }
        bool success = gen.Generate(property, out var unused, out error);
    }
}
