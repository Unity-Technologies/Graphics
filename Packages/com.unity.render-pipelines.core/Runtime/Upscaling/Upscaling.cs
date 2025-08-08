#if ENABLE_UPSCALER_FRAMEWORK
#nullable enable
using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public static class UpscalerRegistry
    {
        public static readonly Dictionary<Type, (Type? OptionsType, string ID)> s_RegisteredUpscalers = new();

        /// <summary>
        /// Registers an IUpscaler type without any custom options type.
        /// </summary>
        public static void Register<TUpscaler>(string id) where TUpscaler : IUpscaler, new()
        {
            s_RegisteredUpscalers[typeof(TUpscaler)] = (null, id);
        }

        /// <summary>
        /// Registers an IUpscaler type with its custom options type.
        /// </summary>
        public static void Register<TUpscaler, TOptions>(string id)
            where TUpscaler : IUpscaler
            where TOptions : UpscalerOptions
        {
            s_RegisteredUpscalers[typeof(TUpscaler)] = (typeof(TOptions), id);
        }
    }

    public class Upscaling
    {
        public List<IUpscaler> upscalers = new();
        public string[] upscalerNames { get; }

        private int activeUpscalerIndex = -1;

        /// <summary>
        /// Initializes the Upscaling system with the given list of upscaler options per upscaler type.
        /// </summary>
        public Upscaling(List<UpscalerOptions> RPAssetUpscalerOptionsList)
        {
            foreach (var kvp in UpscalerRegistry.s_RegisteredUpscalers)
            {
                Type upscalerType = kvp.Key;
                Type? optionsType = kvp.Value.OptionsType;

                // find any serialized options, if any provided by the package implementor
                int optionsIndex = RPAssetUpscalerOptionsList.FindIndex(o => o != null && o.GetType() == optionsType);
                bool optionsNotFound = optionsIndex == -1;
                UpscalerOptions? options = optionsNotFound ? null: RPAssetUpscalerOptionsList[optionsIndex];

                // construct upscaler
                IUpscaler upscaler = optionsType != null
                    ? (IUpscaler)Activator.CreateInstance(upscalerType, new object[] { options! })
                    : (IUpscaler)Activator.CreateInstance(upscalerType);

                if(options != null && string.IsNullOrEmpty(options.UpscalerName))
                {
                    string upscalerName = upscaler.GetName();
                    Debug.LogWarningFormat("[Upscaling] UpscalerOptions with empty UpscalerName for {0}", upscalerName);
                    options.UpscalerName = upscalerName;
                }
                upscalers.Add(upscaler);
            }

            // Get upscaler names
            upscalerNames = new string[upscalers.Count];
            for (int i = 0; i < upscalers.Count; i++)
            {
                upscalerNames[i] = upscalers[i].GetName();
            }
        }

        /// <summary>
        /// Returns the IUpscaler at the given index.
        /// </summary>
        public IUpscaler GetUpscalerAtIndex(int index)
        {
            Debug.Assert(index >= 0 && index < upscalers.Count);
            return upscalers[index];
        }

        /// <summary>
        /// Returns whether an upscaler with the given name was found.
        /// </summary>
        public bool SetActiveUpscaler(string name)
        {
            // TODO (Apoorva): We need to allow the IUpscaler itself to decide whether it can run. E.g.
            // DLSS might need a certain version of Windows, and a compatible GPU. We should add an
            // overrideable function to IUpscaler so that the active IUpscaler can return a bool
            // indicating support.
            
            bool found = false;
            for (int i = 0; i < upscalerNames.Length; i++)
            {
                if (upscalerNames[i] == name)
                {
                    activeUpscalerIndex = i;
                    found = true;
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Returns null if no IUpscaler is active
        /// </summary>
        public IUpscaler? GetActiveUpscaler()
        {
            if (activeUpscalerIndex == -1)
                return null;

            return GetUpscalerAtIndex(activeUpscalerIndex);
        }

        /// <summary>
        /// Returns null if no IUpscaler exists for given type
        /// </summary>
        public IUpscaler? GetIUpscalerOfType<T>() where T : IUpscaler
        {
            if(!UpscalerRegistry.s_RegisteredUpscalers.ContainsKey(typeof(T)))
                return null;
            foreach (IUpscaler upscaler in upscalers)
                if (upscaler.GetType() == typeof(T))
                    return upscaler;
            Debug.LogErrorFormat($"Upscaler type {typeof(T)} not found");
            return null;
        }

        /// <summary>
        /// Returns null if no IUpscaler exists for given type
        /// </summary>
        public IUpscaler? GetIUpscalerOfType(Type T)
        {
            if (!UpscalerRegistry.s_RegisteredUpscalers.ContainsKey(T))
                return null;
            foreach (IUpscaler upscaler in upscalers)
                if (upscaler.GetType() == T)
                    return upscaler;
            Debug.LogErrorFormat($"Upscaler type {T} not found");
            return null;
        }
    }
}
#endif
