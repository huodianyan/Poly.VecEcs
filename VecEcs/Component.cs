using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Poly.VecEcs
{
    public interface IComponentArray
    {
        byte ComponentId { get; }
        Type ComponentType { get; }

        void Resize(int newSize);
        bool Has(int entity);
        void Remove(int entity);
        object Get(int entity);
        //void Set(int entity, object dataRaw);
    }
    public class BufferArray<T> : ComponentArray<DynamicBuffer<T>> where T : struct
    {
        internal BufferArray(World world, byte id, int entityCapacity) : base(world, id, entityCapacity)
        {
        }
        protected override DynamicBuffer<T> CreateComp()
        {
            return new DynamicBuffer<T>(OnChanged, 4);
        }

        internal void OnChanged(EBufferOperation op, int index, T item)
        {
            Console.WriteLine($"{GetType().Name}.OnChanged: {op},{index},{item}");
        }
    }
    public class ComponentArray<T> : IComponentArray where T : struct
    {
        protected readonly Type compType;
        protected readonly World world;
        protected readonly byte compId;
        //private readonly MethodInfo bufferCreateMethod;
        // 1-based index.
        protected T[] comps;
        protected int compsCount;
        protected int[] entityCompIndexs;
        protected int[] recycledCompIndexs;
        protected int recycledCompCount;

        public byte ComponentId => compId;
        public Type ComponentType => compType;
        //public T[] Comps => comps;

        internal ComponentArray(World world, byte id, int entityCapacity)
        {
            compType = typeof(T);
            //isBuffer = compType.GetGenericTypeDefinition() == typeof(DynamicBuffer<>);
            //if(isBuffer)
            //    bufferCreateMethod = compType.GetMethod("Create",BindingFlags.Instance | BindingFlags.NonPublic);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int entity, T comp)
        {
            ref var comRef = ref Add(entity);
            comRef = comp;
        }
        protected virtual T CreateComp()
        {
            return default;
        }

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
            //if (isBuffer)
            //{
            //    var comp = default(T);
            //    bufferCreateMethod.Invoke(comp, new object[] { 4 });
            //    comps[idx] = comp;
            //}
            comps[idx] = CreateComp();
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
            if (comps[compIndex] is IDisposable disposable) disposable.Dispose();
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
        object IComponentArray.Get(int entity) => Get(entity);
        //void IEcsComponentArray.Set(int entity, object dataRaw)
        //{
        //    if (dataRaw == null || dataRaw.GetType() != compType) { throw new Exception($"Invalid component data, valid \"{typeof (T).Name}\" instance required."); }
        //    if (entityCompIndexs[entity] <= 0) { throw new Exception($"Component \"{typeof(T).Name}\" not attached to entity."); }
        //    comps[entityCompIndexs[entity]] = (T)dataRaw;
        //}
    }
}
