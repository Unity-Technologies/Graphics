using System;
using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
namespace UnityEditor.ShaderGraph.Configuration
{
    public static class CPGraphDataProvider
    {
        public struct CPDataEntryDescriptor
        {
            public string name;
            internal ShaderType type;
        }

        public struct TemplateDataDescriptor
        {
            public string templateName;
            public List<(string customizationPointName, List<CPDataEntryDescriptor> inputs, List<CPDataEntryDescriptor> outputs)> CPIO;
        }

        public static void GatherProviderCPIO(ITargetProvider targetProvider, out List<TemplateDataDescriptor> descriptors)
        {
            descriptors = new List<TemplateDataDescriptor>();
            Target target = targetProvider as Target;
            ITemplateProvider templateProvider = new LegacyTemplateProvider(target, new AssetCollection());

            foreach(var template in templateProvider.GetTemplates(new ShaderContainer()))
            {
                var cpDescs = new List<(string, List<CPDataEntryDescriptor>, List<CPDataEntryDescriptor>)>();
                foreach(var cp in template.CustomizationPoints())
                {
                    var inputs = new List<CPDataEntryDescriptor>();
                    var outputs = new List<CPDataEntryDescriptor>();

                    foreach(var input in cp.Inputs)
                    {
                        inputs.Add(new CPDataEntryDescriptor { name = input.Name, type = input.Type});
                    }

                    foreach(var output in cp.Outputs)
                    {
                        outputs.Add(new CPDataEntryDescriptor { name = output.Name, type = output.Type });
                    }

                    cpDescs.Add((cp.Name, inputs, outputs));
                }
                descriptors.Add(new TemplateDataDescriptor { templateName = template.Name, CPIO = cpDescs });
            }
        }
    }
}
