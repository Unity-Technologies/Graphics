using System;
using System.Collections.Generic;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    abstract class VFXSlotObject : VFXSlot
    {
        public override void GetSourceDependentAssets(HashSet<string> dependencies)
        {
            base.GetSourceDependentAssets(dependencies);

            UnityObject obj = (UnityObject)value;

            if( !object.ReferenceEquals(obj,null))
            {
                int instanceID = obj.GetInstanceID();
                dependencies.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(instanceID)));
            }
        }
    }
}
