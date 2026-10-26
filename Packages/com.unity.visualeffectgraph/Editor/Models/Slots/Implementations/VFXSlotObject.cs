using System;
using System.Collections.Generic;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    abstract class VFXSlotObject : VFXSlot
    {
        public override void GetSourceDependentAssets(HashSet<GUID> dependencies)
        {
            base.GetSourceDependentAssets(dependencies);

            UnityObject obj = (UnityObject)value;

            if (!object.ReferenceEquals(obj, null))
            {
                var entityId = obj.GetEntityId();
                var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(entityId));
                if (!guid.Empty())
                    dependencies.Add(guid);
            }
        }
    }
}
