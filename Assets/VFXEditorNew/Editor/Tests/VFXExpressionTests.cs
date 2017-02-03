using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXExpressionTests
    {
        //Will be an helper in Node System
        static public VFXExpression CastFloat(VFXExpression from, VFXValueType toValueType, float defautValue = 0.0f)
        {
            if (!VFXExpressionFloatOperation.IsFloatValueType(from.ValueType) || !VFXExpressionFloatOperation.IsFloatValueType(toValueType))
            {
                throw new ArgumentException(string.Format("Invalid CastFloat : {0} to {1}", from, toValueType));
            }

            if (from.ValueType == toValueType)
            {
                throw new ArgumentException(string.Format("Incoherent CastFloat : {0} to {1}", from, toValueType));
            }

            var fromValueType = from.ValueType;
            var fromValueTypeSize = VFXExpression.TypeToSize(fromValueType);
            var toValueTypeSize = VFXExpression.TypeToSize(toValueType);

            var inputComponent = new VFXExpression[fromValueTypeSize];
            var outputComponent = new VFXExpression[toValueTypeSize];

            if (inputComponent.Length == 1)
            {
                inputComponent[0] = from;
            }
            else
            {
                for (int iChannel = 0; iChannel < fromValueTypeSize; ++iChannel)
                {
                    inputComponent[iChannel] = new VFXExpressionExtractComponent(from, iChannel);
                }
            }

            for (int iChannel = 0; iChannel < toValueTypeSize; ++iChannel)
            {
                if (iChannel < fromValueTypeSize)
                {
                    outputComponent[iChannel] = inputComponent[iChannel];
                }
                else if (fromValueTypeSize == 1)
                {
                    //Manage same logic behavior for float => floatN in HLSL
                    outputComponent[iChannel] = inputComponent[0];
                }
                else
                {
                    outputComponent[iChannel] = new VFXValueFloat(defautValue, true);
                }
            }

            if (toValueTypeSize == 1)
            {
                return outputComponent[0];
            }

            var combine = new VFXExpressionCombine(outputComponent);
            return combine;
        }

        [Test]
        public void ProcessExpressionBasic()
        {
            //Reference some float math operation
            var a = new Vector2(0.75f, 0.5f);
            var b = new Vector3(1.3f, 0.2f, 0.7f);
            var c = 0.8f;
            var d = 0.1f;

            var refResultA = new Vector3(a.x + b.x, a.y + b.y, b.z);
            var refResultB = new Vector3(Mathf.Sin(refResultA.x), Mathf.Sin(refResultA.y), Mathf.Sin(refResultA.z));
            var refResultC = refResultB * c;
            var refResultD = new Vector3(d, d, d) - refResultC;

            //Using expression system
            var value_a = new VFXValueFloat2(a, true);
            var value_b = new VFXValueFloat3(b, true);
            var value_c = new VFXValueFloat(c, true);
            var value_d = new VFXValueFloat(d, true);

            var addExpression = new VFXExpressionAdd(CastFloat(value_a, value_b.ValueType), value_b);
            var sinExpression = new VFXExpressionSin(addExpression);
            var mulExpression = new VFXExpressionMul(sinExpression, CastFloat(value_c, sinExpression.ValueType));
            var substractExpression = new VFXExpressionSubtract(CastFloat(value_d, mulExpression.ValueType), mulExpression);

            var context = new VFXExpression.Context();
            var resultA = context.GetReduced(addExpression);
            var resultB = context.GetReduced(sinExpression);
            var resultC = context.GetReduced(mulExpression);
            var resultD = context.GetReduced(substractExpression);

            Assert.AreEqual(refResultA, (resultA as VFXValueFloat3).Content);
            Assert.AreEqual(refResultB, (resultB as VFXValueFloat3).Content);
            Assert.AreEqual(refResultC, (resultC as VFXValueFloat3).Content);
            Assert.AreEqual(refResultD, (resultD as VFXValueFloat3).Content);

            //Build Graph Proto :
            var addedExpression = new HashSet<VFXExpression>();
            var graphLevel = new Stack<VFXExpression[]>();
            graphLevel.Push(new VFXExpression[] { substractExpression });

            bool hasParent = false;
            do
            {
                hasParent = false;
                var currentLevelList = graphLevel.Peek();
                var parentsList = currentLevelList  .SelectMany(o => o.Parents)
                                                    .Where(o => !addedExpression.Contains(o))
                                                    .Distinct().ToArray();
                if (parentsList.Length > 0)
                {
                    hasParent = true;
                    addedExpression.UnionWith(parentsList);
                    graphLevel.Push(parentsList);
                }

            } while (hasParent);


            var functionList = new HashSet<string>();
            var uniformList = new HashSet<string>();
            var callFunction = new System.Text.StringBuilder();
            while (graphLevel.Count > 0)
            {
                var currentLevelGeneration = graphLevel.Pop();
                foreach (var operation in currentLevelGeneration)
                {
                    if (operation.Is(VFXExpression.Flags.Value))
                    {
                        uniformList.Add(string.Format("{0} {1};\n", VFXExpression.TypeToCode(operation.ValueType), "temp_" + operation.GetHashCode().ToString("X")));
                    }
                    else
                    {
                        string function, call;
                        operation.GetExpressionCode(out function, out call);
                        functionList.Add(function);
                        callFunction.Append(call);
                        callFunction.AppendLine();
                    }
                }
            }

            string final = string.Format("//Inputs\n{0}\n//Functions\n{1}//Call\n{2}",
                                            uniformList.Aggregate((x, y) => x + y),
                                            functionList.Aggregate((x, y) => x + y), 
                                            callFunction.ToString());
            Assert.AreNotEqual(final, null);
        }

        [Test]
        public void ProcessExpressionSampleCurve()
        {
            var a = 0.564f;
            var curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.2f, 0.7f), new Keyframe(0.8f, 0.1f), new Keyframe(1, 1));
            var resultRef = curve.Evaluate(a);

            var sampleValue = new VFXValueFloat(a, true);
            var curveValue = new VFXValueCurve(curve, true);
            var sampleCurve = new VFXExpressionSampleCurve(curveValue, sampleValue);

            var context = new VFXExpression.Context();
            var reduced = context.GetReduced(sampleCurve);

            Assert.AreEqual(resultRef, (reduced as VFXValueFloat).Content);
        }

        [Test]
        public void ProcessExpressionTestConcreteExpression()
        {
            var expressionTypes = typeof(VFXExpression)
                                  .Assembly.GetTypes()
                                  .Where(t => t.IsSubclassOf(typeof(VFXExpression)) && !t.IsAbstract);

            var newInstanceHelper = typeof(VFXExpression).GetMethod("CreateNewInstance", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Type) }, null);
            Assert.AreNotEqual(newInstanceHelper, null);

            foreach (var expressionType in expressionTypes)
            {
                object newInstance = null;
                Assert.DoesNotThrow(() => { newInstance = newInstanceHelper.Invoke(null, new Type[] { expressionType }); },
                                            "ProcessExpressionTestConcreteExpression fails with : " + expressionType.FullName);
                Assert.AreNotEqual(newInstance, null);
                Assert.AreEqual(newInstance.GetType(), expressionType);
            }
        }

        [Test]
        public void ProcessExpressionGPUCodeGeneration() //Test Prototype
        {
            //Reference some float math operation
            var a = new Vector2(0.75f, 0.5f);
            var b = new Vector3(1.3f, 0.2f, 0.7f);
            var c = 0.8f;
            var d = 0.1f;

            var refResultA = new Vector3(a.x + b.x, a.y + b.y, b.z);
            var refResultB = new Vector3(Mathf.Sin(refResultA.x), Mathf.Sin(refResultA.y), Mathf.Sin(refResultA.z));
            var refResultC = refResultB * c;
            var refResultD = new Vector3(d, d, d) - refResultC;

            //Using expression system
            var value_a = new VFXValueFloat2(a, true);
            var value_b = new VFXValueFloat3(b, true);
            var value_c = new VFXValueFloat(c, true);
            var value_d = new VFXValueFloat(d, true);

            var addExpression = new VFXExpressionAdd(CastFloat(value_a, value_b.ValueType), value_b);
            var sinExpression = new VFXExpressionSin(addExpression);
            var mulExpression = new VFXExpressionMul(sinExpression, CastFloat(value_c, sinExpression.ValueType));
            var substractExpression = new VFXExpressionSubtract(CastFloat(value_d, mulExpression.ValueType), mulExpression);

            var addedExpression = new HashSet<VFXExpression>();
            var graphLevel = new Stack<VFXExpression[]>();
            graphLevel.Push(new VFXExpression[] { substractExpression });

            bool hasParent = false;
            do
            {
                hasParent = false;
                var currentLevelList = graphLevel.Peek();
                var parentsList = currentLevelList.SelectMany(o => o.Parents)
                                                    .Where(o => !addedExpression.Contains(o))
                                                    .Distinct().ToArray();
                if (parentsList.Length > 0)
                {
                    hasParent = true;
                    addedExpression.UnionWith(parentsList);
                    graphLevel.Push(parentsList);
                }

            } while (hasParent);


            var functionList = new HashSet<string>();
            var uniformList = new HashSet<string>();
            var callFunction = new System.Text.StringBuilder();
            while (graphLevel.Count > 0)
            {
                var currentLevelGeneration = graphLevel.Pop();
                foreach (var operation in currentLevelGeneration)
                {
                    if (operation.Is(VFXExpression.Flags.Value))
                    {
                        uniformList.Add(string.Format("{0} {1};\n", VFXExpression.TypeToCode(operation.ValueType), "temp_" + operation.GetHashCode().ToString("X")));
                    }
                    else
                    {
                        string function, call;
                        operation.GetExpressionCode(out function, out call);
                        functionList.Add(function);
                        callFunction.Append(call);
                        callFunction.AppendLine();
                    }
                }
            }

            string final = string.Format("//Inputs\n{0}\n//Functions\n{1}//Call\n{2}",
                                            uniformList.Aggregate((x, y) => x + y),
                                            functionList.Aggregate((x, y) => x + y),
                                            callFunction.ToString());
            Assert.AreNotEqual(final, null);
        }
    }
}
