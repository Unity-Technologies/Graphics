using System;

namespace UnityEditor.Rendering.Universal
{
    // Might need to change this name before making it public
    internal abstract class RenderPipelineConverter
    {
        /// <summary>
        /// Name of the converter.
        /// </summary>
        public abstract string name { get; }

        /// <summary>
        /// The information when hovering over the converter.
        /// </summary>
        public abstract string info { get; }

        /// <summary>
        /// A check if the converter is enabled or not. Can be used to do a check if prerequisites are met to have it enabled or disabled.
        /// </summary>
        public virtual bool IsEnabled => true;

        /// <summary>
        /// This method getting triggered when clicking the listview item in the UI.
        /// </summary>
        public virtual void OnClicked(int index)
        {
        }

        // This is so that we can have different segment in our UI, example Unity converters, your custom converters etc..
        // This is not implemented yet
        public virtual string category { get; }

        // This is in which drop down item the converter belongs to.
        // Not properly implemented yet
        public abstract Type conversion { get; }

        /// <summary>
        /// This runs when initializing the converter. To gather data for the UI and also for the converter if needed.
        /// </summary>
        /// <param name="context">The context that will be used to initialize data for the converter.</param>
        public abstract void OnInitialize(InitializeConverterContext context);

        /// <summary>
        /// The method that will be run when converting the assets.
        /// </summary>
        /// <param name="context">The context that will be used when executing converter.</param>
        public abstract void OnRun(ref RunItemContext context);
    }
}
