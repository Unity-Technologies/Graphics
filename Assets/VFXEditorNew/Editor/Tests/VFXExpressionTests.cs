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

            var addExpression = new VFXExpressionAdd(value_a, value_b);
            var sinExpression = new VFXExpressionSin(addExpression);
            var mulExpression = new VFXExpressionMul(sinExpression, value_c);
            var substractExpression = new VFXExpressionSubtract(value_d, mulExpression);

            var context = new VFXExpressionContext();
            var resultA = addExpression.Reduce(context);
            var resultB = sinExpression.Reduce(context);
            var resultC = mulExpression.Reduce(context);
            var resultD = substractExpression.Reduce(context);

            Assert.AreEqual((resultA as VFXValueFloat3).Content, refResultA);
            Assert.AreEqual((resultB as VFXValueFloat3).Content, refResultB);
            Assert.AreEqual((resultC as VFXValueFloat3).Content, refResultC);
            Assert.AreEqual((resultD as VFXValueFloat3).Content, refResultD);

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
                    parentsList.ToList().ForEach(o => addedExpression.Add(o)); //f*** la flemme ^^
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
            var reduced = sampleCurve.Reduce(new VFXExpressionContext());

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

            var addExpression = new VFXExpressionAdd(value_a, value_b);
            var sinExpression = new VFXExpressionSin(addExpression);
            var mulExpression = new VFXExpressionMul(sinExpression, value_c);
            var substractExpression = new VFXExpressionSubtract(value_d, mulExpression);

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
