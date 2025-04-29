using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// This attribute is used to set up a path in the <b>Add Override</b> popup menu in Unity's Volume system.
    /// It allows you to organize and categorize your Volume components into submenus for easier access and management within the editor.
     /// </summary>
    /// <remarks>Specify the name of the menu entry, and use slashes ("/") to create hierarchical submenus in the popup. This is useful for organizing large or complex sets of Volume components.
    /// To further filter the menu entries based on the active Render Pipeline, you can combine this attribute with the <see cref="SupportedOnRenderPipeline"/> attribute.
    /// This enables conditional display of Volume components depending on the Render Pipeline being used in the project.
    /// </remarks>
    /// <example>
    /// <code>
    /// [VolumeComponentMenu("MyVolumeCategory/LightingEffects")]
    /// public class CustomLightingVolume : VolumeComponent { ... }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class VolumeComponentMenu : Attribute
    {
        /// <summary>
        /// The name of the entry in the override list. You can use slashes to create sub-menus.
        /// </summary>
        public readonly string menu;

        // TODO: Add support for component icons

        /// <summary>
        /// Creates a new <see cref="VolumeComponentMenu"/> instance.
        /// </summary>
        /// <param name="menu">The name of the entry in the override list. You can use slashes to
        /// create sub-menus.</param>
        public VolumeComponentMenu(string menu)
        {
            this.menu = menu;
        }
    }

    /// <summary>
    /// This attribute allows you to add commands to the <b>Add Override</b> popup menu on Volumes,
    /// while also specifying the render pipeline(s) for which the command will be supported.
    /// </summary>
    [Obsolete(@"VolumeComponentMenuForRenderPipelineAttribute is deprecated. Use VolumeComponentMenu with SupportedOnRenderPipeline instead. #from(2023.1)", false)]
    public class VolumeComponentMenuForRenderPipeline : VolumeComponentMenu
    {
        /// <summary>
        /// The list of pipeline types that the target class supports.
        /// </summary>
        public Type[] pipelineTypes { get; }

        /// <summary>
        /// Creates a new <see cref="VolumeComponentMenuForRenderPipeline"/> instance.
        /// </summary>
        /// <param name="menu">The name of the entry in the override list. You can use slashes to
        /// create sub-menus.</param>
        /// <param name="pipelineTypes">The list of pipeline types that the target class supports.</param>
        /// <exception cref="Exception">Thrown when the pipelineTypes is null or the types do not inherit from <see cref="RenderPipeline"/>.</exception>
        public VolumeComponentMenuForRenderPipeline(string menu, params Type[] pipelineTypes)
            : base(menu)
        {
            if (pipelineTypes == null)
                throw new Exception("Specify a list of supported pipeline.");

            // Ensure that we only allow class types that inherit from RenderPipeline
            foreach (var t in pipelineTypes)
            {
                if (!typeof(RenderPipeline).IsAssignableFrom(t))
                    throw new Exception(
                        $"You can only specify types that inherit from {typeof(RenderPipeline)}, please check {t}");
            }

            this.pipelineTypes = pipelineTypes;
        }
    }



    /// <summary>
    /// This attribute prevents the component from being included in the list of available
    /// overrides in the Volume Inspector via the <b>Add Override</b> button.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [Obsolete("VolumeComponentDeprecated has been deprecated (UnityUpgradable) -> [UnityEngine] UnityEngine.HideInInspector #from(2023.1)", false)]
    public sealed class VolumeComponentDeprecated : Attribute
    {
    }


    /// <summary>
    /// The base class for all components that can be part of a <see cref="VolumeProfile"/>.
    /// This class serves as the foundation for creating and managing volume components in Unity's
    /// volume system, enabling the handling of various <see cref="VolumeParameter"/> types in a unified way.
    /// </summary>
    /// <remarks>The <see cref="VolumeComponent"/> class is automatically integrated into the volume framework,
    /// which handles interpolation and blending of <see cref="VolumeParameter"/> members at runtime.
    /// It ensures that parameter values can be adjusted and smoothly transitioned based on different factors,
    /// such as render pipeline settings, quality settings, or user-defined parameters.
    ///
    /// Due to the need to store multiple <see cref="VolumeParameter{T}"/> types in a single collection,
    /// this base class provides a mechanism to handle them generically. It allows for easy management and
    /// manipulation of parameters of varying types, ensuring consistency across different volume components.
    ///
    /// - Stores and manages a collection of <see cref="VolumeParameter"/> objects..
    /// - Integrates seamlessly into <see cref="VolumeProfile"/> for enhanced control over rendering and post-processing effects.
    /// </remarks>
    /// <example>
    /// <para>
    /// You can create a custom volume component by inheriting from this base class and defining your own.
    /// <see cref="VolumeParameter"/> fields. The <see cref="VolumeManager"/> will handle the interpolation and blending for you.
    /// </para>
    /// <code>
    /// using UnityEngine.Rendering;
    ///
    /// [Serializable, VolumeComponentMenuForRenderPipeline("Custom/Example Component")]
    /// public class ExampleComponent : VolumeComponent
    /// {
    ///     public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    /// }
    /// </code>
    ///
    /// <para>
    /// In the example above, the custom component `ExampleComponent` extends `VolumeComponent` and defines a parameter
    /// (`intensity`) that can be manipulated within the volume framework. The `ClampedFloatParameter` is a type of
    /// <see cref="VolumeParameter{T}"/> that ensures the value remains within a specified range. 
    /// </para>
    /// </example>
    [Serializable]
    public partial class VolumeComponent : ScriptableObject
    {
        /// <summary>
        /// Local attribute for VolumeComponent fields only.
        /// It handles relative indentation of a property for inspector.
        /// </summary>
        public sealed class Indent : PropertyAttribute
        {
            /// <summary> Relative indent amount registered in this attribute </summary>
            public readonly int relativeAmount;

            /// <summary> Constructor </summary>
            /// <param name="relativeAmount">Relative indent change to use</param>
            public Indent(int relativeAmount = 1)
                => this.relativeAmount = relativeAmount;
        }

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
        /// The backing storage of <see cref="parameters"/>. Use this for performance-critical work.
        /// </summary>
        internal readonly List<VolumeParameter> parameterList = new();

        ReadOnlyCollection<VolumeParameter> m_ParameterReadOnlyCollection;
        /// <summary>
        /// A read-only collection of all the <see cref="VolumeParameter"/>s defined in this class.
        /// </summary>
        public ReadOnlyCollection<VolumeParameter> parameters
        {
            get
            {
                if (m_ParameterReadOnlyCollection == null)
                    m_ParameterReadOnlyCollection = parameterList.AsReadOnly();
                return m_ParameterReadOnlyCollection;
            }
        }

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
                    {
                        VolumeParameter volumeParameter = (VolumeParameter)field.GetValue(o);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        VolumeDebugData.AddVolumeParameterDebugId(volumeParameter, field);
#endif
                        parameters.Add(volumeParameter);
                    }
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
            parameterList.Clear();
            FindParameters(this, parameterList);

            foreach (var parameter in parameterList)
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
            foreach (var parameter in parameterList)
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
        /// <para> Below is the default implementation for blending:</para>
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
        ///         if (toParam.overrideState)
        ///         {
        ///             // Keep track of the override state to ensure that state will be reset on next frame (and for debugging purpose)
        ///             stateParam.overrideState = toParam.overrideState;
        ///             stateParam.Interp(stateParam, toParam, interpFactor);
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public virtual void Override(VolumeComponent state, float interpFactor)
        {
            int count = parameterList.Count;

            for (int i = 0; i < count; i++)
            {
                var stateParam = state.parameterList[i];
                var toParam = parameterList[i];

                if (toParam.overrideState)
                {
                    // Keep track of the override state to ensure that state will be reset on next frame (and for debugging purpose)
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
            SetOverridesTo(parameterList, state);
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

                for (int i = 0; i < parameterList.Count; i++)
                    hash = hash * 23 + parameterList[i].GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns true if any of the volume properites has been overridden.
        /// </summary>
        /// <returns>True if any of the volume properites has been overridden.</returns>
        public bool AnyPropertiesIsOverridden()
        {
            for (int i = 0; i < parameterList.Count; ++i)
            {
                if (parameterList[i].overrideState) return true;
            }
            return false;
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
            if (parameterList == null)
                return;

            for (int i = 0; i < parameterList.Count; i++)
            {
                if (parameterList[i] != null)
                    parameterList[i].Release();
            }
        }
    }
}
