using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    public class CompatibilityTestGraphModel : GraphModel
    {
        public CompatibilityTestGraphModel()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Guid = new SerializableGUID(42, 42);
        }
    }
}
