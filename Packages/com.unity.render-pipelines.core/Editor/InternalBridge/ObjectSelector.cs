using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnityEditor.Rendering
{
    static class ObjectSelector
    {
        public static void Show(UnityEngine.Object obj, Type requiredType, UnityEngine.Object objectBeingEdited, bool allowSceneObjects, List<EntityId> allowedEntityIds = null, Action<UnityEngine.Object> onObjectSelectorClosed = null, Action<UnityEngine.Object> onObjectSelectedUpdated = null, bool showNoneItem = true)
        {
            UnityEditor.ObjectSelector.get.Show(obj, requiredType, objectBeingEdited, allowSceneObjects, allowedEntityIds, onObjectSelectorClosed, onObjectSelectedUpdated, showNoneItem);
        }
    }
}
