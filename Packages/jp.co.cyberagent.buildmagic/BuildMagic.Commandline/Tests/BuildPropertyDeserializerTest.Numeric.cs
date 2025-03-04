// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagicEditor.Commandline.Internal;
using NUnit.Framework;

namespace BuildMagicEditor.Commandline.Tests
{
    partial class SerializableTypeBuildPropertyDeserializerTest
    {
        [Test]
        public void ByteBuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new ByteBuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(byte)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void ByteBuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new ByteBuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("123", typeof(byte));

            Assert.AreEqual(typeof(byte), deserializedValue.GetType());
            Assert.AreEqual((byte)123, (byte)deserializedValue);
        }

        [Test]
        public void SByteBuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new SByteBuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(sbyte)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void SByteBuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new SByteBuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("-123", typeof(sbyte));

            Assert.AreEqual(typeof(sbyte), deserializedValue.GetType());
            Assert.AreEqual((sbyte)-123, (sbyte)deserializedValue);
        }

        [Test]
        public void UInt16BuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new UInt16BuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(ushort)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void UInt16BuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new UInt16BuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("123", typeof(ushort));

            Assert.AreEqual(typeof(ushort), deserializedValue.GetType());
            Assert.AreEqual((ushort)123, (ushort)deserializedValue);
        }

        [Test]
        public void Int16BuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new Int16BuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(short)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void Int16BuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new Int16BuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("-123", typeof(short));

            Assert.AreEqual(typeof(short), deserializedValue.GetType());
            Assert.AreEqual((short)-123, (short)deserializedValue);
        }

        [Test]
        public void UInt32BuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new UInt32BuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(uint)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void UInt32BuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new UInt32BuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("123", typeof(uint));

            Assert.AreEqual(typeof(uint), deserializedValue.GetType());
            Assert.AreEqual((uint)123, (uint)deserializedValue);
        }

        [Test]
        public void Int32BuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new Int32BuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(int)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void Int32BuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new Int32BuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("-123", typeof(int));

            Assert.AreEqual(typeof(int), deserializedValue.GetType());
            Assert.AreEqual((int)-123, (int)deserializedValue);
        }

        [Test]
        public void UInt64BuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new UInt64BuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(ulong)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void UInt64BuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new UInt64BuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("123", typeof(ulong));

            Assert.AreEqual(typeof(ulong), deserializedValue.GetType());
            Assert.AreEqual((ulong)123, (ulong)deserializedValue);
        }

        [Test]
        public void Int64BuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new Int64BuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(long)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void Int64BuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new Int64BuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("-123", typeof(long));

            Assert.AreEqual(typeof(long), deserializedValue.GetType());
            Assert.AreEqual((long)-123, (long)deserializedValue);
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
            var deserializedValue = deserializer.Deserialize("12.3", typeof(float));

            Assert.AreEqual(typeof(float), deserializedValue.GetType());
            Assert.AreEqual((float)12.3, (float)deserializedValue);
        }

        [Test]
        public void DoubleBuildPropertyDeserializer_WillProcess()
        {
            var deserializer = new DoubleBuildPropertyDeserializer();
            Assert.IsTrue(deserializer.WillProcess(typeof(double)));
            Assert.IsFalse(deserializer.WillProcess(typeof(string)));
        }

        [Test]
        public void DoubleBuildPropertyDeserializer_Deserialize()
        {
            var deserializer = new DoubleBuildPropertyDeserializer();
            var deserializedValue = deserializer.Deserialize("12.3", typeof(double));

            Assert.AreEqual(typeof(double), deserializedValue.GetType());
            Assert.AreEqual((double)12.3, (double)deserializedValue);
        }

    }
}
