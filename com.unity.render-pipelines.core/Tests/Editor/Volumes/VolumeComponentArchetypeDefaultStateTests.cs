using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using FsCheck;
using NUnit.Framework;
using UnityEngine.Tests;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEngine.Rendering.Tests
{
    public class VolumeComponentArchetypeDefaultStateTests
    {
        [OneTimeSetUp]
        public static void SetupFixture()
        {
            ArbX.Register();
        }

        [Test]
        public void ReplaceDataResetStackVolumeComponents()
        {
            bool Property(VolumeComponentType[] types, int seed)
            {
                void CallOnEnable(VolumeComponent volumeComponent)
                {
                    volumeComponent.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(volumeComponent, Array.Empty<object>());
                }

                var archetype = VolumeComponentArchetype.FromTypes(types);
                if (!archetype.GetOrAddDefaultState(out var extension))
                    return false;

                var stack = VolumeStack.FromArchetype(archetype);

                // Randomly change stack parameters
                Random.InitState(seed);
                foreach (var pair in stack.components)
                {
                    // Run OnEnable to make sure parameters are available
                    CallOnEnable(pair.Value);

                    foreach (var parameter in pair.Value.parameters)
                    {
                        parameter.overrideState = Random.value > 0.5f;
                        switch (parameter)
                        {
                            case VolumeParameter<int> intParam:
                                intParam.value = (int)(Random.value * 10000);
                                break;
                            case VolumeParameter<float> floatParam:
                                floatParam.value = Random.value;
                                break;
                        }
                    }
                }

                extension.ReplaceData(stack);
                // assert the values are replaced
                foreach (var pair in stack.components)
                {
                    foreach (var parameter in pair.Value.parameters)
                    {
                        // no parameter ar override
                        if (parameter.overrideState)
                            return false;
                    }

                    // Check default value
                    var defaultInstance = (VolumeComponent)ScriptableObject.CreateInstance(pair.Value.GetType());
                    CallOnEnable(defaultInstance);

                    var c = pair.Value.parameters.Count;
                    for (var i = 0; i < c; i++)
                    {
                        switch (pair.Value.parameters[i])
                        {
                            case VolumeParameter<int>:
                                if (defaultInstance.parameters[i].GetValue<int>() != pair.Value.parameters[i].GetValue<int>())
                                    return false;
                                break;
                            case VolumeParameter<float>:
                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                if (defaultInstance.parameters[i].GetValue<float>() != pair.Value.parameters[i].GetValue<float>())
                                    return false;
                                break;
                        }
                    }
                }

                return true;
            }

            Prop.ForAll<VolumeComponentType[], int>(Property).ContextualQuickCheckThrowOnFailure();
        }

    }
}
