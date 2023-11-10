using Katuusagi.ILPostProcessorCommon;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Katuusagi.ConstExpressionForUnity.Tests
{
    public static class StaticExpressionTestFunctions
    {
        public class ReflectionCheck
        {
            public int A;
            public int B => A;
            public event Action<int> C;
            public ReflectionCheck() { }
            public void D(int a)
            {
                C(a);
            }
        }

        [StaticExpression]
        public static Type Threw(Type t)
        {
            return t;
        }

        [StaticExpression]
        public static string Threw(string value)
        {
            return value;
        }

        [StaticExpression]
        public static char Threw(char value)
        {
            return value;
        }

        [StaticExpression]
        public static bool Threw(bool value)
        {
            return value;
        }

        [StaticExpression]
        public static sbyte Threw(sbyte value)
        {
            return value;
        }

        [StaticExpression]
        public static byte Threw(byte value)
        {
            return value;
        }

        [StaticExpression]
        public static short Threw(short value)
        {
            return value;
        }

        [StaticExpression]
        public static ushort Threw(ushort value)
        {
            return value;
        }

        [StaticExpression]
        public static int Threw(int value)
        {
            return value;
        }

        [StaticExpression]
        public static uint Threw(uint value)
        {
            return value;
        }

        [StaticExpression]
        public static long Threw(long value)
        {
            return value;
        }

        [StaticExpression]
        public static ulong Threw(ulong value)
        {
            return value;
        }

        [StaticExpression]
        public static float Threw(float value)
        {
            return value;
        }

        [StaticExpression]
        public static double Threw(double value)
        {
            return value;
        }

        [StaticExpression]
        public static DayOfWeek Threw(DayOfWeek value)
        {
            return value;
        }

        [StaticExpression]
        public static ReadOnlyArray<string> MakeArray(string e1, string e2, string e3)
        {
            return new string[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<char> MakeArray(char e1, char e2, char e3)
        {
            return new char[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<bool> MakeArray(bool e1, bool e2, bool e3)
        {
            return new bool[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<sbyte> MakeArray(sbyte e1, sbyte e2, sbyte e3)
        {
            return new sbyte[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<byte> MakeArray(byte e1, byte e2, byte e3)
        {
            return new byte[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<short> MakeArray(short e1, short e2, short e3)
        {
            return new short[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<ushort> MakeArray(ushort e1, ushort e2, ushort e3)
        {
            return new ushort[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<int> MakeArray(int e1, int e2, int e3)
        {
            return new int[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<uint> MakeArray(uint e1, uint e2, uint e3)
        {
            return new uint[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<long> MakeArray(long e1, long e2, long e3)
        {
            return new long[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<ulong> MakeArray(ulong e1, ulong e2, ulong e3)
        {
            return new ulong[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<float> MakeArray(float e1, float e2, float e3)
        {
            return new float[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<double> MakeArray(double e1, double e2, double e3)
        {
            return new double[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static ReadOnlyArray<DayOfWeek> MakeArray(DayOfWeek e1, DayOfWeek e2, DayOfWeek e3)
        {
            return new DayOfWeek[] { e1, e2, e3 };
        }

        [StaticExpression]
        public static PackedStruct<int> MakeGeneric(int value)
        {
            return new PackedStruct<int>()
            {
                value = value,
            };
        }

        [StaticExpression]
        public static Vector3 MakeVector3(float x, float y, float z)
        {
            return new Vector3(x, y, z);
        }

        [StaticExpression(CalculationFailedWarning = false)]
        public static int FindLargestPrime(int n)
        {
            return FindLargestPrimeRaw(n);
        }

        [StaticExpression]
        public static int GetArrayElement(ReadOnlyArray<int> array, int index)
        {
            return array[index];
        }

        [StaticExpression]
        public static float GetZ(Vector3 v)
        {
            return v.z;
        }

        [StaticExpression(CalculationFailedWarning = false)]
        public static MemberInfo GetMember<T>(string name)
        {
            return typeof(T).GetMember(name).FirstOrDefault();
        }

        [StaticExpression]
        public static MemberInfo GetMember(Type t, string name)
        {
            return t.GetMember(name).FirstOrDefault();
        }

        [StaticExpression]
        public static ConstructorInfo GetConstructor(Type t)
        {
            return t.GetConstructors().FirstOrDefault();
        }

        [StaticExpression]
        public static FieldInfo GetField(Type t, string name)
        {
            return t.GetField(name);
        }

        [StaticExpression]
        public static PropertyInfo GetProperty(Type t, string name)
        {
            return t.GetProperty(name);
        }

        [StaticExpression]
        public static MethodInfo GetMethod(Type t, string name)
        {
            return t.GetMethod(name);
        }

        [StaticExpression]
        public static EventInfo GetEvent(Type t, string name)
        {
            return t.GetEvent(name);
        }

        [StaticExpression]
        public static TypeInfo GetNestedType(Type t, string name)
        {
            return (TypeInfo)t.GetNestedType(name);
        }

        public static int FindLargestPrimeRaw(int n)
        {
            bool[] isPrime = new bool[n + 1];
            for (int i = 2; i <= n; i++)
            {
                isPrime[i] = true;
            }

            for (int p = 2; p * p <= n; p++)
            {
                if (!isPrime[p])
                {
                    continue;
                }

                for (int i = p * p; i <= n; i += p)
                {
                    isPrime[i] = false;
                }
            }

            for (int i = n; i >= 2; i--)
            {
                if (isPrime[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
