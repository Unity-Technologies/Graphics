using System;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    class FakeScriptableObject<T>
    {
        public T MonoBehaviour;

        public FakeScriptableObject() {}

        public FakeScriptableObject(T value)
        {
            MonoBehaviour = value;
        }
    }
}
