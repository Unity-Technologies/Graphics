using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// This attribute allows you to add commands to the <strong>Add Override</strong> popup menu
    /// on Volumes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class VolumeComponentMenu : Attribute
    {
        /// <summary>
        /// The name of the entry in the override list. You can use slashes to create sub-menus.
        /// </summary>
        public readonly string menu;
        // TODO: Add support for component icons

        /// <summary>
        /// Creates a new <seealso cref="VolumeComponentMenu"/> instance.
        /// </summary>
        /// <param name="menu">The name of the entry in the override list. You can use slashes to
        /// create sub-menus.</param>
        public VolumeComponentMenu(string menu)
        {
            this.menu = menu;
        }
    }

    /// <summary>
    /// An attribute set on deprecated volume components.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class VolumeComponentDeprecated : Attribute
    {
    }

    /// <summary>
    /// The base class for all the components that can be part of a <see cref="VolumeProfile"/>.
    /// The Volume framework automatically handles and interpolates any <see cref="VolumeParameter"/> members found in this class.
    /// </summary>
    /// <example>
    /// <code>
    /// using UnityEngine.Rendering;
    ///
    /// [Serializable, VolumeComponentMenu("Custom/Example Component")]
    /// public class ExampleComponent : VolumeComponent
    /// {
    ///     public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class VolumeComponent : ScriptableObject
    {
        /// <summary>
        /// The active state of the set of parameters defined in this class. You can use this to
        /// quickly turn on or off all the overrides at once.
        /// </summary>
        public bool active = true;

        /// <summary>
        /// The name displayed in the component header. If you do not set a name, Unity generates one from
        /// the class name automatically.
        /// </summary>
        public string displayName { get; protected set; } = "";

        /// <summary>
        /// A read-only collection of all the <see cref="VolumeParameter"/>s defined in this class.
        /// </summary>
        public ReadOnlyCollection<VolumeParameter> parameters { get; private set; }

#pragma warning disable 414
        [SerializeField]
        bool m_AdvancedMode = false; // Editor-only
#pragma warning restore 414

        /// <summary>
        /// Extracts all the <see cref="VolumeParameter"/>s defined in this class and nested classes.
        /// </summary>
        /// <param name="o">The object to find the parameters</param>
        /// <param name="parameters">The list filled with the parameters.</param>
        /// <param name="filter">If you want to filter the parameters</param>
        internal static void FindParameters(object o, List<VolumeParameter> parameters, Func<FieldInfo, bool> filter = null)
        {
            if (o == null)
                return;

            var fields = o.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .OrderBy(t => t.MetadataToken); // Guaranteed order

            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                {
                    if (filter?.Invoke(field) ?? true)
                        parameters.Add((VolumeParameter)field.GetValue(o));
                }
                else if (!field.FieldType.IsArray && field.FieldType.IsClass)
                    FindParameters(field.GetValue(o), parameters, filter);
            }
        }

        /// <summary>
        /// Unity calls this method when it loads the class.
        /// </summary>
        /// <remarks>
        /// If you want to override this method, you must call <c>base.OnEnable()</c>.
        /// </remarks>
        protected virtual void OnEnable()
        {
            // Automatically grab all fields of type VolumeParameter for this instance
            var fields = new List<VolumeParameter>();
            FindParameters(this, fields);
            parameters = fields.AsReadOnly();

            foreach (var parameter in parameters)
            {
                if (parameter != null)
                    parameter.OnEnable();
                else
                    Debug.LogWarning("Volume Component " + GetType().Name + " contains a null parameter; please make sure all parameters are initialized to a default value. Until this is fixed the null parameters will not be considered by the system.");
            }
        }

        /// <summary>
        /// Unity calls this method when the object goes out of scope.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (parameters == null)
                return;

            foreach (var parameter in parameters)
            {
                if (parameter != null)
                    parameter.OnDisable();
            }
        }

        /// <summary>
        /// Interpolates a <see cref="VolumeComponent"/> with this component by an interpolation
        /// factor and puts the result back into the given <see cref="VolumeComponent"/>.
        /// </summary>
        /// <remarks>
        /// You can override this method to do your own blending. Either loop through the
        /// <see cref="parameters"/> list or reference direct fields. You should only use
        /// <see cref="VolumeParameter.SetValue"/> to set parameter values and not assign
        /// directly to the state object. you should also manually check
        /// <see cref="VolumeParameter.overrideState"/> before you set any values.
        /// </remarks>
        /// <param name="state">The internal component to interpolate from. You must store
        /// the result of the interpolation in this same component.</param>
        /// <param name="interpFactor">The interpolation factor in range [0,1].</param>
        /// <example>
        /// Below is the default implementation for blending:
        /// <code>
        /// public virtual void Override(VolumeComponent state, float interpFactor)
        /// {
        ///     int count = parameters.Count;
        ///
        ///     for (int i = 0; i &lt; count; i++)
        ///     {
        ///         var stateParam = state.parameters[i];
        ///         var toParam = parameters[i];
        ///
        ///         // Keep track of the override state for debugging purpose
        ///         stateParam.overrideState = toParam.overrideState;
        ///
        ///         if (toParam.overrideState)
        ///             stateParam.Interp(stateParam, toParam, interpFactor);
        ///     }
        /// }
        /// </code>
        /// </example>
        public virtual void Override(VolumeComponent state, float interpFactor)
        {
            int count = parameters.Count;

            for (int i = 0; i < count; i++)
            {
                var stateParam = state.parameters[i];
                var toParam = parameters[i];

                if (toParam.overrideState)
                {
                    // Keep track of the override state for debugging purpose
                    stateParam.overrideState = toParam.overrideState;
                    stateParam.Interp(stateParam, toParam, interpFactor);
                }
            }
        }

        /// <summary>
        /// Sets the state of all the overrides on this component to a given value.
        /// </summary>
        /// <param name="state">The value to set the state of the overrides to.</param>
        public void SetAllOverridesTo(bool state)
        {
            SetOverridesTo(parameters, state);
        }

        /// <summary>
        /// Sets the override state of the given parameters on this component to a given value.
        /// </summary>
        /// <param name="state">The value to set the state of the overrides to.</param>
        internal void SetOverridesTo(IEnumerable<VolumeParameter> enumerable, bool state)
        {
            foreach (var prop in enumerable)
            {
                prop.overrideState = state;
                var t = prop.GetType();

                if (VolumeParameter.IsObjectParameter(t))
                {
                    // This method won't be called a lot but this is sub-optimal, fix me
                    var innerParams = (ReadOnlyCollection<VolumeParameter>)
                        t.GetProperty("parameters", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(prop, null);

                    if (innerParams != null)
                        SetOverridesTo(innerParams, state);
                }
            }
        }

        /// <summary>
        /// A custom hashing function that Unity uses to compare the state of parameters.
        /// </summary>
        /// <returns>A computed hash code for the current instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                //return parameters.Aggregate(17, (i, p) => i * 23 + p.GetHash());

                int hash = 17;

                for (int i = 0; i < parameters.Count; i++)
                    hash = hash * 23 + parameters[i].GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Unity calls this method before the object is destroyed.
        /// </summary>
        protected virtual void OnDestroy() => Release();

        /// <summary>
        /// Releases all the allocated resources.
        /// </summary>
        public void Release()
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] != null)
                    parameters[i].Release();
            }
        }
    }
}
