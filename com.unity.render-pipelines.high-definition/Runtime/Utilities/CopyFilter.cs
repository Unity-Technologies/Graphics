/// <summary>
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// In the past, we forget most of the time when adding a field to add it also in the CopyTo method.
    /// To ensure we don't forget it anymore, a Runtime test have been set.
    /// But sometimes we don't want to copy all. This attribute is here to white list some.
    /// Also in some case we want to copy the content and not the actuall reference.
    /// </summary>
    /// <example>
    /// class Example
    /// {
    ///     int field1;                     //will check if value are equals
    ///     object field2;                  //will check if reference are equals
    ///     [ValueCopy]
    ///     object field3;                  //will not check the reference but check that each value inside are the same.
    ///     [ExcludeCopy]
    ///     int field4;                     //will not check anything
    ///     int property1 { get; set; }     //will check if generated backing field is copied
    ///     object property3 { get; set; }  //will check if generated backing field's reference are equals
    ///     [field: ValueCopy]
    ///     object property3 { get; set; }  //will not check the reference but check that each value inside are the same, in the generated backing field.
    ///     [field: ExcludeCopy]
    ///     int property2 { get; set; }     //will not check anything
    ///
    ///     // Also all delegate (include Action and Func) and backing field using them (such as event)
    ///     // will not be checked as moving a functor is touchy and should not be done most of the time.
    ///
    ///     void CopyTo(Example other)
    ///     {
    ///         // copy each relevant field here
    ///
    ///         // If Example is added to the type list in com.unity.render-pipelines.high-definition\Tests\Editor\CopyToTests.cs
    ///         // Every field and backing field non white listed will raise an error if not copied in this CopyTo
    ///     }
    /// }
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    class CopyFilterAttribute : Attribute
    {
        public enum Filter
        {
            Exclude = 1,        // field or backing field will not be checked by CopyTo test (white listing)
            CheckContent = 2    // check the content of object value instead of doing a simple reference check
        }
#if UNITY_EDITOR
        public readonly Filter filter;
#endif

        protected CopyFilterAttribute(Filter test)
        {
#if UNITY_EDITOR
            this.filter = test;
#endif
        }
    }

    sealed class ExcludeCopyAttribute : CopyFilterAttribute
    {
        public ExcludeCopyAttribute()
            : base(Filter.Exclude)
        { }
    }

    sealed class ValueCopyAttribute : CopyFilterAttribute
    {
        public ValueCopyAttribute()
            : base(Filter.CheckContent)
        { }
    }
}
