using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXBlockController : VFXContextSlotContainerController
    {
        public VFXBlockController(VFXBlock model, VFXContextController contextController) : base(model, contextController)
        {
        }

        public VFXBlock block
        {
            get { return model as VFXBlock; }
        }

        public int index
        {
            get { return contextController.FindBlockIndexOf(this); }
        }

        bool ShouldIgnoreMember(Type type, FieldInfo field)
        {
            return typeof(ISpaceable).IsAssignableFrom(type) && field.Name == "space";
        }
    }
}
