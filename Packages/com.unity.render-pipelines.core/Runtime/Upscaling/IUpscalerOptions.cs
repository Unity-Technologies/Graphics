#if ENABLE_UPSCALER_FRAMEWORK
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Scripting;
using static UnityEngine.Rendering.DynamicResolutionHandler;

namespace UnityEngine.Rendering
{
#nullable enable
    #region Attributes

    /// <summary>
    /// Base class for all custom option attributes.
    /// UpscalerOptions attributes facilitate GUI rendering for a given option by sepcifying its type, range of values and display label.
    /// </summary>
    public abstract class BaseOptionAttribute : PropertyAttribute // Using PropertyAttribute for Unity Editor integration
    {
        public string? DisplayName { get; protected set; }

        protected BaseOptionAttribute(string? displayName = null)
        {
            DisplayName = displayName;
        }
    }


    /// <summary>
    /// Marks an int field as representing an enum value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class EnumOptionAttribute : BaseOptionAttribute
    {
        public Type EnumType { get; private set; }

        public EnumOptionAttribute(Type enumType, string? displayName = null) : base(displayName)
        {
            if (enumType == null || !enumType.IsEnum)
            {
                throw new ArgumentException("EnumType must be a valid Enum type.", nameof(enumType));
            }
            EnumType = enumType;
        }
    }

