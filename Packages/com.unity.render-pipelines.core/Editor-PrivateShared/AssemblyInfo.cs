using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Universal.Editor")]


//WARNING:
//  Remember to only use this shared API to cherry pick the code part that you want to
//  share but not go directly in user codebase project.
//  Every new entry here should be discussed. It is always better to have good public API.
//  Don't add logic in this assemblie. It is only to share private methods. Only redirection allowed.


/*EXAMPLE:
//In Unity.RenderPipeline.Core.Editor:
namespace TestNamespace
{
    public class PublicType
    {
        internal static void StaticDoSomething() { }
        internal void InstanceDoSomething() { }
    }

    internal class InternalType
    {
        internal static void StaticDoSomething() { }
        internal void InstanceDoSomething() { }
    }
}


//In Unity.RenderPipeline.Core.Editor.Shared:
namespace TestNamespace.Shared
{
    internal static class PublicType
    {
        public static void StaticDoSomething()
            => TestNamespace.PublicType.StaticDoSomething();

        public static void InstanceDoSomething(TestNamespace.PublicType publicType)
            => publicType.InstanceDoSomething();
        
        internal struct Wrapper
        {
            TestNamespace.PublicType m_wrapped;
        
            public Wrapper(TestNamespace.PublicType publicTypeInstance)
                => m_wrapped = publicTypeInstance;

            public void InstanceDoSomething()
                => m_wrapped.InstanceDoSomething();
        }
    }


    internal static class InternalType
    {
        public static void StaticDoSomething()
            => TestNamespace.InternalType.StaticDoSomething();

        public static void InstanceDoSomething(object objectCastedInternalType)
            => (objectCastedInternalType as TestNamespace.InternalType).InstanceDoSomething();
        
        internal struct Wrapper
        {
            TestNamespace.InternalType m_wrapped;

            public Wrapper(object objectCastedInternalType)
                => m_wrapped = objectCastedInternalType as TestNamespace.InternalType;

            public void InstanceDoSomething()
                => m_wrapped.InstanceDoSomething();
        }
    }
}


//In Unity.RenderPipeline.Universal.Editor:
class TestPrivateAPIShared
{
    void CallStaticMethodOfPublicType()
        => TestNamespace.Shared.PublicType.StaticDoSomething();
    
    void CallInstanceMethodOfPublicTypeThroughStatic()
    {
        var instance = new TestNamespace.PublicType();
        TestNamespace.Shared.PublicType.InstanceDoSomething(instance);
    }

    void CallInstanceMethodOfPublicTypeThroughWrapper()
    {
        var instance = new TestNamespace.PublicType();
        var wrapper = new TestNamespace.Shared.PublicType.Wrapper(instance);
        wrapper.InstanceDoSomething();
    }
    
    void CallStaticMethodOfInternalType()
        => TestNamespace.Shared.InternalType.StaticDoSomething();
    
    void CallInstanceMethodOfInternalTypeThroughStatic()
    {
        var instance = new object(); //get the object via an API instead
        TestNamespace.Shared.InternalType.InstanceDoSomething(instance);
    }

    void CallInstanceMethodOfInternalTypeThroughWrapper()
    {
        var instance = new object(); //get the object via an API instead
        var wrapper = new TestNamespace.Shared.InternalType.Wrapper(instance);
        wrapper.InstanceDoSomething();
    }
}
*/
