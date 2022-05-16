using System;
using System.Runtime.CompilerServices;

namespace Poly.VecEcs
{
    #region Component
    public interface IEcsComponentArray
    {
        byte ComponentId { get; }
        Type ComponentType { get; }

        void Resize(int newSize);
        bool Has(int entity);
        void Remove(int entity);
        object Get(int entity);
        //void Set(int entity, object dataRaw);
    }
    //public interface IComponentArray<T> : IEcsComponentArray// where T : struct
    //{
    //    ref T Add(int entity);
    //    new ref T Get(int entity);
    //    //new void Set(int entity, T dataRaw);
    //}
    public class EcsComponentArray<T> : IEcsComponentArray where T : struct
    {
        private readonly Type compType;
        private readonly EcsWorld world;
        private readonly byte compId;
        // 1-based index.
        private T[] comps;
        private int compsCount;
        private int[] entityCompIndexs;
        private int[] recycledCompIndexs;
        private int recycledCompCount;

        public byte ComponentId => compId;
        public Type ComponentType => compType;
        //public T[] Comps => comps;

        internal EcsComponentArray(EcsWorld world, byte id, int entityCapacity)
        {
            compType = typeof(T);
            this.world = world;
            compId = id;
            comps = new T[entityCapacity / 4 + 1];
            entityCompIndexs = new int[entityCapacity];
            compsCount = 1;
            recycledCompIndexs = new int[entityCapacity / 8];
            recycledCompCount = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int capacity) => Array.Resize(ref entityCompIndexs, capacity);
        public ref T Add(int entity)
        {
            if (!world.IsEntityValid(entity)) { throw new Exception("Cant touch destroyed entity."); }
            if (entityCompIndexs[entity] > 0) { throw new Exception($"Component \"{typeof(T).Name}\" already attached to entity."); }
            int idx;
            if (recycledCompCount > 0)
                idx = recycledCompIndexs[--recycledCompCount];
            else
            {
                idx = compsCount;
                if (compsCount == comps.Length) Array.Resize(ref comps, compsCount << 1);
                compsCount++;
            }
            entityCompIndexs[entity] = idx;
            world.OnComponentAdded(entity, compId);
            return ref comps[idx];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entity)
        {
            if (!world.IsEntityValid(entity)) { throw new Exception("Cant touch destroyed entity."); }
            if (entityCompIndexs[entity] == 0) { throw new Exception($"Cant get \"{typeof(T).Name}\" component - not attached."); }
            return ref comps[entityCompIndexs[entity]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entity)
        {
            if (!world.IsEntityValid(entity)) { throw new Exception("Cant touch destroyed entity."); }
            return entityCompIndexs[entity] > 0;
        }
        public void Remove(int entity)
        {
            if (!world.IsEntityValid(entity)) { throw new Exception("Cant touch destroyed entity."); }
            ref var compIndex = ref entityCompIndexs[entity];
            if (compIndex == 0) return;
            if (recycledCompCount == recycledCompIndexs.Length) Array.Resize(ref recycledCompIndexs, recycledCompCount << 1);
            recycledCompIndexs[recycledCompCount++] = compIndex;
            comps[compIndex] = default;
            compIndex = 0;
            world.OnComponentRemoved(entity, compId);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int entity, T comp)
        {
            if (entityCompIndexs[entity] <= 0) { throw new Exception($"Component \"{typeof(T).Name}\" not attached to entity."); }
            comps[entityCompIndexs[entity]] = comp;
        }
        object IEcsComponentArray.Get(int entity) => Get(entity);
        //void IEcsComponentArray.Set(int entity, object dataRaw)
        //{
        //    if (dataRaw == null || dataRaw.GetType() != compType) { throw new Exception($"Invalid component data, valid \"{typeof (T).Name}\" instance required."); }
        //    if (entityCompIndexs[entity] <= 0) { throw new Exception($"Component \"{typeof(T).Name}\" not attached to entity."); }
        //    comps[entityCompIndexs[entity]] = (T)dataRaw;
        //}
    }
    #endregion
}