    /// <summary>
    /// Marks a float field as an option.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class FloatOptionAttribute : BaseOptionAttribute
    {
        public float Min { get; private set; }
        public float Max { get; private set; }
        public bool HasRange { get; private set; } // Indicates if min/max were explicitly set
        public FloatOptionAttribute(string? displayName = null) : base(displayName)
        {
            HasRange = false;
            Min = 0f; // Default, will be ignored if HasRange is false
            Max = 0f;
        }
        public FloatOptionAttribute(float min, float max, string? displayName = null) : base(displayName)
        {
            HasRange = true;
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Marks an int field as an option.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class IntOptionAttribute : BaseOptionAttribute
    {
        public int Min { get; private set; }
        public int Max { get; private set; }
        public bool HasRange { get; private set; } // Indicates if min/max were explicitly set

        // Constructor for int without range
        public IntOptionAttribute(string? displayName = null) : base(displayName)
        {
            HasRange = false;
            Min = 0; // Default
            Max = 0;
        }

        // Constructor for int with range
        public IntOptionAttribute(int min, int max, string? displayName = null) : base(displayName)
        {
            HasRange = true;
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Marks a boolean field as an option.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class BoolOptionAttribute : BaseOptionAttribute
    {
        public BoolOptionAttribute(string? displayName = null) : base(displayName) { }
    }
    #endregion

    #region OptionsBase
    /// <summary>
    /// Represents a single configurable option that can be read from and written to.
    /// </summary>
    public interface IOption
    {
        string Id { get; } // A unique identifier for the option (e.g., field name)
        string DisplayName { get; } // A user-friendly name for the option
        Type ValueType { get; } // The actual C# type of the option's value (e.g., typeof(float), typeof(DLSSQuality))

        abstract object? GetValue(object targetInstance);
        abstract void SetValue(object targetInstance, object? newValue);
    }

    /// <summary>
    /// Represents an option whose value is an enum.
    /// </summary>
    public interface IEnumOption : IOption
    {
        Type EnumType { get; } // The specific enum Type (e.g., typeof(DLSSQuality))
        Array GetEnumValues(); // Returns an array of the actual enum values
        string[] GetEnumNames(); // Returns an array of the enum names (for display)
    }

    /// <summary>
    /// Represents a numeric option that has a defined minimum and maximum value.
    /// </summary>
    public interface IRangeOption : IOption
    {
        float MinValue { get; } // Using float for generics, int ranges will cast
        float MaxValue { get; } // Using float for generics, int ranges will cast
        bool HasRange { get; } // True if a min/max was explicitly set
    }


    /// <summary>
    /// Abstract base class for option implementations, handles common properties.
    /// </summary>
    internal abstract class OptionBase : IOption
    {
        protected readonly FieldInfo _fieldInfo;

        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public abstract Type ValueType { get; }

        protected OptionBase(FieldInfo fieldInfo, string displayName)
        {
            _fieldInfo = fieldInfo ?? throw new ArgumentNullException(nameof(fieldInfo));
            Id = fieldInfo.Name;
            DisplayName = displayName;
        }

        public virtual object? GetValue(object targetInstance)
        {
            if (targetInstance == null || !_fieldInfo.DeclaringType!.IsAssignableFrom(targetInstance.GetType()))
            {
                throw new ArgumentException($"Target instance is not compatible with option's field type. Expected {_fieldInfo.DeclaringType.Name}, got {targetInstance?.GetType().Name}.", nameof(targetInstance));
            }
            return _fieldInfo.GetValue(targetInstance);
        }

        public virtual void SetValue(object targetInstance, object? newValue)
        {
            if (targetInstance == null || !_fieldInfo.DeclaringType!.IsAssignableFrom(targetInstance.GetType()))
            {
                throw new ArgumentException($"Target instance is not compatible with option's field type. Expected {_fieldInfo.DeclaringType.Name}, got {targetInstance?.GetType().Name}.", nameof(targetInstance));
            }
            _fieldInfo.SetValue(targetInstance, newValue);
        }
    }
    #endregion


    #region TypedOptions
    /// <summary>
    /// Represents an integer option backed by an enum.
    /// </summary>
    internal class EnumIntOption : OptionBase, IEnumOption
    {
        public Type EnumType { get; private set; }

        public override Type ValueType => EnumType;

        public EnumIntOption(FieldInfo fieldInfo, string displayName, Type enumType)
            : base(fieldInfo, displayName)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException($"Provided type '{enumType.Name}' is not an enum.", nameof(enumType));
            }
            if (fieldInfo.FieldType != typeof(int))
            {
                throw new ArgumentException($"EnumIntOption can only wrap int fields. Field '{fieldInfo.Name}' is of type '{fieldInfo.FieldType.Name}'.", nameof(fieldInfo));
            }
            EnumType = enumType;
        }

        public override object? GetValue(object targetInstance)
        {
            int intValue = (int)(base.GetValue(targetInstance) ?? 0); // Handle potential null for value types
            return Enum.ToObject(EnumType, intValue);
        }

        public override void SetValue(object targetInstance, object? newValue)
        {
            if (newValue != null && newValue.GetType() != EnumType)
            {
                throw new ArgumentException($"Cannot set EnumIntOption '{DisplayName}': value type mismatch. Expected '{EnumType.Name}', got '{newValue?.GetType().Name}'.", nameof(newValue));
            }
            base.SetValue(targetInstance, Convert.ToInt32(newValue));
        }

        public Array GetEnumValues() => Enum.GetValues(EnumType);
        public string[] GetEnumNames() => Enum.GetNames(EnumType);
    }

    /// <summary>
    /// Represents a boolean option.
    /// </summary>
    internal class BoolOption : OptionBase
    {
        public override Type ValueType => typeof(bool);

        public BoolOption(FieldInfo fieldInfo, string displayName)
            : base(fieldInfo, displayName)
        {
            if (fieldInfo.FieldType != typeof(bool))
            {
                throw new ArgumentException($"BoolOption can only wrap bool fields. Field '{fieldInfo.Name}' is of type '{fieldInfo.FieldType.Name}'.", nameof(fieldInfo));
            }
        }
    }
    
    /// <summary>
    /// Represents a float option, with an optional min/max range.
    /// </summary>
    internal class FloatOption : OptionBase, IRangeOption
    {
        public override Type ValueType => typeof(float);
        public float MinValue { get; private set; }
        public float MaxValue { get; private set; }
        public bool HasRange { get; private set; }

        public FloatOption(FieldInfo fieldInfo, string displayName, float min, float max, bool hasRange)
            : base(fieldInfo, displayName)
        {
            if (fieldInfo.FieldType != typeof(float))
            {
                throw new ArgumentException($"FloatOption can only wrap float fields. Field '{fieldInfo.Name}' is of type '{fieldInfo.FieldType.Name}'.", nameof(fieldInfo));
            }
            MinValue = min;
            MaxValue = max;
            HasRange = hasRange;
        }
    }

    /// <summary>
    /// Represents an int option, with an optional min/max range.
    /// </summary>
    internal class IntOption : OptionBase, IRangeOption
    {
        public override Type ValueType => typeof(int);
        public float MinValue { get; private set; } // Storing as float, will cast to int when used
        public float MaxValue { get; private set; } // Storing as float, will cast to int when used
        public bool HasRange { get; private set; }

        public IntOption(FieldInfo fieldInfo, string displayName, int min, int max, bool hasRange)
            : base(fieldInfo, displayName)
        {
            if (fieldInfo.FieldType != typeof(int))
            {
                throw new ArgumentException($"IntOption can only wrap int fields. Field '{fieldInfo.Name}' is of type '{fieldInfo.FieldType.Name}'.", nameof(fieldInfo));
            }
            MinValue = min;
            MaxValue = max;
            HasRange = hasRange;
        }
    }

    #endregion

    #region OptionsRegistry
    /// <summary>
    /// Provides methods to generically inspect UpscalerOptions instances
    /// and retrieve their configurable options.
    /// </summary>
    public static class UpscalerOptionsRegistry
    {
        // Cached reflection results
        private static readonly Dictionary<Type, List<IOption>> s_CachedOptions = new();

#if UNITY_EDITOR
        /// <summary>
        /// Discovers and returns all configurable options for a given UpscalerOptions instance.
        /// This method uses reflection and caches results for performance.
        /// </summary>
        /// <param name="optionsInstance">The UpscalerOptions instance to inspect.</param>
        /// <returns>A list of IOption objects representing the configurable options.</returns>
        [Preserve] // Prevent IL2CPP stripping if attributes/reflection are only used by this.
        public static IReadOnlyList<IOption> GetConfigurableOptions(UpscalerOptions optionsInstance)
        {
            if (optionsInstance == null)
            {
                Debug.LogWarning("GetConfigurableOptions called with a null options instance.");
                return new List<IOption>();
            }

            Type optionsType = optionsInstance.GetType();

            // Use cached metadata if available
            if (s_CachedOptions.TryGetValue(optionsType, out var optionsList))
            {
                return optionsList;
            }

            // Otherwise, perform reflection and build the metadata
            List<IOption> discoveredOptions = new List<IOption>();

            // Get all instance fields, both public and non-public (for [SerializeField] private fields)
            FieldInfo[] fields = optionsType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                // Check for our custom option attributes
                BaseOptionAttribute? attr = field.GetCustomAttribute<BaseOptionAttribute>(true);

                if (attr == null) continue; // Not an option we care about

                // Determine the display name, prioritizing the one from the attribute
                string displayName = attr.DisplayName ?? ObjectNames.NicifyVariableName(field.Name);

                // Create the appropriate IOption wrapper based on the attribute type
                if (attr is EnumOptionAttribute enumAttr)
                {
                    discoveredOptions.Add(new EnumIntOption(field, displayName, enumAttr.EnumType));
                }
                else if (attr is FloatOptionAttribute floatAttr)
                {
                    discoveredOptions.Add(new FloatOption(field, displayName, floatAttr.Min, floatAttr.Max, floatAttr.HasRange));
                }
                else if (attr is IntOptionAttribute intAttr)
                {
                    discoveredOptions.Add(new IntOption(field, displayName, intAttr.Min, intAttr.Max, intAttr.HasRange));
                }
                else if (attr is BoolOptionAttribute)
                {
                    discoveredOptions.Add(new BoolOption(field, displayName));
                }
                else
                {
                    Debug.LogWarning($"Unhandled BaseOptionAttribute type for field '{field.Name}' in '{optionsType.Name}': {attr.GetType().Name}.");
                }
            }

            // Cache and return
            s_CachedOptions[optionsType] = discoveredOptions;
            return discoveredOptions;
        }

        /// <summary>
        /// Finds a specific option by its ID (field name).
        /// </summary>
        /// <param name="optionsInstance">The UpscalerOptions instance.</param>
        /// <param name="optionId">The ID (field name) of the option to find.</param>
        /// <returns>The IOption object if found, otherwise null.</returns>
        public static IOption? GetOptionById(UpscalerOptions optionsInstance, string optionId)
        {
            foreach(IOption opt in GetConfigurableOptions(optionsInstance))
            {
                if (opt.Id == optionId)
                    return opt;
            }
            return null;
        }
#endif
    };
    #endregion


    [Serializable]
    public class UpscalerOptions : ScriptableObject
    {
        public string UpscalerName
        {
            get => m_UpscalerName;
            set => m_UpscalerName = value;
        }

        public UpsamplerScheduleType InjectionPoint
        {
            get => m_InjectionPoint;
            set => m_InjectionPoint = value;
        }

        [SerializeField] private string m_UpscalerName = "";
        [SerializeField] private UpsamplerScheduleType m_InjectionPoint = UpsamplerScheduleType.BeforePost;


#if UNITY_EDITOR
        public bool DrawOptionsEditorGUI()
        {
            bool optionUpdated = false;
            IReadOnlyList<IOption> options = UpscalerOptionsRegistry.GetConfigurableOptions(this);
            foreach (IOption opt in options)
            {
                object? currentValue = opt.GetValue(this);
                object? newValue = null; // store the value returned by the GUI control

                // ranged int/float options
                if (opt is IRangeOption rangeOption && rangeOption.HasRange)
                {
                    if (opt.ValueType == typeof(float))
                    {
                        float currentFloatValue = (float)currentValue!;
                        newValue = EditorGUILayout.Slider(opt.DisplayName, currentFloatValue, rangeOption.MinValue, rangeOption.MaxValue);
                    }
                    else if (opt.ValueType == typeof(int))
                    {
                        int currentIntValue = (int)currentValue!;
                        newValue = EditorGUILayout.IntSlider(opt.DisplayName, currentIntValue, (int)rangeOption.MinValue, (int)rangeOption.MaxValue);
                    }
                    else
                    {
                        // Fallback for unexpected IRangeOption types (shouldn't happen if attributes are correct)
                        EditorGUILayout.LabelField(opt.DisplayName, $"Range Type (Unhandled): {opt.ValueType.Name}, Value: {currentValue?.ToString() ?? "N/A"}");
                    }
                }
                // regular options
                else
                {
                    if (opt is IEnumOption enumOption)
                    {
                        Enum currentEnumValue = (Enum)currentValue!;
                        newValue = EditorGUILayout.EnumPopup(enumOption.DisplayName, currentEnumValue);
                    }
                    else if (opt.ValueType == typeof(float))
                    {
                        float currentFloatValue = (float)currentValue!;
                        newValue = EditorGUILayout.FloatField(opt.DisplayName, currentFloatValue);
                    }
                    else if (opt.ValueType == typeof(int))
                    {
                        int currentIntValue = (int)currentValue!;
                        newValue = EditorGUILayout.IntField(opt.DisplayName, currentIntValue);
                    }
                    else if (opt.ValueType == typeof(bool))
                    {
                        bool currentBoolValue = (bool)currentValue!;
                        newValue = EditorGUILayout.Toggle(opt.DisplayName, currentBoolValue);
                    }
                }

                bool valueChanged = newValue != null && !newValue.Equals(currentValue);
                if (valueChanged)
                {
                    opt.SetValue(this, newValue);
                    optionUpdated = true;
                }
            }

            return optionUpdated;
        }

        // The core method to ensure options exist and are linked.
        // It operates on a SerializedProperty representing the list.
        public static bool ValidateSerializedUpscalerOptionReferencesWithinRPAsset(ScriptableObject parentRPAsset, SerializedProperty optionsListProp)
        {
            if (parentRPAsset == null)
            {
                Debug.LogError("[Auto-Populate] Parent asset is null.");
                return false;
            }
            if (optionsListProp == null || !optionsListProp.isArray)
            {
                Debug.LogError($"[Auto-Populate] Provided SerializedProperty '{optionsListProp?.name ?? "null"}' is not a valid list for upscaler options.");
                return false;
            }

            bool propertyModified = false;

            // remove null entries
            for (int i = optionsListProp.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty elementProp = optionsListProp.GetArrayElementAtIndex(i);
                if (elementProp.objectReferenceValue == null)
                {
                    optionsListProp.DeleteArrayElementAtIndex(i);
                    propertyModified = true;
                    Debug.LogWarning($"[RP Asset] Removed null upscaler option from asset '{parentRPAsset.name}'.");
                }
            }

            // default-initialize registered upscaler options if they're not found within serialized asset
            foreach (var kvp in UpscalerRegistry.s_RegisteredUpscalers)
            {
                Type upscalerType = kvp.Key;
                Type? optionsType = kvp.Value.OptionsType;

                if (optionsType == null)
                    continue;

                string upscalerName = kvp.Value.ID;

                bool foundExisting = false;
                for (int i = 0; i < optionsListProp.arraySize; i++)
                {
                    SerializedProperty elementProp = optionsListProp.GetArrayElementAtIndex(i);
                    UpscalerOptions? existingOption = elementProp.objectReferenceValue as UpscalerOptions;

                    if (existingOption != null && existingOption.GetType() == optionsType /*&& existingOption.UpscalerName == upscalerName*/)
                    {
                        foundExisting = true;
                        break;
                    }
                }

                if (!foundExisting)
                {
                    UpscalerOptions newOption = (UpscalerOptions)ScriptableObject.CreateInstance(optionsType);
                    newOption.hideFlags = HideFlags.HideInHierarchy;
                    newOption.UpscalerName = upscalerName;

                    AssetDatabase.AddObjectToAsset(newOption, parentRPAsset);

                    optionsListProp.arraySize++;
                    optionsListProp.GetArrayElementAtIndex(optionsListProp.arraySize - 1).objectReferenceValue = newOption;

                    propertyModified = true;
                    Debug.Log($"[RP Asset] Auto-populated missing upscaler option on asset '{parentRPAsset.name}': {optionsType.Name} for ID: {upscalerName}");
                }
            }
            return propertyModified;
        }
#endif
    }

#nullable disable
}
#endif // ENABLE_UPSCALER_FRAMEWORK
