using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct ShaderGraphTemplate
    {
        public string name;
        public string category;
        [Multiline]
        public string description;
        public Texture2D icon;
        public Texture2D thumbnail;
    }
}
