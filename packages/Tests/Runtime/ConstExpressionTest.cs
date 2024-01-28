using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;

namespace Katuusagi.ConstExpressionForUnity.Tests
{
    public class ConstExpressionTest
    {
        [Test]
        public void String()
        {
            const string e1 = "hoge";
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Char()
        {
            const char e1 = 'a';
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);

            foreach(var v in ConstExpressionTestFunctions.MakeArray(10, 20, 30))
            {
                v.ToString();
            }
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Bool()
        {
            const bool e1 = true;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void SByte()
        {
            const sbyte e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Byte()
        {
            const byte e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Short()
        {
            const short e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void UShort()
        {
            const ushort e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Int()
        {
            const int e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void UInt()
        {
            const uint e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Long()
        {
            const long e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void ULong()
        {
            const ulong e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Float()
        {
            const float e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Double()
        {
            const double e1 = 7;
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Enum()
        {
            Assert.AreEqual(ConstExpressionTestFunctions.Threw(DayOfWeek.Sunday), DayOfWeek.Sunday);
        }

        [Test]
        public void StringArray()
        {
            const string e1 = "abc";
            const string e2 = "def";
            const string e3 = "ghi";
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void CharArray()
        {
            const char e1 = 'a';
            const char e2 = 'b';
            const char e3 = 'c';
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void BoolArray()
        {
            const bool e1 = true;
            const bool e2 = false;
            const bool e3 = true;
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = ConstExpressionTestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void EnumArray()
        {
            var result = ConstExpressionTestFunctions.MakeArray(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday);
            Assert.AreEqual(result[0], DayOfWeek.Monday);
            Assert.AreEqual(result[1], DayOfWeek.Tuesday);
            Assert.AreEqual(result[2], DayOfWeek.Thursday);
        }

        [Test]
        public void GenericStructure()
        {
            var result = ConstExpressionTestFunctions.MakeGeneric(10.0f, 20.0f, 30.0f);
            Assert.AreEqual(result.value, new Vector3(10.0f, 20.0f, 30.0f));
        }

        [Test]
        public void Structure()
        {
            const float x = 10;
            const float y = 20;
            const float z = 30;
            Assert.AreEqual(ConstExpressionTestFunctions.MakeVector3(x, y, z), new Vector3(x, y, z));
        }

        [Test]
        public void CalcPrime()
        {
            const int n = 1000;
            Assert.AreEqual(ConstExpressionTestFunctions.FindLargestPrime(n), ConstExpressionTestFunctions.FindLargestPrimeRaw(n));
        }

        [Test]
        public void Jump()
        {
            // ConstExpression以降にジャンプ系命令を生成する
            try
            {
                ConstExpressionTestFunctions.MakeVector3(1, 2, 3);
            }
            finally
            {
                string.Format("");
            }

            string.Format("");

            foreach (int v in "hogehoge".ToList())
            {
                string.Format("");
            }

            string.Format("");

            // ジャンプ直後の箇所に展開されているパターン
            try
            {
                ConstExpressionTestFunctions.MakeVector3(1, 2, 3);
            }
            finally
            {
                ConstExpressionTestFunctions.MakeVector3(1, 2, 3);
            }

            ConstExpressionTestFunctions.MakeVector3(1, 2, 3);

            foreach (int v in ConstExpressionTestFunctions.MakeArray(1, 2, 3))
            {
                ConstExpressionTestFunctions.FindLargestPrime(v);
            }

            ConstExpressionTestFunctions.MakeVector3(1, 2, 3);
        }

        [Test]
        public void ExpressionChain()
        {
            var z = ConstExpressionTestFunctions.GetZ(ConstExpressionTestFunctions.MakeVector3(1f, 2.5f, 3.45f));
            Assert.AreEqual(z, 3.45f);
            var a = ConstExpressionTestFunctions.GetArrayElement(ConstExpressionTestFunctions.MakeArray(10, 20, 30), 1);
            Assert.AreEqual(a, 20);
        }
    }
}
