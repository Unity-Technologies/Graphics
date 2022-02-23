using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Tests
{
    class RemoveComponentTests
    {
        #region Components
        class Apple : MonoBehaviour { }
        class Banana : MonoBehaviour { }
        [RequireComponent(typeof(Apple))]
        class AdditionalApple : MonoBehaviour, IAdditionalData { }
        [RequireComponent(typeof(Banana))]
        class AdditionalBanana : MonoBehaviour, IAdditionalData { }
        [RequireComponent(typeof(Banana))]
        class AdditionalBananaColor : MonoBehaviour, IAdditionalData { }
        class WaterMelon : MonoBehaviour, IAdditionalData { }
        [RequireComponent(typeof(Apple), typeof(Banana))]
        class FruitBasket : MonoBehaviour, IAdditionalData { }
        #endregion

        class Property
        {
            static bool GenericRemoveComponent(
                [DisallowNull] GameObject gameObject,
                [DisallowNull] Type componentToRemove,
                [DisallowNull] Type[] componentsToAdd,
                [DisallowNull] Type[] expectedComponents,
                [DisallowNull] Action<Component> removeMethod)
            {
                // Add all the components to the game object
                foreach (var type in componentsToAdd)
                    gameObject.AddComponent(type);

                removeMethod(gameObject.GetComponent(componentToRemove));

                if (!componentsToAdd.Except(expectedComponents).All(type => gameObject.GetComponent(type) == null))
                    return false;

                if (!expectedComponents.All(type => gameObject.GetComponent(type) != null))
                    return false;

                return true;
            }

            static void RemoveComponent([DisallowNull] Component component) => RemoveComponentUtils.RemoveComponent(component);

            public static bool RemoveComponent(
                [DisallowNull] GameObject gameObject,
                [DisallowNull] Type componentToRemove,
                [DisallowNull] Type[] componentsToAdd,
                [DisallowNull] Type[] expectedComponents)
                => GenericRemoveComponent(gameObject, componentToRemove, componentsToAdd, expectedComponents, RemoveComponent);

            static void RemoveAdditionalComponent([DisallowNull] Component component)
            {
                using (ListPool<Type>.Get(out var componentsToRemove))
                {
                    var additionalData = component as IAdditionalData;
                    if (RemoveComponentUtils.TryGetComponentsToRemove(additionalData, componentsToRemove, out var error))
                        RemoveAdditionalDataUtils.RemoveAdditionalDataComponent(additionalData, componentsToRemove);
                }
            }
            public static bool RemoveAdditionalComponent(
                [DisallowNull] GameObject gameObject,
                [DisallowNull] Type componentToRemove,
                [DisallowNull] Type[] componentsToAdd,
                [DisallowNull] Type[] expectedComponents)
                 => GenericRemoveComponent(gameObject, componentToRemove, componentsToAdd, expectedComponents, RemoveAdditionalComponent);
        }

        static TestCaseData[] s_RemoveComponentTestCaseDatas =
        {
            new TestCaseData(typeof(Banana), new Type[] {typeof(Banana), typeof(AdditionalBanana)}, Array.Empty<Type>())
                .SetName("RemoveSingleAdditionalData"),
            new TestCaseData(typeof(Banana), new Type[] {typeof(Banana), typeof(AdditionalBanana), typeof(AdditionalBananaColor)}, Array.Empty<Type>())
                .SetName("RemovesMultipleAdditionalDatas"),
            new TestCaseData(typeof(Apple), new Type[] {typeof(Banana), typeof(AdditionalBanana), typeof(Apple), typeof(AdditionalApple)}, new Type[] {typeof(Banana), typeof(AdditionalBanana)})
                .SetName("OnlyRemoveItsAdditionalData")
        };

        [Test, TestCaseSource(nameof(s_RemoveComponentTestCaseDatas))]
        public void RemoveComponent([DisallowNull] Type componentToRemove, [DisallowNull] Type[] componentsToAdd, [DisallowNull] Type[] expectedComponents)
        {
            Assert.True(Property.RemoveComponent(m_GameObject, componentToRemove, componentsToAdd, expectedComponents));
        }

        static TestCaseData[] s_RemoveAdditionalDataComponentTestCaseDatas =
        {
            new TestCaseData(typeof(AdditionalBanana), new Type[] {typeof(Banana), typeof(AdditionalBanana) }, Array.Empty<Type>())
                .SetName("RemoveSingleAdditionalData"),
            new TestCaseData(typeof(AdditionalBananaColor), new Type[] {typeof(Banana), typeof(AdditionalBanana), typeof(AdditionalBananaColor)}, Array.Empty<Type>())
                .SetName("RemovesMultipleAdditionalDatas"),
           new TestCaseData(typeof(AdditionalBananaColor), new Type[] {typeof(Banana), typeof(Banana), typeof(AdditionalBanana), typeof(AdditionalBananaColor)}, Array.Empty<Type>())
                .SetName("RemovesMultipleComponent"),
            new TestCaseData(typeof(AdditionalApple), new Type[] {typeof(Banana), typeof(AdditionalBanana), typeof(Apple), typeof(AdditionalApple)}, new Type[] {typeof(Banana), typeof(AdditionalBanana)})
                .SetName("OnlyRemoveItsAdditionalData")
        };

        [Test, TestCaseSource(nameof(s_RemoveAdditionalDataComponentTestCaseDatas))]
        public void RemoveAdditionalDataComponent([DisallowNull] Type componentToRemove, [DisallowNull] Type[] componentsToAdd, [DisallowNull] Type[] expectedComponents)
        {
            Assert.True(Property.RemoveAdditionalComponent(m_GameObject, componentToRemove, componentsToAdd, expectedComponents));
        }

        static TestCaseData[] s_TryGetComponentsToRemoveTestCaseDatas =
         {
            new TestCaseData(typeof(FruitBasket))
                .Returns(new string[] { "Apple", "Banana" })
                .SetName("Multiple"),
            new TestCaseData(typeof(AdditionalBanana))
                .Returns(new string[] {"Banana"})
                .SetName("Single"),
            new TestCaseData(typeof(WaterMelon))
                .Returns(Array.Empty<string>())
                .SetName("Empty"),
        };

        private GameObject m_GameObject;
        [SetUp]
        public void SetUp()
        {
            m_GameObject = new("RemoveComponentTestsGO");
        }

        [TearDown]
        public void TearDown()
        {
            GameObject.DestroyImmediate(m_GameObject);
        }

        [Test, TestCaseSource(nameof(s_TryGetComponentsToRemoveTestCaseDatas))]
        public string[] TestTryGetComponentsToRemove([DisallowNull] Type type)
        {
            string[] result = Array.Empty<string>();
            var additionalData = m_GameObject.AddComponent(type) as IAdditionalData;

            using (ListPool<Type>.Get(out var componentsToRemove))
            {
                if (RemoveComponentUtils.TryGetComponentsToRemove(additionalData, componentsToRemove, out var error))
                    result = componentsToRemove.Select(t => t.Name).ToArray();
            }
            return result;
        }
    }
}
