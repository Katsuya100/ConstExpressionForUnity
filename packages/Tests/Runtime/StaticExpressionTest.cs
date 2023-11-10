using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Katuusagi.ConstExpressionForUnity.Tests
{
    public class StaticExpressionTest
    {
        [Test]
        public void Type()
        {
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(typeof(int)), typeof(int));
        }

        [Test]
        public void String()
        {
            const string e1 = "hoge";
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Char()
        {
            const char e1 = 'a';
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);

            foreach (var v in StaticExpressionTestFunctions.MakeArray(10, 20, 30))
            {
                v.ToString();
            }
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Bool()
        {
            const bool e1 = true;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void SByte()
        {
            const sbyte e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Byte()
        {
            const byte e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Short()
        {
            const short e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void UShort()
        {
            const ushort e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Int()
        {
            const int e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void UInt()
        {
            const uint e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Long()
        {
            const long e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void ULong()
        {
            const ulong e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Float()
        {
            const float e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Double()
        {
            const double e1 = 7;
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(e1), e1);
        }

        [Test]
        public void Enum()
        {
            Assert.AreEqual(StaticExpressionTestFunctions.Threw(DayOfWeek.Sunday), DayOfWeek.Sunday);
        }

        [Test]
        public void StringArray()
        {
            const string e1 = "abc";
            const string e2 = "def";
            const string e3 = "ghi";
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
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
            var result = StaticExpressionTestFunctions.MakeArray(e1, e2, e3);
            Assert.AreEqual(result[0], e1);
            Assert.AreEqual(result[1], e2);
            Assert.AreEqual(result[2], e3);
        }

        [Test]
        public void EnumArray()
        {
            var result = StaticExpressionTestFunctions.MakeArray(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday);
            Assert.AreEqual(result[0], DayOfWeek.Monday);
            Assert.AreEqual(result[1], DayOfWeek.Tuesday);
            Assert.AreEqual(result[2], DayOfWeek.Thursday);
        }

        [Test]
        public void GenericStructure()
        {
            var result = StaticExpressionTestFunctions.MakeGeneric(100);
            Assert.AreEqual(result.value, 100);
        }

        [Test]
        public void Structure()
        {
            const float x = 10;
            const float y = 20;
            const float z = 30;
            Assert.AreEqual(StaticExpressionTestFunctions.MakeVector3(x, y, z), new Vector3(x, y, z));
        }

        [Test]
        public void CalcPrime()
        {
            const int n = 1000;
            Assert.AreEqual(StaticExpressionTestFunctions.FindLargestPrime(n), StaticExpressionTestFunctions.FindLargestPrimeRaw(n));
        }

        [Test]
        public void Jump()
        {
            // ConstExpression以降にジャンプ系命令を生成する
            try
            {
                StaticExpressionTestFunctions.MakeVector3(1, 2, 3);
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
                StaticExpressionTestFunctions.MakeVector3(1, 2, 3);
            }
            finally
            {
                StaticExpressionTestFunctions.MakeVector3(1, 2, 3);
            }

            StaticExpressionTestFunctions.MakeVector3(1, 2, 3);

            foreach (int v in StaticExpressionTestFunctions.MakeArray(1, 2, 3))
            {
                StaticExpressionTestFunctions.FindLargestPrime(v);
            }

            StaticExpressionTestFunctions.MakeVector3(1, 2, 3);
        }

        [Test]
        public void ExpressionChain()
        {
            var z = StaticExpressionTestFunctions.GetZ(StaticExpressionTestFunctions.MakeVector3(1f, 2.5f, 3.45f));
            Assert.AreEqual(z, 3.45f);
            var arr = ConstExpressionTestFunctions.MakeArray(10, 20, 30);
            var a = StaticExpressionTestFunctions.GetArrayElement(arr, 1);
            Assert.AreEqual(a, 20);
            var t = StaticExpressionTestFunctions.Threw(StaticExpressionTestFunctions.Threw(typeof(int)));
            Assert.AreEqual(t, typeof(int));
            var c = StaticExpressionTestFunctions.GetConstructor(StaticExpressionTestFunctions.GetNestedType(typeof(StaticExpressionTestFunctions), "ReflectionCheck"));
            Assert.AreEqual(c.ReflectedType, typeof(StaticExpressionTestFunctions.ReflectionCheck));
        }

        [Test]
        public void Reflection()
        {
            var t = StaticExpressionTestFunctions.GetNestedType(typeof(StaticExpressionTestFunctions), "ReflectionCheck");
            Assert.AreEqual(t.Name, "ReflectionCheck");

            var c = StaticExpressionTestFunctions.GetConstructor(typeof(StaticExpressionTestFunctions.ReflectionCheck));
            Assert.AreEqual(c.ReflectedType, t);

            var f = StaticExpressionTestFunctions.GetField(typeof(StaticExpressionTestFunctions.ReflectionCheck), "A");
            Assert.AreEqual(f.Name, "A");

            var p = StaticExpressionTestFunctions.GetProperty(typeof(StaticExpressionTestFunctions.ReflectionCheck), "B");
            Assert.AreEqual(p.Name, "B");

            var e = StaticExpressionTestFunctions.GetEvent(typeof(StaticExpressionTestFunctions.ReflectionCheck), "C");
            Assert.AreEqual(e.Name, "C");

            var m = StaticExpressionTestFunctions.GetMethod(typeof(StaticExpressionTestFunctions.ReflectionCheck), "D");
            Assert.AreEqual(m.Name, "D");

            var member = StaticExpressionTestFunctions.GetMember(typeof(StaticExpressionTestFunctions.ReflectionCheck), "D");
            Assert.AreEqual(member.Name, "D");
        }

        [Test]
        public void GenericMethod()
        {
            var member = StaticExpressionTestFunctions.GetMember<StaticExpressionTestFunctions.ReflectionCheck>("D");
            Assert.AreEqual(member.Name, "D");
            MemberInfo Wrap<T>()
            {
                return StaticExpressionTestFunctions.GetMember<T>("D");
            }

            member = Wrap<StaticExpressionTestFunctions.ReflectionCheck>();
            Assert.AreEqual(member.Name, "D");
        }
    }
}
