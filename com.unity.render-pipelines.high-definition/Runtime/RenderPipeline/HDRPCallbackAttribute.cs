using UnityEngine.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Utility functions for allowing easy configuration of external callbacks from within HDRP render loop.
// Any method within the HDRP assembly tagged with the HDRPCallbackMethod attribute will get invoked
// when HDRPCallbackAttribute.ConfigureAllLoadedCallbacks() is called.
// HDRenderPipeline.cs calls ConfigureAllLoadedCallbacks() within the HDRenderPipeline constructor.
// Users are expected to subscribe to relevant callbacks within their configuration method.

[AttributeUsage(AttributeTargets.Class)]
public class HDRPCallbackAttribute : PreserveAttribute
{
    static public void ConfigureCallbacks(Type type)
    {
        var methodInfo = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(mi => mi.GetCustomAttributes(typeof(HDRPCallbackMethodAttribute), false).Length > 0).FirstOrDefault();

        if (methodInfo != null)
            methodInfo.Invoke(null, null);
    }

    static public void ConfigureCallbacks(IEnumerable<Type> types)
    {
        foreach (var type in types)
            ConfigureCallbacks(type);
    }

    static public void ConfigureAllLoadedCallbacks()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            ConfigureCallbacks(assembly.GetTypes());
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class HDRPCallbackMethodAttribute : PreserveAttribute { }