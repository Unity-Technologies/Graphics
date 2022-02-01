using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class BaseView : VisualElement, IModelView
    {
        /// <inheritdoc />
        public BaseGraphTool GraphTool { get; }

        /// <inheritdoc />
        public virtual ICommandTarget Parent => GraphTool;

        /// <summary>
        /// The dispatcher.
        /// </summary>
        /// <remarks>To dispatch a command, use <see cref="Dispatch"/>. This will ensure the command is also dispatched to parent dispatchers.</remarks>
        public Dispatcher Dispatcher { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseView"/> class.
        /// </summary>
        /// <param name="graphTool">The tool hosting this view.</param>
        protected BaseView(BaseGraphTool graphTool)
        {
            GraphTool = graphTool;
            Dispatcher = new CommandDispatcher();
        }

        /// <inheritdoc />
        public virtual void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            Dispatcher.Dispatch(command, diagnosticsFlags);
            Parent?.Dispatch(command, diagnosticsFlags);
        }
    }
}
