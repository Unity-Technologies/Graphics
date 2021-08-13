using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.ShaderSandbox;

namespace ShaderSandbox
{
    internal class WorkflowRegistry
    {
        class ProviderData
        {
            internal int DefaultOrder;
            internal ITemplateProvider Provider;
        }

        class WorkflowSet
        {
            internal Workflow workflow;
            internal List<ProviderData> providers = new List<ProviderData>();

            internal void SortProviders()
            {
                providers.Sort((left, right) => (left.DefaultOrder.CompareTo(right.DefaultOrder)));
            }

            internal IEnumerable<ITemplateProvider> Providers => providers.Select((d) => (d.Provider));
        }

        Dictionary<string, WorkflowSet> workflowMap = new Dictionary<string, WorkflowSet>();

        internal void Register(Workflow workflow)
        {
            workflowMap[workflow.Name] = new WorkflowSet { workflow = workflow };
        }

        // Registers a provider to a workflow with a particular sorting order (controls the order of sub-shaders)
        internal void RegisterProvider(string workflowName, ITemplateProvider provider, int defaultOrder)
        {
            if(workflowMap.TryGetValue(workflowName, out var set))
            {
                set.providers.Add(new ProviderData { DefaultOrder = defaultOrder, Provider = provider });
                set.SortProviders();
            }
        }

        internal Workflow FindWorkflow(string workflowName)
        {
            if (workflowMap.TryGetValue(workflowName, out var set))
                return set.workflow;
            return Workflow.Invalid;
        }

        internal IEnumerable<ITemplateProvider> FindProviders(string workflowName)
        {
            if (workflowMap.TryGetValue(workflowName, out var set))
                return set.Providers;
            return null;
        }
    }
}
