#if UNITY_EDITOR

using System;
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
    }
}
#endif