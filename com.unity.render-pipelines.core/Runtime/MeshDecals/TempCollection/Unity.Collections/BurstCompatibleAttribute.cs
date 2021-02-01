using System;

namespace Unity.Collections
{
    /// <summary>
    /// Documents and enforces (via generated tests) that the tagged method or property has to stay burst compatible.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public class BurstCompatibleAttribute : Attribute
    {
        /// <summary>
        /// Types to be used for the declared generic type or method.
        /// </summary>
        /// <remarks>
        /// The generic type arguments are tracked separately for types and methods. Say a generic type also contains
        /// a generic method, like in the case of Foo&lt;T&gt;.Bar&lt;U&gt;(T baz, U blah). You must specify
        /// GenericTypeArguments for Foo and also for Bar to establish the concrete types for T and U. When code
        /// generation occurs for the Burst compatibility tests, any time T appears (in the definition of Foo)
        /// it will be replaced with the generic type argument you specified for Foo and whenever U appears
        /// (in method Bar's body) it will be replaced by whatever generic type argument you specified for the method
        /// Bar.
        /// </remarks>
        public Type[] GenericTypeArguments { get; set; }

        public string RequiredUnityDefine = null;
    }
    /// <summary>
    /// Internal attribute to state that a method is not burst compatible even though the containing type is.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class NotBurstCompatibleAttribute : Attribute
    {
    }
}
