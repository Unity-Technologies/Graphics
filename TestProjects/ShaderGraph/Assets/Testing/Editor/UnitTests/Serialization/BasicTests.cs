using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests.Serialization
{
    [TestFixture]
    public class BasicTests
    {
        class Person
        {
            public int age;
            public string name;
        }

        [Test]
        public void CanSerializePerson()
        {
            var person = new Person { age = 24, name = "Peter" };
            var json = JsonConvert.SerializeObject(person);
            Assert.AreEqual(@"{""age"":24,""name"":""Peter""}", json);
        }

        [Test]
        public void CanDeserializePerson()
        {
            var json = @"{""age"":24,""name"":""Peter""}";
            var person = JsonConvert.DeserializeObject<Person>(json);
            Assert.AreEqual(24, person.age);
            Assert.AreEqual("Peter", person.name);
        }

        [Test]
        public void CanSerializeAndDeserializePerson()
        {
            var originalPerson = new Person { age = 24, name = "Peter" };

            var json = JsonConvert.SerializeObject(originalPerson);
            var deserializedPerson = JsonConvert.DeserializeObject<Person>(json);

            Assert.AreEqual(originalPerson.age, deserializedPerson.age);
            Assert.AreEqual(originalPerson.name, deserializedPerson.name);
        }

    }
}
