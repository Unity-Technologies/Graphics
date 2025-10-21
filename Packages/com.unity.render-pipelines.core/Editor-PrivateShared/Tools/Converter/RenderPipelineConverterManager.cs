using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Rendering.Converter
{
    class RenderPipelineConverterManager : ScriptableSingleton<RenderPipelineConverterManager>
        , ISerializationCallbackReceiver
    {
        [field:SerializeField]
        public List<ConverterState> converterStates { get; private set; } = new();

        public RenderPipelineConverterManager()
        {
            ReloadConverters();
        }

        private void ReloadConverters() 
        {
            using(UnityEngine.Pool.HashSetPool<Type>.Get(out var availableConverterTypes))
            {
                foreach (var converterType in TypeCache.GetTypesDerivedFrom<IRenderPipelineConverter>())
                {
                    if (converterType.IsAbstract || converterType.IsInterface)
                        continue;

                    var obsoleteAtt = converterType.GetCustomAttribute<ObsoleteAttribute>();
                    if (obsoleteAtt != null && obsoleteAtt.IsError == false)
                    {
                        // Skip obsolete converters that are soft deprecated
                        continue;
                    }

                    availableConverterTypes.Add(converterType);

                    var serializedConverter = converterStates.Find(i => i.converter.GetType() == converterType);

                    if (serializedConverter != null)
                        continue;

                    var renderPipelineConverter = Activator.CreateInstance(converterType) as IRenderPipelineConverter;

                    // Create a new ConvertState which holds the active state of the converter
                    var converterState = new ConverterState
                    {
                        isSelected = false,
                        isInitialized = false,
                        items = new List<ConverterItemState>(),
                        converter = renderPipelineConverter
                    };
                    converterStates.Add(converterState);
                }

                foreach(var converter in converterStates)
                {
                    if (!availableConverterTypes.Contains(converter.converter.GetType()))
                    {
                        Debug.Log($"Removing converter state {converter.converter.GetType()} as it is no longer available or deprecated.");
                        converterStates.Remove(converter);
                    }
                }
            }
        }

        internal void Reset()
        {
            converterStates.Clear();
            ReloadConverters();
        }

        public void OnBeforeSerialize()
        {
            // TODO: As the converters have data stored inside during initialization we need to clear the states
            // Once we keep the items to be classes and converters can inherit them to store any kind of data this can be removed
            foreach (var state in converterStates)
                state.Clear();
        }

        public void OnAfterDeserialize()
        {
            // Something null, remove it
            for (int i = converterStates.Count - 1; i >= 0; i--)
            {
                if (converterStates[i] == null || converterStates[i].converter == null)
                {
                    converterStates.RemoveAt(i);
                }
            }
        }
    }
}
