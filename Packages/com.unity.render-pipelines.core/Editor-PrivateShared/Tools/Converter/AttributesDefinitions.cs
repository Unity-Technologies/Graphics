using System;
using UnityEngine.Categorization;

namespace UnityEditor.Rendering.Converter
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    internal class PipelineConverterAttribute : CategoryInfoAttribute
    {
        public string source { get; }
        public string destination { get; }

        public PipelineConverterAttribute(string pipelineFrom, string pipelineTo)
        {
            if (string.IsNullOrEmpty(pipelineFrom) || string.IsNullOrEmpty(pipelineTo))
                throw new ArgumentNullException(string.IsNullOrEmpty(pipelineTo) ? nameof(pipelineTo) : nameof(pipelineFrom), "PipelineConverterAttribute parameters cannot be null or empty");

            source = pipelineFrom;
            destination = pipelineTo;
            Name = "Pipeline Converter";
            Order = int.MinValue;
        }
    }

    // Attribute to mark that type/member is a pipeline updater
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    internal class PipelineToolsAttribute : CategoryInfoAttribute
    {
        public PipelineToolsAttribute()
        {
            Name = "Pipeline Tools";
            Order = int.MinValue + 1;
        }
    }
}
