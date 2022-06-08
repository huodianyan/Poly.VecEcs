using Poly.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Poly.VecEcs
{
    //internal interface IDynamicBuffer
    //{
    //}
    public enum EBufferOperation : byte
    {
        OP_ADD,
        OP_CLEAR,
        OP_INSERT,
        //OP_REMOVE,
        OP_REMOVEAT,
        OP_SET,
        OP_DIRTY
    };

    public struct DynamicBuffer<T> : IFastList<T> where T : struct
    {
        private T[] items;
        private int length;
        private readonly Action<EBufferOperation, int, T> changedCallback;

        public int Capacity => items.Length;
        public bool IsCreated => items != null;
        public bool IsEmpty => length == 0;
        public int Length
        {
            get => length;
            set
            {
                if (value > items.Length) EnsureCapacity(value);
                length = value;
            }
        }
        public T this[int index]
        {
            get => index >= items.Length ? default : items[index];
            set
            {
                //if (index > items.Length) EnsureCapacity(index);
                if (index >= length) Length = index + 1;
                items[index] = value;
            }
        }

        internal DynamicBuffer(Action<EBufferOperation, int, T> changedCallback, int capacity)
        {
            items = ArrayPool<T>.Shared.Rent(capacity);
            length = 0;
            this.changedCallback = changedCallback;
        }
        public void Dispose()
        {
            if (items == null) return;
            ArrayPool<T>.Shared.Return(items);
            items = null;
            length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add() => Add(default);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (length == items.Length) EnsureCapacity(length + 1);//Array.Resize(ref items, length << 1);
            items[length++] = item;
            changedCallback?.Invoke(EBufferOperation.OP_ADD, length - 1, item);
            //Console.WriteLine($"FastList.Add: {count}, {item.ToString()}");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(FastArray<T> range)
        {
            var newCount = range.Length + length;
            if (newCount > items.Length) EnsureCapacity(newCount);
            for (int i = 0, j = range.Length; i < j; i++)
                items[length++] = range[i];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (length > 0) Array.Clear(items, 0, length);
            length = 0;
            changedCallback?.Invoke(EBufferOperation.OP_CLEAR, 0, default);
        }
        public ref T ElementAt(int index) => ref items[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int capacity)
        {
            if (capacity <= items.Length) return;
            var array = ArrayPool<T>.Shared.Rent(capacity);
            System.Array.Copy(items, array, length);
            var oldArray = items;
            items = array;
            ArrayPool<T>.Shared.Return(oldArray);
        }
        public FastArray<T>.Enumerator GetEnumerator() => new FastArray<T>.Enumerator(items, 0, length);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            if (length == items.Length) EnsureCapacity(length + 1);
            for (int i = length - 1; i >= index; i--)
                items[i + 1] = items[i];
            items[index] = item;
            length++;
            changedCallback?.Invoke(EBufferOperation.OP_INSERT, index, item);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
            items[index] = default;
            if (--length > index)
                for (int i = index; i < length; i++)
                    items[i] = items[i + 1];
            changedCallback?.Invoke(EBufferOperation.OP_REMOVEAT, index, default);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwapBack(int index)
        {
            if (--length > index) items[index] = items[length];
            items[length] = default;
        }
    }
}
