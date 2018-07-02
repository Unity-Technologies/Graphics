using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using RenderGraph;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RenderGraphSample
{
    [InitializeOnLoad]
    public static class Playground
    {
        struct TestNode : IDummy
        {
            int m_Token;

            public TestNode(int token)
            {
                m_Token = token;
            }

            public void Setup(ref ResourceBuilder builder)
            {
                Debug.Log("Setup");
            }

            public void Run(int a)
            {
                Debug.Log("Run: " + m_Token + ", " + a);
            }
        }

        interface IDummy
        {
            void Run(int a);
        }

        static Playground()
        {
            var assemblyName = new AssemblyName("RenderGraphFn");
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
            var typeBuilder = moduleBuilder.DefineType("RenderGraph.NodeFunctionLoader", TypeAttributes.Public);
            var methodBuilder = typeBuilder.DefineMethod("GetRunFn", MethodAttributes.Public | MethodAttributes.Static, typeof(IntPtr), null);
            methodBuilder.InitLocals = true;
            var typeParameters = methodBuilder.DefineGenericParameters("T");
            typeParameters[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
            typeParameters[0].SetInterfaceConstraints(typeof(IDummy));
            var ilGen = methodBuilder.GetILGenerator();
//            ilGen.DeclareLocal(typeof(TestNode));
//            ilGen.Emit(OpCodes.Ldloca_S, (short)0);
//            ilGen.Emit(OpCodes.Ldc_I4_4);
//            ilGen.Emit(OpCodes.Ldftn, typeof(TestNode).GetMethod("Run", BindingFlags.Public | BindingFlags.Instance));
//            ilGen.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, typeof(void), new Type[] { typeof(int) }, null);
//            ilGen.Emit(OpCodes.Ldftn, typeof(TestNode).GetMethod("Run", BindingFlags.Public | BindingFlags.Instance));
//            ilGen.Emit(OpCodes.Ret);

            ilGen.DeclareLocal(typeParameters[0]);
//            ilGen.Emit(OpCodes.Ldloca_S, (short)0);
//            ilGen.Emit(OpCodes.Ldc_I4_4);
//            ilGen.Emit(OpCodes.Ldloca_S, (short)0);
//            ilGen.Emit(OpCodes.Ldvirtftn, typeof(IDummy).GetMethod("Run", BindingFlags.Public | BindingFlags.Instance));
//            ilGen.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, typeof(void), new Type[] { typeof(int) }, null);
            ilGen.Emit(OpCodes.Ldloc, (short)0);
//            ilGen.Emit(OpCodes.Ldobj);
            ilGen.Emit(OpCodes.Ldvirtftn, typeof(IDummy).GetMethod("Run", BindingFlags.Public | BindingFlags.Instance));
            ilGen.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();
            var method = type.GetMethod("GetRunFn", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(typeof(TestNode));
            var ptr = method.Invoke(null, null);
            Debug.Log(ptr);
        }
    }
}
