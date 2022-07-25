using System;
using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
namespace UnityEditor.ShaderGraph.Configuration
{
    public static class CPGraphDataProvider
    {
        public class CPEntryDescriptor
        {
            public string name;
            internal ShaderType type;
        }

        public class CPDataDescriptor
        {
            public string customizationPointName;
            public List<CPEntryDescriptor> inputs;
            public List<CPEntryDescriptor> outputs;
        }

        public class TemplateDataDescriptor
        {
            public string templateName;
            public List<CPDataDescriptor> CPIO;
        }

        public static void GatherProviderCPIO(ITargetProvider targetProvider, out List<TemplateDataDescriptor> descriptors)
        {
            descriptors = new List<TemplateDataDescriptor>();
            Target target = targetProvider as Target;
            ITemplateProvider templateProvider = new LegacyTemplateProvider(target, new AssetCollection());

            foreach(var template in templateProvider.GetTemplates(new ShaderContainer()))
            {
                var cpDescs = new List<CPDataDescriptor>();
                foreach(var cp in template.CustomizationPoints())
                {
                    var inputs = new List<CPEntryDescriptor>();
                    var outputs = new List<CPEntryDescriptor>();

                    foreach(var input in cp.Inputs)
                    {
                        inputs.Add(new CPEntryDescriptor { name = input.Name, type = input.Type});
                    }

                    foreach(var output in cp.Outputs)
                    {
                        outputs.Add(new CPEntryDescriptor { name = output.Name, type = output.Type });
                    }

                    cpDescs.Add(new CPDataDescriptor() { customizationPointName = cp.Name, inputs = inputs, outputs = outputs });
                }
                descriptors.Add(new TemplateDataDescriptor { templateName = template.Name, CPIO = cpDescs });
            }
        }
    }
}
