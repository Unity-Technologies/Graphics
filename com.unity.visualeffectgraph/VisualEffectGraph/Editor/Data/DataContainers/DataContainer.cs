using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Xml;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    public abstract class DataContainer : ScriptableObject
    {
        public abstract DataContainerInfo getContainerInfo();

    }

    public abstract class DataContainerInfo
    {
        public abstract void Serialize(XmlWriter doc);
        public abstract DataContainer CreateDataContainer();
    }

}
