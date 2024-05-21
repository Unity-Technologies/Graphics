using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(name = "Output Event", category = "#1Event")]
    class VFXOutputEvent : VFXContext
    {
        [VFXSetting, SerializeField, Delayed]
        protected string eventName = "On Received Event";

        public VFXOutputEvent() : base(VFXContextType.OutputEvent, VFXDataType.SpawnEvent, VFXDataType.OutputEvent)
        {
        }

        public override bool CanBeCompiled()
        {
            var anyInputContextPlugged = inputContexts.Any();
            return anyInputContextPlugged;
        }

        private static void CollectParentsContextRecursively(VFXContext start, HashSet<VFXContext> parents)
        {
            if (parents.Contains(start))
                return;
            parents.Add(start);
            foreach (var parent in start.inputContexts)
                CollectParentsContextRecursively(parent, parents);
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                var parents = new HashSet<VFXContext>();
                CollectParentsContextRecursively(this, parents);

                var attributeManager = GetGraph().attributesManager;
                //Detect all attribute used in source spawner & consider as read source from them
                //This can be done using VFXDataSpawner after read attribute from spawn feature merge (require to be sure that the order of compilation is respected)
                foreach (var block in parents.SelectMany(o => o.children).OfType<VFX.Block.VFXSpawnerSetAttribute>())
                {
                    var attributeName = block.GetSetting("attribute");
                    if (attributeManager.TryFind((string)attributeName.value, out var attribute))
                    {
                        yield return new VFXAttributeInfo(attribute, VFXAttributeMode.ReadSource);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not find attribute {attributeName}");
                    }
                }
            }
        }

        protected override int outputFlowCount => 0;
        public override string name => "Output Event";
    }
}
