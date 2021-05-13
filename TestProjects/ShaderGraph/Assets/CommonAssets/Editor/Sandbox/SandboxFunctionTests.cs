using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Sandbox.UnitTests
{
    class SandboxFunctionTests
    {
        [Test]
        public void CreateIdentityFunction()
        {
            var func = IdentityFunction();
            Assert.NotNull(func);
            Assert.NotNull(func.Name);
            Assert.NotNull(func.Parameters);
            Assert.AreEqual(func.Parameters.Count, 2);
            Assert.IsFalse(func.FunctionsCalled.Any());
            Assert.IsFalse(func.IncludePaths.Any());
            Assert.NotNull(func.Body);
            Assert.IsFalse(func.isGeneric);
        }

        [Test]
        public void CreateOpFunction()
        {
            var func = OpFunction();
            Assert.NotNull(func);
            Assert.NotNull(func.Name);
            Assert.NotNull(func.Parameters);
            Assert.AreEqual(func.Parameters.Count, 3);
            Assert.IsFalse(func.FunctionsCalled.Any());
            Assert.IsFalse(func.IncludePaths.Any());
            Assert.NotNull(func.Body);
            Assert.IsFalse(func.isGeneric);
        }

        [Test]
        public void TestValueEquals_SameSimpleFunction()
        {
            var func = IdentityFunction();
            Assert.NotNull(func);
            var func2 = IdentityFunction();
            Assert.NotNull(func2);

            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsTrue(func.ValueEquals(func2));

            func = OpFunction();
            Assert.NotNull(func);
            func2 = OpFunction();
            Assert.NotNull(func2);

            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsTrue(func.ValueEquals(func2));
        }

        [Test]
        public void TestValueEquals_SimpleNestedFunctions()
        {
            var func = IdentityFunction();
            var func2 = IdentityFunction();
            Assert.NotNull(func);
            Assert.NotNull(func2);
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsTrue(func.ValueEquals(func2));

            for (int wrapCount = 0; wrapCount < 5; wrapCount++)
            {
                var oldFunc = func;

                func = WrapFunction(func, "wrap" + wrapCount);
                func2 = WrapFunction(func2, "wrap" + wrapCount);
                Assert.NotNull(func);
                Assert.NotNull(func2);
                Assert.IsFalse(ReferenceEquals(func, func2));
                Assert.IsTrue(func.ValueEquals(func2));

                Assert.IsFalse(func.ValueEquals(oldFunc));
                Assert.IsTrue(func.FunctionsCalled.FirstOrDefault().ValueEquals(oldFunc));
            }
        }

        [Test]
        public void TestValueEquals_DifferentParameterTypes()
        {
            var func = IdentityFunction(type: Types._float2);
            var func2 = IdentityFunction(type: Types._float3);
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));

            func = WrapFunction(IdentityFunction(type: Types._float2));
            func2 = WrapFunction(IdentityFunction(type: Types._float3));
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));
        }

        [Test]
        public void TestValueEquals_DifferentParameterNames()
        {
            var func = IdentityFunction(inputName: "a");
            var func2 = IdentityFunction(inputName: "A");
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));

            func = IdentityFunction(outputName: "b");
            func2 = IdentityFunction(outputName: "B");
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));

            func = WrapFunction(IdentityFunction(inputName: "a"));
            func2 = WrapFunction(IdentityFunction(inputName: "A"));
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));
        }

        [Test]
        public void TestValueEquals_DifferentFunctionNames()
        {
            var func = IdentityFunction(name: "Identity");
            var func2 = IdentityFunction(name: "Identitx");
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));

            func = WrapFunction(IdentityFunction(name: "Identity"), name:"Wrap");
            func2 = WrapFunction(IdentityFunction(name: "Identitx"), name: "Wrap");
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));
        }

        [Test]
        public void TestValueEquals_DifferentBodyCode()
        {
            var func = OpFunction(op: "-");
            var func2 = OpFunction(op: "+");
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));

            func = WrapFunction(func);
            func2 = WrapFunction(func2);
            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));
        }

        [Test]
        public void AddCollidingParameterNames()
        {

        }

        [Test]
        public void TestDependentFunctions()
        {
            var func = ParallelFunctions(name:"Combined");
            Assert.NotNull(func);
            Assert.NotNull(func.Name);
            Assert.NotNull(func.Parameters);
            Assert.AreEqual(func.Parameters.Count, 5);
            Assert.IsTrue(func.FunctionsCalled.Any());
            Assert.IsFalse(func.IncludePaths.Any());
            Assert.NotNull(func.Body);
            Assert.IsFalse(func.isGeneric);

            var func2 = SerialFunctions(name:"Combined");
            Assert.NotNull(func2);
            Assert.NotNull(func2.Name);
            Assert.NotNull(func2.Parameters);
            Assert.AreEqual(func2.Parameters.Count, 3);
            Assert.IsTrue(func2.FunctionsCalled.Any());
            Assert.IsFalse(func2.IncludePaths.Any());
            Assert.NotNull(func2.Body);
            Assert.IsFalse(func2.isGeneric);

            Assert.IsFalse(ReferenceEquals(func, func2));
            Assert.IsFalse(func.ValueEquals(func2));
        }

        [Test]
        public void TestGenericFunctions()
        {
        }

        ShaderFunction IdentityFunction(SandboxType type = null, string name = "Identity", string inputName = "i", string outputName = "OUT", string assignOp = "=")
        {
            if (type == null)
                type = Types._float;

            var funcBuilder = new ShaderFunction.Builder(name);
            funcBuilder.AddInput(type, inputName);
            funcBuilder.AddOutput(type, outputName);
            funcBuilder.AddLine($"{inputName} {assignOp} {outputName};");
            return funcBuilder.Build();
        }

        ShaderFunction OpFunction(SandboxType type = null, string name = "Add", string inputA = "a", string inputB = "b", string output = "o", string op = "+")
        {
            if (type == null)
                type = Types._float;

            var funcBuilder = new ShaderFunction.Builder(name);
            funcBuilder.AddInput(type, inputA);
            funcBuilder.AddInput(type, inputB);
            funcBuilder.AddOutput(type, output);
            funcBuilder.AddLine($"{output} = {inputA} {op} {inputB};");
            return funcBuilder.Build();
        }

        ShaderFunction WrapFunction(ShaderFunction innerFunction = null, string name = null)
        {
            if (innerFunction == null)
                innerFunction = OpFunction();

            if (name == null)
                name = "Wrapper_" + innerFunction.Name;

            var funcBuilder = new ShaderFunction.Builder(name);

            foreach (var p in innerFunction.Parameters)
                funcBuilder.AddParameter(p);

            using (var args = funcBuilder.Call(innerFunction))
            {
                foreach (var p in innerFunction.Parameters)
                    args.Add(p.Name);
            }

            return funcBuilder.Build();
        }

        // bundles two function calls into a single function, exposing all of the combined parameters
        ShaderFunction ParallelFunctions(string name = null, ShaderFunction subFunctionA = null, ShaderFunction subFunctionB = null)
        {
            if (subFunctionA == null)
                subFunctionA = OpFunction();

            if (subFunctionB == null)
                subFunctionB = IdentityFunction();

            if (name == null)
                name = "Parallel_" + subFunctionA.Name + "_" + subFunctionB.Name;

            var funcBuilder = new ShaderFunction.Builder(name);

            foreach (var p in subFunctionA.Parameters)
                funcBuilder.AddParameter(p);
            foreach (var p in subFunctionB.Parameters)
                funcBuilder.AddParameter(p);

            using (var args = funcBuilder.Call(subFunctionA))
            {
                foreach (var p in subFunctionA.Parameters)
                    args.Add(p.Name);
            }

            using (var args = funcBuilder.Call(subFunctionB))
            {
                foreach (var p in subFunctionB.Parameters)
                    args.Add(p.Name);
            }

            return funcBuilder.Build();
        }

        // calls two functions serially, passing the outputs of the first to the inputs of the second
        // the inputs are the input to the first function, and the output are the outputs of the second function
        ShaderFunction SerialFunctions(string name = null, ShaderFunction subFunctionA = null, ShaderFunction subFunctionB = null)
        {
            if (subFunctionA == null)
                subFunctionA = OpFunction();

            if (subFunctionB == null)
                subFunctionB = IdentityFunction();

            if (name == null)
                name = "Serial_" + subFunctionA.Name + "_" + subFunctionB.Name;

            var funcBuilder = new ShaderFunction.Builder(name);

            var innerParams = new List<string>();
            foreach (var p in subFunctionA.Parameters)
                if (p.IsInput)
                    funcBuilder.AddParameter(p);
                else
                {
                    // declare local variable to pass output
                    var innerParam = "inner_" + p.Name;
                    funcBuilder.DeclareVariable(p.Type, innerParam);
                    innerParams.Add(innerParam);
                }

            foreach (var p in subFunctionB.Parameters)
                if (p.IsOutput)
                    funcBuilder.AddParameter(p);

            int curInnerParam = 0;
            using (var args = funcBuilder.Call(subFunctionA))
            {
                foreach (var p in subFunctionA.Parameters)
                    if (p.IsInput)
                        args.Add(p.Name);
                    else
                        args.Add(innerParams[curInnerParam++]);
            }

            curInnerParam = 0;
            using (var args = funcBuilder.Call(subFunctionB))
            {
                foreach (var p in subFunctionB.Parameters)
                    if (p.IsOutput)
                        args.Add(p.Name);
                    else
                        args.Add(innerParams[curInnerParam++]);
            }

            return funcBuilder.Build();
        }
    }
}
