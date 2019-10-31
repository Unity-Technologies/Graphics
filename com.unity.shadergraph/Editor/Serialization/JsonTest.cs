using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    class ClassB : BaseClass
    {
        public string value;
    }

    [Serializable]
    class ClassA
    {
        public JsonTestRef<ClassB> classB;
    }

    abstract class BaseClass
    {

    }

    struct JsonTestRef<T>
    {
        public string value;
    }

    static class JsonTest
    {
        [MenuItem("JSON/Test")]
        static void Run()
        {
            Debug.Log(EditorJsonUtility.ToJson(new List<JsonTestRef<ClassB>>{new JsonTestRef<ClassB> { value = "hello" }}));

//            var json = EditorJsonUtility.ToJson(new ClassA { classB = new JsonRef<ClassB>("123") });
//            Debug.Log(json);
//            var obj = new ClassA();
//            EditorJsonUtility.FromJsonOverwrite(json, obj);
//            Debug.Log(EditorJsonUtility.ToJson(obj));
        }
    }
}
