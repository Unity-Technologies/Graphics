#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Rendering
{ 
    public interface IRequiredSetting
    {
        public bool state { get; }
        public string name { get; }
        public string description { get; }

        // Following properties are required to store informations to call editor functions using reflection
        public string editorAssemblyName { get; }
        public string editorClassName { get; }
        public string editorShowFunctionName { get; }
    }
}
#endif