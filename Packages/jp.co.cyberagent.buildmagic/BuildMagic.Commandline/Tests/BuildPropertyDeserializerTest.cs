// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagicEditor.Commandline.Internal;
using NUnit.Framework;

namespace BuildMagicEditor.Commandline.Tests
{
    public class SerializableTypeBuildPropertyDeserializerTest
    {
        [Test]
        public void SerializableTypeBuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new SerializableTypeBuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(SerializableClass)));
            Assert.IsFalse(deserializer.WillProcess(typeof(NonSerializableClass)));
        }

        [Test]
        public void SerializableTypeBuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new SerializableTypeBuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("{\"value\":123}", typeof(SerializableClass));

            Assert.AreEqual(typeof(SerializableClass), deserializedValue.GetType());
            Assert.AreEqual(123, ((SerializableClass)deserializedValue).value);
        }

        [Test]
        public void IntBuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new IntBuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(int)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void IntBuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new IntBuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("345", typeof(int));

            Assert.AreEqual(typeof(int), deserializedValue.GetType());
            Assert.AreEqual(345, (int)deserializedValue);
        }

        [Test]
        public void SingleBuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new SingleBuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(float)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void SingleBuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new SingleBuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("12.5", typeof(float));

            Assert.AreEqual(typeof(float), deserializedValue.GetType());
            Assert.AreEqual(12.5f, (float)deserializedValue);
        }

        [Test]
        public void EnumBuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new EnumBuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(EnumType)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void EnumBuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new EnumBuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("Two", typeof(EnumType));

            Assert.AreEqual(typeof(EnumType), deserializedValue.GetType());
            Assert.AreEqual(EnumType.Two, (EnumType)deserializedValue);
        }

        private enum EnumType
        {
            One,
            Two,
            Three
        }

        private class NonSerializableClass
        {
        }

        [Serializable]
        private class SerializableClass
        {
            public int value;
        }
    }
}
