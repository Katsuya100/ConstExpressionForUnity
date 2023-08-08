using System;
using System.Collections;
using System.Collections.Generic;

namespace Katuusagi.ConstExpressionForUnity
{
    public class ReadOnlyArray<T> : IReadOnlyList<T>
    {
        private T[] _array;

        private ReadOnlyArray()
        {
        }

        public T this[int index] => _array[index];

        public int Count => _array.Length;

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)_array).GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return _array;
        }

        public static implicit operator ReadOnlySpan<T>(ReadOnlyArray<T> b)
        {
            return b._array;
        }

        public static implicit operator ReadOnlyArray<T>(T[] array)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            var instance = new ReadOnlyArray<T>();
            instance._array = array;
            return instance;
        }
    }
}
