using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    class RenderPipelineConverterManager : ScriptableSingleton<RenderPipelineConverterManager>, ISerializationCallbackReceiver
    {
        List<IRenderPipelineConverter> m_RenderPipelineConverters = new List<IRenderPipelineConverter>();

        public List<IRenderPipelineConverter> renderPipelineConverters => m_RenderPipelineConverters;

        [SerializeField] SerializedDictionary<string, ConverterState> m_RenderPipelineConvertersStates = new();

        public ConverterState GetConverterState(IRenderPipelineConverter renderPipelineConverter)
        {
            if (!m_RenderPipelineConvertersStates.TryGetValue(renderPipelineConverter.GetType().AssemblyQualifiedName, out var state))
                throw new KeyNotFoundException($"Unable to find state for {renderPipelineConverter.GetType()}");

            return state;
        }

        public RenderPipelineConverterManager()
        {
            ReloadConverters();
        }

        private void ReloadConverters() 
        { 
            m_RenderPipelineConverters.Clear();
            m_RenderPipelineConvertersStates.Clear();
            foreach (var converterType in TypeCache.GetTypesDerivedFrom<IRenderPipelineConverter>())
            {
                if (converterType.IsAbstract || converterType.IsInterface)
                    continue;

                var renderPipelineConverter = Activator.CreateInstance(converterType) as RenderPipelineConverter;
                m_RenderPipelineConverters.Add(renderPipelineConverter);

                // Create a new ConvertState which holds the active state of the converter
                var converterState = new ConverterState
                {
                    isActive = false,
                    isInitialized = false,
                    items = new List<ConverterItemState>(),
                };
                m_RenderPipelineConvertersStates.Add(renderPipelineConverter.GetType().AssemblyQualifiedName, converterState);
            }
        }

        internal void Reset()
        {
            ReloadConverters();
        }

        public void OnBeforeSerialize()
        {
            // TODO: As the converters have data stored inside during initialization we need to clear the states
            // Once we keep the items to be classes and converters can inherit them to store any kind of data this can be removed
            foreach (var kvp in m_RenderPipelineConvertersStates)
            {
                var state = kvp.Value;
                state.isInitialized = false;
                state.items.Clear();
                state.pending = 0;
                state.warnings = 0;
                state.errors = 0;
                state.success = 0;
            }
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
