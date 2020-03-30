using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using UnityEngine.Serialization;

namespace UnityEditor.VFX
{
    [Serializable]
    struct VFXEnumValue
    {
        public string name;
        public VFXSerializableObject value;
    }
}
