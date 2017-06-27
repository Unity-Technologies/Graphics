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
        //Will be function of Context Code builder
        static private string temp_GetUniqueName(object expression)
        {
            return "temp_" + expression.GetHashCode().ToString("X");
        }

        static private string temp_AggregateWithComa(System.Collections.Generic.IEnumerable<string> input)
        {
            return input.Aggregate((a, b) => a + ", " + b);
        }

        static public void temp_GetExpressionCode(VFXExpression expression, out string function, out string call)
        {
            function = call = null;
            if (expression.Is(VFXExpression.Flags.InvalidOnGPU))
            {
                throw new ArgumentException(string.Format("GetExpressionCode failed (not valid on GPU) with {0}", expression.GetType().FullName));
            }

            if (expression.Parents.Length == 0)
            {
                throw new ArgumentException(string.Format("GetExpressionCode failed (Parents empty) with {0}", expression.GetType().FullName));
            }

            var fnName = expression.GetType().Name;
            foreach (var additionnalParam in expression.AdditionnalParameters)
            {
                fnName += additionnalParam.ToString();
            }

            var param = expression.Parents.Select((o, i) => string.Format("{0} {1}", VFXExpression.TypeToCode(o.ValueType), expression.ParentsCodeName[i]));
            var fnHeader = string.Format("{0} {1}({2})", VFXExpression.TypeToCode(expression.ValueType), fnName, temp_AggregateWithComa(param));
            var fnContent = string.Format("return {0};", expression.GetOperationCodeContent());
            function = string.Format("{0}\n{{\n{1}\n}}\n", fnHeader, fnContent);
            call = string.Format("{0} {1} = {2}({3});", VFXExpression.TypeToCode(expression.ValueType), temp_GetUniqueName(expression), fnName, temp_AggregateWithComa(expression.Parents.Select(o => temp_GetUniqueName(o))));
        }

        [Test]
        public void ProcessStoreValue()
        {
            var a = 123.0f;
            var b = 789.0f;

            var valueFloat = new VFXValue<float>(0.0f);
            valueFloat.SetContent(a);
            Assert.AreEqual(a, valueFloat.Get<float>());

            valueFloat.SetContent(new FloatN(b));
            Assert.AreEqual(b, valueFloat.Get<float>());
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
            var value_a = new VFXValue<Vector2>(a);
            var value_b = new VFXValue<Vector3>(b);
            var value_c = new VFXValue<float>(c);
            var value_d = new VFXValue<float>(d);

            var addExpression = new VFXExpressionAdd(VFXOperatorUtility.CastFloat(value_a, value_b.ValueType), value_b);
            var sinExpression = new VFXExpressionSin(addExpression);
            var mulExpression = new VFXExpressionMul(sinExpression, VFXOperatorUtility.CastFloat(value_c, sinExpression.ValueType));
            var substractExpression = new VFXExpressionSubtract(VFXOperatorUtility.CastFloat(value_d, mulExpression.ValueType), mulExpression);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var resultA = context.Compile(addExpression);
            var resultB = context.Compile(sinExpression);
            var resultC = context.Compile(mulExpression);
            var resultD = context.Compile(substractExpression);

            Assert.AreEqual(refResultA, resultA.Get<Vector3>());
            Assert.AreEqual(refResultB, resultB.Get<Vector3>());
            Assert.AreEqual(refResultC, resultC.Get<Vector3>());
            Assert.AreEqual(refResultD, resultD.Get<Vector3>());

            //Build Graph Proto :
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
            }
            while (hasParent);


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
                        temp_GetExpressionCode(operation, out function, out call);
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

            var sampleValue = new VFXValue<float>(a);
            var curveValue = new VFXValue<AnimationCurve>(curve);
            var sampleCurve = new VFXExpressionSampleCurve(curveValue, sampleValue);

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            var reduced = context.Compile(sampleCurve);

            Assert.AreEqual(resultRef, reduced.Get<float>());
        }

        [Test]
        public void ProcessExpressionTestConcreteExpression()
        {
            var expressionTypes = typeof(VFXExpression)
                .Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(VFXExpression)) && !t.IsAbstract && !t.IsGenericType);

            var newInstanceHelper = typeof(VFXExpression).GetMethod("CreateNewInstance", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Type) }, null);
            Assert.AreNotEqual(newInstanceHelper, null);

            foreach (var expressionType in expressionTypes)
            {
                object newInstance = null;
                Assert.DoesNotThrow(() => { newInstance = newInstanceHelper.Invoke(null, new Type[] { expressionType }); },
                    "ProcessExpressionTestConcreteExpression fails with : " + expressionType.FullName);

                if (expressionType.GetConstructors().Any())
                {
                    Assert.AreNotEqual(newInstance, null);
                    Assert.AreEqual(newInstance.GetType(), expressionType);
                }
                else
                {
                    Assert.AreEqual(newInstance, null);
                }
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
            //var refResultD = new Vector3(d, d, d) - refResultC;

            //Using expression system
            var value_a = new VFXValue<Vector2>(a);
            var value_b = new VFXValue<Vector3>(b);
            var value_c = new VFXValue<float>(c);
            var value_d = new VFXValue<float>(d);

            var addExpression = new VFXExpressionAdd(VFXOperatorUtility.CastFloat(value_a, value_b.ValueType), value_b);
            var sinExpression = new VFXExpressionSin(addExpression);
            var mulExpression = new VFXExpressionMul(sinExpression, VFXOperatorUtility.CastFloat(value_c, sinExpression.ValueType));
            var substractExpression = new VFXExpressionSubtract(VFXOperatorUtility.CastFloat(value_d, mulExpression.ValueType), mulExpression);

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
            }
            while (hasParent);


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
                        temp_GetExpressionCode(operation, out function, out call);
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
