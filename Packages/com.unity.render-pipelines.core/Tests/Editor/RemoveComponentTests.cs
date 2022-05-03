using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Tests
{
    class RemoveComponent
    {
        #region Components
        interface ITest { }
        protected class Apple : MonoBehaviour, ITest { }
        protected class Banana : MonoBehaviour, ITest { }
        [RequireComponent(typeof(Apple))]
        protected class AdditionalApple : MonoBehaviour, IAdditionalData, ITest { }
        [RequireComponent(typeof(Banana))]
        protected class AdditionalBanana : MonoBehaviour, IAdditionalData, ITest { }
        [RequireComponent(typeof(Banana))]
        protected class AdditionalBananaColor : MonoBehaviour, IAdditionalData, ITest { }
        protected class WaterMelon : MonoBehaviour, IAdditionalData, ITest{ }
        [RequireComponent(typeof(Apple), typeof(Banana))]
        protected class FruitBasket : MonoBehaviour, IAdditionalData, ITest { }
        #endregion

        #region SetUp & TearDown

        protected GameObject m_GameObject;
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
        #endregion

        protected static Type[] GenericRemoveComponent(
                [DisallowNull] GameObject gameObject,
                [DisallowNull] Type componentToRemove,
                [DisallowNull] Type[] componentsToAdd,
                [DisallowNull] Action<Component> removeMethod)
        {
            componentsToAdd.ToList().ForEach(type => gameObject.AddComponent(type));
            removeMethod(gameObject.GetComponent(componentToRemove));

            return gameObject.GetComponents(typeof(ITest)).Select(c => c.GetType()).ToArray();
        }
    }

    [TestOf(typeof(RemoveComponentUtils))]
    class RemoveComponentUtilsTests : RemoveComponent
    {
        static TestCaseData[] s_RemoveComponentTestCaseDatas =
        {
            new TestCaseData(typeof(Banana), new Type[] {typeof(Banana), typeof(AdditionalBanana)})
                .SetName("Removal of target component removes it's additional data")
                .Returns(Array.Empty<Type>()),
            new TestCaseData(typeof(Banana), new Type[] {typeof(Banana), typeof(AdditionalBanana), typeof(AdditionalBananaColor)})
                .SetName("Removal of target component removes all it's additional datas")
                .Returns(Array.Empty<Type>()),
            new TestCaseData(typeof(Apple), new Type[] {typeof(Banana), typeof(AdditionalBanana), typeof(Apple), typeof(AdditionalApple)})
                .SetName("Given multiple components, each with additional datas, the removal of the component only removes its additional datas")
                .Returns(new Type[] {typeof(Banana), typeof(AdditionalBanana) }),
        };

        [Test, TestCaseSource(nameof(s_RemoveComponentTestCaseDatas))]
        public Type[] RemoveComponentAndPropagateTheDeleteToAdditionalDatas([DisallowNull] Type componentToRemove, [DisallowNull] Type[] componentsToAdd)
        {
            return GenericRemoveComponent(m_GameObject, componentToRemove, componentsToAdd, RemoveComponentUtils.RemoveComponent);
        }
    }

    [TestOf(typeof(RemoveAdditionalDataUtils))]
    class RemoveAdditionalDataUtilsTests : RemoveComponent
    {
        static TestCaseData[] s_TryGetComponentsToRemoveTestCaseDatas =
         {
            new TestCaseData(typeof(AdditionalBanana))
                .Returns(new string[] {"Banana"})
                .SetName("For additional data targeting one component, return the targeted component (most common case)"),
            new TestCaseData(typeof(FruitBasket))
                .Returns(new string[] { "Apple", "Banana" })
                .SetName("For additional data targeting multiple components, return all the targeted components"),
            new TestCaseData(typeof(WaterMelon))
                .Returns(Array.Empty<string>())
                .SetName("For additional data targetting no component, return empty collection."),
        };

        [Test, TestCaseSource(nameof(s_TryGetComponentsToRemoveTestCaseDatas))]
        public string[] TryGetComponentsToRemove([DisallowNull] Type type)
        {
            string[] result = Array.Empty<string>();
            var additionalData = m_GameObject.AddComponent(type) as IAdditionalData;

            using (ListPool<Type>.Get(out var componentsToRemove))
            {
                if (RemoveAdditionalDataUtils.TryGetComponentsToRemove(additionalData, componentsToRemove, out var error))
                    result = componentsToRemove.Select(t => t.Name).ToArray();
            }
            return result;
        }

        static TestCaseData[] s_RemoveAdditionalDataComponentTestCaseDatas =
        {
            new TestCaseData(typeof(AdditionalBanana), new Type[] {typeof(Banana), typeof(AdditionalBanana) })
                .SetName("For single additional data, when removing it, the target component is deleted")
                .Returns(Array.Empty<Type>()),
            new TestCaseData(typeof(AdditionalBananaColor), new Type[] {typeof(Banana), typeof(AdditionalBanana), typeof(AdditionalBananaColor)})
                .SetName("For multiple additional datas, when removing one of them, target component is deleted, and the other additional data")
                .Returns(Array.Empty<Type>()),
           new TestCaseData(typeof(AdditionalBananaColor), new Type[] {typeof(Banana), typeof(Banana), typeof(AdditionalBanana), typeof(AdditionalBananaColor)})
                .SetName("For multiple additional component and datas, when removing one of them everything is removed")
                .Returns(Array.Empty<Type>()),
            new TestCaseData(typeof(AdditionalApple), new Type[] {typeof(Banana), typeof(AdditionalBanana), typeof(Apple), typeof(AdditionalApple)})
                .SetName("For multiple types of target component, when deleting an additional data, only the target component is being removed")
                .Returns(new Type[] {typeof(Banana), typeof(AdditionalBanana)})
        };

        [Test, TestCaseSource(nameof(s_RemoveAdditionalDataComponentTestCaseDatas))]
        [NUnit.Framework.Property("FogBugz", "1396805")]
        public Type[] RemoveAdditionalDataComponentAndPropagateToComponent([DisallowNull] Type componentToRemove, [DisallowNull] Type[] componentsToAdd)
        {
            return GenericRemoveComponent(m_GameObject, componentToRemove, componentsToAdd, c => RemoveAdditionalDataUtils.RemoveAdditionalData(c, false));
        }
    }
}
