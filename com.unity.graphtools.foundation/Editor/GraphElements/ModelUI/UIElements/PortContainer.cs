using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A VisualElement used as a container for <see cref="Port"/>s.
    /// </summary>
    public class PortContainer : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<PortContainer> {}

        public static readonly string ussClassName = "ge-port-container";
        public static readonly string portCountClassNamePrefix = ussClassName.WithUssModifier("port-count-");

        /// <summary>
        /// Initializes a new instance of the <see cref="PortContainer"/> class.
        /// </summary>
        public PortContainer()
        {
            AddToClassList(ussClassName);
            this.AddStylesheet("PortContainer.uss");
        }

        public void UpdatePorts(IEnumerable<IPortModel> ports, IModelView view)
        {
            var uiPorts = this.Query<Port>().ToList();
            var portViewModels = ports?.ToList() ?? new List<IPortModel>();

            // Check if we should rebuild ports
            bool rebuildPorts = false;
            if (uiPorts.Count != portViewModels.Count)
            {
                rebuildPorts = true;
            }
            else
            {
                int i = 0;
                foreach (var portModel in portViewModels)
                {
                    if (!Equals(uiPorts[i].PortModel, portModel))
                    {
                        rebuildPorts = true;
                        break;
                    }

                    i++;
                }
            }

            if (rebuildPorts)
            {
                foreach (var visualElement in Children())
                {
                    if (visualElement is Port port)
                    {
                        port.RemoveFromView();
                    }
                }

                Clear();

                foreach (var portModel in portViewModels)
                {
                    var ui = GraphElementFactory.CreateUI<Port>(view, portModel);
                    Debug.Assert(ui != null, "GraphElementFactory does not know how to create UI for " + portModel.GetType());
                    Add(ui);

                    ui.AddToView(view);
                }
            }
            else
            {
                foreach (var port in uiPorts)
                {
                    port.UpdateFromModel();
                }
            }

            this.PrefixEnableInClassList(portCountClassNamePrefix, portViewModels.Count.ToString());
        }
    }
}
