using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Poly.VecEcs
{
    public class World
    {
        //world
        private readonly string id;
        private readonly object shared;
        private bool isInited;
        private bool isDestroyed;
        //Entity
        internal EntityInternal[] entities;
        private int entityCount;
        private int[] recycledEntities;
        private int recycledEntityCount;
        //Component
        internal IComponentArray[] compArrays;
        private byte compArrayCount;
        private readonly Dictionary<Type, IComponentArray> compArrayDict;
        //Query
        private readonly Dictionary<int, Query> queryDict;
        private readonly List<Query> queryList;
        private List<Query>[] allQueryLists;
        private List<Query>[] anyQueryLists;
        private List<Query>[] noneQueryLists;
        private QueryDesc[] recycledQueryDescs;
        private int recycledQueryDescCount;
        //System
        private Dictionary<Type, ISystem> systemDict;
        private List<ISystem> systemList;
        //private IRunSystem[] runSystems;
        //private int runSystemCount;

        public string Id => id;
        public int EntityCount => entityCount - recycledEntityCount;
        public int WorldSize => entities.Length;
        public IList<ISystem> SystemList => systemList;
        public IList<Query> QueryList => queryList;
        public IDictionary<Type, IComponentArray> CompArrayDict => compArrayDict;
        //public bool IsAlive => !isDestroyed;

        public event Action<int> WorldResizedEvent;
        public event Action<int> EntityCreatedEvent;
        public event Action<int> EntityDestroyedEvent;
        public event Action<int, int> EntityComponentAddedEvent;
        public event Action<int, int> EntityComponentRemovedEvent;
        //public event Action<int, int> EntityComponentChangedEvent;
        public event Action<Query> QueryCreatedEvent;

        public World(string id = "Default", object shared = null, in Config cfg = default)
        {
            //world
            this.id = id;
            this.shared = shared;
            // entities.
            var capacity = cfg.EntityCapacity > 0 ? cfg.EntityCapacity : Config.EntityCapacityDefault;
            entities = new EntityInternal[capacity];
            capacity = cfg.RecycledEntityCapacity > 0 ? cfg.RecycledEntityCapacity : Config.RecycledEntityCapacityDefault;
            recycledEntities = new int[capacity];
            entityCount = 0;
            recycledEntityCount = 0;
            // component
            //capacity = cfg.ComponentCapacity > 0 ? cfg.ComponentCapacity : Config.ComponentCapacityDefault;
            compArrays = new IComponentArray[256];
            compArrayDict = new Dictionary<Type, IComponentArray>(256);
            allQueryLists = new List<Query>[256];
            anyQueryLists = new List<Query>[256];
            noneQueryLists = new List<Query>[256];
            //compTypeSize = cfg.PoolDenseSize > 0 ? cfg.PoolDenseSize : Config.PoolDenseSizeDefault;
            //recycledCompSize = cfg.PoolRecycledSize > 0 ? cfg.PoolRecycledSize : Config.PoolRecycledSizeDefault;
            compArrayCount = 0;
            // query
            capacity = cfg.QueryCapacity > 0 ? cfg.QueryCapacity : Config.QueryCapacityDefault;
            queryDict = new Dictionary<int, Query>(capacity);
            queryList = new List<Query>(capacity);
            recycledQueryDescs = new QueryDesc[64];
            recycledQueryDescCount = 0;
            //system
            //capacity = cfg.SystemCapacity > 0 ? cfg.SystemCapacity : Config.SystemCapacityDefault;
            systemDict = new Dictionary<Type, ISystem>();
            systemList = new List<ISystem>();
            //runSystems = new IRunSystem[capacity];
            //runSystemCount = 0;
            //isDestroyed = false;
        }
        public void Destroy()
        {
            isDestroyed = true;
            //Entity
            for (var i = entityCount - 1; i >= 0; i--)
            {
                ref var entityData = ref entities[i];
                if (entityData.ComponentCount > 0)
                    DestroyEntity(i);
            }
            //Component
            compArrays = Array.Empty<IComponentArray>();
            compArrayDict.Clear();
            //Query
            queryDict.Clear();
            queryList.Clear();
            allQueryLists = Array.Empty<List<Query>>();
            anyQueryLists = Array.Empty<List<Query>>();
            noneQueryLists = Array.Empty<List<Query>>();
            compArrayCount = 0;
            recycledQueryDescs = null;
            //System
            systemDict.Clear();
            systemList.Clear();
        }
        public void Init()
        {
            foreach (var system in systemList)
                system.Init(this);
            isInited = true;
        }
        public void Update()
        {
            var count = systemList.Count;
            for (int i = 0; i < count; i++)
                systemList[i].Update();
        }
        public T GetShared<T>()
        {
            return (T)shared;
        }

        #region Entity
        public int CreateEntity()
        {
            int entity;
            if (recycledEntityCount > 0)
            {
                entity = recycledEntities[--recycledEntityCount];
                ref var entityData = ref entities[entity];
                entityData.Version = (short)-entityData.Version;
            }
            else
            {
                // new entity.
                if (entityCount == entities.Length)
                {
                    // resize entities and component pools.
                    var newSize = entityCount << 1;
                    Array.Resize(ref entities, newSize);
                    for (int i = 0, iMax = compArrayCount; i < iMax; i++)
                        compArrays[i].Resize(newSize);
                    for (int i = 0, iMax = queryList.Count; i < iMax; i++)
                        queryList[i].Resize(newSize);
                    WorldResizedEvent?.Invoke(newSize);
                }
                entity = entityCount++;
                entities[entity].Version = 1;
            }
            EntityCreatedEvent?.Invoke(entity);
            return entity;
        }
        public void DestroyEntity(int entity)
        {
            if (entity < 0 || entity >= entityCount) { throw new Exception("Cant touch destroyed entity."); }
            ref var entityData = ref entities[entity];
            if (entityData.Version < 0)
                return;
            // kill components.
            if (entityData.ComponentCount > 0)
            {
                var idx = 0;
                while (entityData.ComponentCount > 0 && idx < compArrayCount)
                {
                    for (; idx < compArrayCount; idx++)
                    {
                        if (compArrays[idx].Has(entity))
                        {
                            compArrays[idx++].Remove(entity);
                            break;
                        }
                    }
                }
                if (entityData.ComponentCount != 0) { throw new Exception($"Invalid components count on entity {entity} => {entityData.ComponentCount}."); }
                return;
            }
            entityData.Version = (short)(entityData.Version == short.MaxValue ? -1 : -(entityData.Version + 1));
            if (recycledEntityCount == recycledEntities.Length)
                Array.Resize(ref recycledEntities, recycledEntityCount << 1);
            recycledEntities[recycledEntityCount++] = entity;
            EntityDestroyedEvent?.Invoke(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityValid(int entity)
        {
            return entity >= 0 && entity < entityCount && entities[entity].Version > 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEntityComponentCount(int entity)
        {
            return entities[entity].ComponentCount;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetEntityVersion(int entity)
        {
            return entities[entity].Version;
        }
        public int GetAllEntities(ref int[] entities)
        {
            var count = entityCount - recycledEntityCount;
            if (entities == null || entities.Length < count)
                entities = new int[count];
            var id = 0;
            for (int i = 0, iMax = entityCount; i < iMax; i++)
            {
                ref var entityData = ref this.entities[i];
                // should we skip empty entities here?
                if (entityData.Version > 0 && entityData.ComponentCount >= 0)
                    entities[id++] = i;
            }
            return count;
        }
        public int GetComponents(int entity, ref object[] list)
        {
            var compCount = entities[entity].ComponentCount;
            if (compCount == 0) { return 0; }
            if (list == null || list.Length < compCount)
                list = new object[compCount];
            for (int i = 0, j = 0, iMax = compArrayCount; i < iMax; i++)
            {
                if (compArrays[i].Has(entity))
                    list[j++] = compArrays[i].Get(entity);
            }
            return compCount;
        }
        public int GetComponentTypes(int entity, ref Type[] list)
        {
            var compCount = entities[entity].ComponentCount;
            if (compCount == 0) { return 0; }
            if (list == null || list.Length < compCount)
                list = new Type[compCount];
            for (int i = 0, j = 0, iMax = compArrayCount; i < iMax; i++)
            {
                if (compArrays[i].Has(entity))
                    list[j++] = compArrays[i].ComponentType;
            }
            return compCount;
        }
        internal void OnComponentAdded(int entity, byte compId)
        {
            var allQueryList = allQueryLists[compId];
            var anyQueryList = anyQueryLists[compId];
            var noneQueryList = noneQueryLists[compId];
            // add component.
            if (allQueryList != null)
            {
                foreach (var query in allQueryList)
                {
                    if (query.Marches(entity))
                    {
                        if (query.entityIndexs[entity] > 0) { throw new Exception("Entity already in filter."); }
                        query.AddEntity(entity);
                    }
                }
            }
            if (anyQueryList != null)
            {
                foreach (var query in anyQueryList)
                {
                    if (query.entityIndexs[entity] == 0)
                        query.AddEntity(entity);
                }
            }
            if (noneQueryList != null)
            {
                foreach (var query in noneQueryList)
                {
                    if (query.entityIndexs[entity] > 0)
                        query.RemoveEntity(entity);
                }
            }
            entities[entity].ComponentCount++;
            EntityComponentAddedEvent?.Invoke(entity, compId);
        }
        internal void OnComponentRemoved(int entity, byte compId)
        {
            var allQueryList = allQueryLists[compId];
            var anyQueryList = anyQueryLists[compId];
            var noneQueryList = noneQueryLists[compId];
            if (allQueryList != null)
            {
                foreach (var query in allQueryList)
                {
                    if (query.entityIndexs[entity] > 0)
                        query.RemoveEntity(entity);
                }
            }
            if (anyQueryList != null)
            {
                foreach (var query in anyQueryList)
                {
                    if (!query.Marches(entity))
                        query.RemoveEntity(entity);
                }
            }
            if (noneQueryList != null)
            {
                foreach (var query in noneQueryList)
                {
                    if (query.Marches(entity))
                    {
                        if (query.entityIndexs[entity] > 0) { throw new Exception("Entity already in filter."); }
                        query.AddEntity(entity);
                    }
                }
            }
            ref var entityData = ref entities[entity];
            entityData.ComponentCount--;
            if (entityData.ComponentCount == 0)
                DestroyEntity(entity);
            EntityComponentRemovedEvent?.Invoke(entity, compId);
        }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public ref T AddComponent<T>(int entity) where T : struct
        //{
        //    var compArray = GetComponentArray<T>();
        //    return ref compArray.Add(entity);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public ref T AddComponent<T>(int entity, byte compId) where T : struct
        //{
        //    var compArray = GetComponentArray<T>(compId);
        //    return ref compArray.Add(entity);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void AddComponent<T>(int entity, T addComp) where T : struct
        //{
        //    var compArray = GetComponentArray<T>();
        //    ref var comp = ref compArray.Add(entity);
        //    comp = addComp;
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void AddComponent<T>(int entity, T addComp, byte compId) where T : struct
        //{
        //    var compArray = GetComponentArray<T>(compId);
        //    ref var comp = ref compArray.Add(entity);
        //    comp = addComp;
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void SetComponent<T>(int entity, T comp) where T : struct
        //{
        //    var compArray = GetComponentArray<T>();
        //    compArray.Set(entity, comp);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void SetComponent<T>(int entity, T comp, byte compId) where T : struct
        //{
        //    var compArray = GetComponentArray<T>(compId);
        //    compArray.Set(entity, comp);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void RemoveComponent<T>(int entity) where T : struct
        //{
        //    var compArray = GetComponentArray<T>();
        //    compArray.Remove(entity);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void RemoveComponent<T>(int entity, byte compId) where T : struct
        //{
        //    var compArray = GetComponentArray<T>(compId);
        //    compArray.Remove(entity);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public ref T GetComponent<T>(int entity) where T : struct
        //{
        //    var compArray = GetComponentArray<T>();
        //    return ref compArray.Get(entity);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public ref T GetComponent<T>(int entity, byte compId) where T : struct
        //{
        //    var compArray = GetComponentArray<T>(compId);
        //    return ref compArray.Get(entity);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool HasComponent<T>(int entity) where T : struct
        //{
        //    var compArray = GetComponentArray<T>();
        //    return compArray.Has(entity);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool HasComponent<T>(int entity, byte compId) where T : struct
        //{
        //    var compArray = GetComponentArray<T>(compId);
        //    return compArray.Has(entity);
        //}
        #endregion

        #region Component
        public BufferArray<T> GetBufferArray<T>() where T : struct
        {
            var compType = typeof(DynamicBuffer<T>);
            if (compArrayDict.TryGetValue(compType, out var existArray))
                return (BufferArray<T>)existArray;
            var compArray = new BufferArray<T>(this, compArrayCount, entities.Length);
            compArrayDict[compType] = compArray;
            compArrays[compArrayCount++] = compArray;
            return compArray;
        }
        public ComponentArray<T> GetComponentArray<T>() where T : struct
        {
            var compType = typeof(T);
            if (compArrayDict.TryGetValue(compType, out var existArray))
                return (ComponentArray<T>)existArray;
            var compArray = new ComponentArray<T>(this, compArrayCount, entities.Length);
            //if (compType.IsGenericType && compType.GetGenericTypeDefinition() == typeof(DynamicBuffer<>))
            //    compArray = typeof(BufferArray<>).MakeGenericType(compType.GenericTypeArguments[0]);
            compArrayDict[compType] = compArray;
            compArrays[compArrayCount++] = compArray;
            return compArray;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentArray<T> GetComponentArray<T>(byte compId) where T : struct
        {
            return compId >= 0 && compId < compArrayCount ? (ComponentArray<T>)compArrays[compId] : null;
        }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public IEcsComponentArray GetComponentArray(Type type)
        //{
        //    return compArrayDict.TryGetValue(type, out var compArray) ? compArray : null;
        //}
        public byte GetComponentId(Type type)
        {
            if (!compArrayDict.TryGetValue(type, out var compArray))
                return 0;
            return compArray.ComponentId;
        }
        public Type GetComponentType(int typeId)
        {
            return compArrays[typeId].ComponentType;
        }
        #endregion

        #region Query
        public Query GetQuery(QueryDesc questDesc)
        {
            //questDesc.Build();
            var hash = questDesc.hash;
            if (queryDict.TryGetValue(hash, out var query))
            {
                questDesc.Reset();
                if (recycledQueryDescCount == recycledQueryDescs.Length) Array.Resize(ref recycledQueryDescs, recycledQueryDescCount << 1);
                recycledQueryDescs[recycledQueryDescCount++] = questDesc;
                return query;
            }
            query = new Query(this, questDesc, entities.Length / 4, entities.Length);
            queryDict[hash] = query;
            queryList.Add(query);
            // add to component dictionaries for fast compatibility scan.
            for (int i = 0, iMax = questDesc.allCount; i < iMax; i++)
            {
                var list = allQueryLists[questDesc.all[i]];
                if (list == null)
                {
                    list = new List<Query>(8);
                    allQueryLists[questDesc.all[i]] = list;
                }
                list.Add(query);
            }
            for (int i = 0, iMax = questDesc.anyCount; i < iMax; i++)
            {
                var list = anyQueryLists[questDesc.any[i]];
                if (list == null)
                {
                    list = new List<Query>(8);
                    anyQueryLists[questDesc.any[i]] = list;
                }
                list.Add(query);
            }
            for (int i = 0, iMax = questDesc.noneCount; i < iMax; i++)
            {
                var list = noneQueryLists[questDesc.none[i]];
                if (list == null)
                {
                    list = new List<Query>(8);
                    noneQueryLists[questDesc.none[i]] = list;
                }
                list.Add(query);
            }
            // scan exist entities for compatibility with new filter.
            for (int i = 0, iMax = entityCount; i < iMax; i++)
            {
                ref var entityData = ref entities[i];
                if (entityData.ComponentCount > 0 && query.Marches(i))
                    query.AddEntity(i);
            }
            QueryCreatedEvent?.Invoke(query);
            return query;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueryDesc CreateQueryDesc()
        {
            return recycledQueryDescCount > 0 ? recycledQueryDescs[--recycledQueryDescCount] : new QueryDesc(this);
        }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //bool IsMaskCompatible(EcsQueryDesc queryDesc, int entity)
        //{
        //    for (int i = 0, iMax = queryDesc.allCount; i < iMax; i++)
        //        if (!compArrays[queryDesc.all[i]].Has(entity)) return false;
        //    for (int i = 0, iMax = queryDesc.noneCount; i < iMax; i++)
        //        if (compArrays[queryDesc.none[i]].Has(entity)) return false;
        //    return true;
        //}
        #endregion

        #region System
        public ISystem GetSystem(Type type)
        {
            systemDict.TryGetValue(type, out var system);
            return system;
        }
        public void AddSystem(ISystem system)
        {
            var type = system.GetType();
            if (systemDict.ContainsKey(type))
                return;
            systemDict.Add(type, system);
            systemList.Add(system);
            if (isInited)
                system.Init(this);
        }
        public void DestroySystem(ISystem system)
        {
            if (isDestroyed) return;
            var type = system.GetType();
            if (systemDict.ContainsKey(type))
                return;
            systemDict.Remove(type);
            systemList.Remove(system);
            system.Dispose();
        }
        #endregion

        public struct Config
        {
            public int EntityCapacity;
            public int RecycledEntityCapacity;
            //public int ComponentCapacity;
            public int QueryCapacity;
            //public int SystemCapacity;
            internal const int EntityCapacityDefault = 512;
            internal const int RecycledEntityCapacityDefault = 512;
            //internal const int ComponentCapacityDefault = 512;
            internal const int QueryCapacityDefault = 512;
            //internal const int SystemCapacityDefault = 64;
        }
    }

}
