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

        System.Exception error;
        if (!Property.New(PropertyPath.FromUnsafe("test"), typeof(int), out var property, out error))
        {
            Debug.LogException(error);
            return;
        }
        if (!property.AddFeature(new DisplayName(UIDefinition.PropertyName.FromUnsafe("Nicer Name To Use")), out error))
        {
            Debug.LogException(error);
            return;
        }
        if (!property.AddFeature(new Min<int>(-10), out error))
        {
            Debug.LogException(error);
            return;
        }
        if (!property.AddFeature(new Max<int>(30), out error))
        {
            Debug.LogException(error);
            return;
        }
        if (!property.AddFeature(new DisplayAsSlider(), out error))
        {
            Debug.LogException(error);
            return;
        }

        bool success = gen.Generate(property, out var unused, out error);
    }
}
