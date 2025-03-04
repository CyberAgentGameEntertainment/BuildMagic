// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagicEditor.Commandline.Internal;
using NUnit.Framework;

namespace BuildMagicEditor.Commandline.Tests
{
    public partial class SerializableTypeBuildPropertyDeserializerTest
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

        [Test]
        public void BoolBuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new BoolBuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(bool)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void BoolBuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new BoolBuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("true", typeof(bool));

            Assert.AreEqual(typeof(bool), deserializedValue.GetType());
            Assert.AreEqual(true, (bool)deserializedValue);
            Assert.AreEqual(false, (bool)deserializer.Deserialize("false", typeof(bool)));
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
