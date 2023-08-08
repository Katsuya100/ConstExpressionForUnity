using NUnit.Framework;
using UnityEngine;

namespace Katuusagi.ConstExpressionForUnity.Tests
{
    public class ConstExpressionForUnityTest
    {
        [Test]
        public void String()
        {
            const string e1 = "hoge";
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Char()
        {
            const char e1 = 'a';
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void SByte()
        {
            const sbyte e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Byte()
        {
            const byte e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Short()
        {
            const short e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void UShort()
        {
            const ushort e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Int()
        {
            const int e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void UInt()
        {
            const uint e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Long()
        {
            const long e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void ULong()
        {
            const ulong e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Float()
        {
            const float e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Double()
        {
            const double e1 = 7;
            Assert.AreEqual(TestFunctions.Threw(e1), e1);
        }

        [Test]
        public void CharArray()
        {
            const char e1 = 'a';
            const char e2 = 'b';
            const char e3 = 'c';
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void SByteArray()
        {
            const sbyte e1 = 7;
            const sbyte e2 = 9;
            const sbyte e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void ByteArray()
        {
            const byte e1 = 7;
            const byte e2 = 9;
            const byte e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void ShortArray()
        {
            const short e1 = 7;
            const short e2 = 9;
            const short e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void UShortArray()
        {
            const ushort e1 = 7;
            const ushort e2 = 9;
            const ushort e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void IntArray()
        {
            const int e1 = 7;
            const int e2 = 9;
            const int e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void UIntArray()
        {
            const uint e1 = 7;
            const uint e2 = 9;
            const uint e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void LongArray()
        {
            const long e1 = 7;
            const long e2 = 9;
            const long e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void ULongArray()
        {
            const ulong e1 = 7;
            const ulong e2 = 9;
            const ulong e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void FloatArray()
        {
            const float e1 = 7;
            const float e2 = 9;
            const float e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void DoubleArray()
        {
            const double e1 = 7;
            const double e2 = 9;
            const double e3 = 11;
            var result = TestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void Structure()
        {
            const float x = 10;
            const float y = 20;
            const float z = 30;
            Assert.AreEqual(TestFunctions.MakeVector3(x, y, z), new Vector3(x, y, z));
        }

        [Test]
        public void CalcPrime()
        {
            const int n = 1000;
            Assert.AreEqual(TestFunctions.FindLargestPrime(n), TestFunctions.FindLargestPrimeRaw(n));
        }
    }
}
