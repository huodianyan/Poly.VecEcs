using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Poly.VecEcs
{
    #region World
    public class EcsWorld
    {
        //world
        private readonly string id;
        private readonly object shared;
        private bool isInited;
        private bool isDestroyed;
        //Entity
        internal EcsEntity[] entityDatas;
        private int entityCount;
        private int[] recycledEntities;
        private int recycledEntityCount;
        //Component
        internal IEcsComponentArray[] compArrays;
        private byte compArrayCount;
        private readonly Dictionary<Type, IEcsComponentArray> compArrayDict;
        //Query
        private readonly Dictionary<int, EcsQuery> queryDict;
        private readonly List<EcsQuery> queryList;
        private List<EcsQuery>[] allQueryLists;
        private List<EcsQuery>[] anyQueryLists;
        private List<EcsQuery>[] noneQueryLists;
        private EcsQueryDesc[] recycledQueryDescs;
        private int recycledQueryDescCount;
        //System
        private Dictionary<Type, IEcsSystem> systemDict;
        private List<IEcsSystem> systemList;
        //private IRunSystem[] runSystems;
        //private int runSystemCount;

        public string Id => id;
        public int EntityCount => entityCount - recycledEntityCount;
        public int WorldSize => entityDatas.Length;
        public IList<IEcsSystem> SystemList => systemList;
        public IList<EcsQuery> QueryList => queryList;
        public IDictionary<Type, IEcsComponentArray> CompArrayDict => compArrayDict;
        //public bool IsAlive => !isDestroyed;

        public event Action<int> WorldResizedEvent;
        public event Action<int> EntityCreatedEvent;
        public event Action<int> EntityDestroyedEvent;
        public event Action<int, int> EntityComponentAddedEvent;
        public event Action<int, int> EntityComponentRemovedEvent;
        //public event Action<int, int> EntityComponentChangedEvent;
        public event Action<EcsQuery> QueryCreatedEvent;

        public EcsWorld(string id = "Default", object shared = null, in Config cfg = default)
        {
            //world
            this.id = id;
            this.shared = shared;
            // entities.
            var capacity = cfg.EntityCapacity > 0 ? cfg.EntityCapacity : Config.EntityCapacityDefault;
            entityDatas = new EcsEntity[capacity];
            capacity = cfg.RecycledEntityCapacity > 0 ? cfg.RecycledEntityCapacity : Config.RecycledEntityCapacityDefault;
            recycledEntities = new int[capacity];
            entityCount = 0;
            recycledEntityCount = 0;
            // component
            //capacity = cfg.ComponentCapacity > 0 ? cfg.ComponentCapacity : Config.ComponentCapacityDefault;
            compArrays = new IEcsComponentArray[256];
            compArrayDict = new Dictionary<Type, IEcsComponentArray>(256);
            allQueryLists = new List<EcsQuery>[256];
            anyQueryLists = new List<EcsQuery>[256];
            noneQueryLists = new List<EcsQuery>[256];
            //compTypeSize = cfg.PoolDenseSize > 0 ? cfg.PoolDenseSize : Config.PoolDenseSizeDefault;
            //recycledCompSize = cfg.PoolRecycledSize > 0 ? cfg.PoolRecycledSize : Config.PoolRecycledSizeDefault;
            compArrayCount = 0;
            // query
            capacity = cfg.QueryCapacity > 0 ? cfg.QueryCapacity : Config.QueryCapacityDefault;
            queryDict = new Dictionary<int, EcsQuery>(capacity);
            queryList = new List<EcsQuery>(capacity);
            recycledQueryDescs = new EcsQueryDesc[64];
            recycledQueryDescCount = 0;
            //system
            //capacity = cfg.SystemCapacity > 0 ? cfg.SystemCapacity : Config.SystemCapacityDefault;
            systemDict = new Dictionary<Type, IEcsSystem>();
            systemList = new List<IEcsSystem>();
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
                ref var entityData = ref entityDatas[i];
                if (entityData.ComponentCount > 0)
                    DestroyEntity(i);
            }
            //Component
            compArrays = Array.Empty<IEcsComponentArray>();
            compArrayDict.Clear();
            //Query
            queryDict.Clear();
            queryList.Clear();
            allQueryLists = Array.Empty<List<EcsQuery>>();
            anyQueryLists = Array.Empty<List<EcsQuery>>();
            noneQueryLists = Array.Empty<List<EcsQuery>>();
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
                ref var entityData = ref entityDatas[entity];
                entityData.Version = (short)-entityData.Version;
            }
            else
            {
                // new entity.
                if (entityCount == entityDatas.Length)
                {
                    // resize entities and component pools.
                    var newSize = entityCount << 1;
                    Array.Resize(ref entityDatas, newSize);
                    for (int i = 0, iMax = compArrayCount; i < iMax; i++)
                        compArrays[i].Resize(newSize);
                    for (int i = 0, iMax = queryList.Count; i < iMax; i++)
                        queryList[i].Resize(newSize);
                    WorldResizedEvent?.Invoke(newSize);
                }
                entity = entityCount++;
                entityDatas[entity].Version = 1;
            }
            EntityCreatedEvent?.Invoke(entity);
            return entity;
        }
        public void DestroyEntity(int entity)
        {
            if (entity < 0 || entity >= entityCount) { throw new Exception("Cant touch destroyed entity."); }
            ref var entityData = ref entityDatas[entity];
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
            return entity >= 0 && entity < entityCount && entityDatas[entity].Version > 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEntityComponentCount(int entity)
        {
            return entityDatas[entity].ComponentCount;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetEntityVersion(int entity)
        {
            return entityDatas[entity].Version;
        }
        public int GetAllEntities(ref int[] entities)
        {
            var count = entityCount - recycledEntityCount;
            if (entities == null || entities.Length < count)
                entities = new int[count];
            var id = 0;
            for (int i = 0, iMax = entityCount; i < iMax; i++)
            {
                ref var entityData = ref entityDatas[i];
                // should we skip empty entities here?
                if (entityData.Version > 0 && entityData.ComponentCount >= 0)
                    entities[id++] = i;
            }
            return count;
        }
        public int GetComponents(int entity, ref object[] list)
        {
            var compCount = entityDatas[entity].ComponentCount;
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
            var compCount = entityDatas[entity].ComponentCount;
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
            entityDatas[entity].ComponentCount++;
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
            ref var entityData = ref entityDatas[entity];
            entityData.ComponentCount--;
            if (entityData.ComponentCount == 0)
                DestroyEntity(entity);
            EntityComponentRemovedEvent?.Invoke(entity, compId);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddComponent<T>(int entity) where T : struct
        {
            var compArray = GetComponentArray<T>();
            return ref compArray.Add(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddComponent<T>(int entity, byte compId) where T : struct
        {
            var compArray = GetComponentArray<T>(compId);
            return ref compArray.Add(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(int entity, T addComp) where T : struct
        {
            var compArray = GetComponentArray<T>();
            ref var comp = ref compArray.Add(entity);
            comp = addComp;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(int entity, T addComp, byte compId) where T : struct
        {
            var compArray = GetComponentArray<T>(compId);
            ref var comp = ref compArray.Add(entity);
            comp = addComp;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>(int entity, T comp) where T : struct
        {
            var compArray = GetComponentArray<T>();
            compArray.Set(entity, comp);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>(int entity, T comp, byte compId) where T : struct
        {
            var compArray = GetComponentArray<T>(compId);
            compArray.Set(entity, comp);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int entity) where T : struct
        {
            var compArray = GetComponentArray<T>();
            compArray.Remove(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int entity, byte compId) where T : struct
        {
            var compArray = GetComponentArray<T>(compId);
            compArray.Remove(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entity) where T : struct
        {
            var compArray = GetComponentArray<T>();
            return ref compArray.Get(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entity, byte compId) where T : struct
        {
            var compArray = GetComponentArray<T>(compId);
            return ref compArray.Get(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(int entity) where T : struct
        {
            var compArray = GetComponentArray<T>();
            return compArray.Has(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(int entity, byte compId) where T : struct
        {
            var compArray = GetComponentArray<T>(compId);
            return compArray.Has(entity);
        }
        #endregion

        #region Component
        public EcsComponentArray<T> GetComponentArray<T>() where T : struct
        {
            var compType = typeof(T);
            if (compArrayDict.TryGetValue(compType, out var existArray))
                return (EcsComponentArray<T>)existArray;
            var compArray = new EcsComponentArray<T>(this, compArrayCount, entityDatas.Length);
            compArrayDict[compType] = compArray;
            //if (compArrayCount == compArrays.Length)
            //{
            //    var newSize = compArrayCount << 1;
            //    Array.Resize(ref compArrays, newSize);
            //    Array.Resize(ref allQueryLists, newSize);
            //    Array.Resize(ref noneQueryLists, newSize);
            //}
            compArrays[compArrayCount++] = compArray;
            return compArray;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsComponentArray<T> GetComponentArray<T>(byte compId) where T : struct
        {
            return compId >= 0 && compId < compArrayCount ? (EcsComponentArray<T>)compArrays[compId] : null;
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
        public EcsQuery GetQuery(EcsQueryDesc questDesc)
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
            query = new EcsQuery(this, questDesc, entityDatas.Length / 4, entityDatas.Length);
            queryDict[hash] = query;
            queryList.Add(query);
            // add to component dictionaries for fast compatibility scan.
            for (int i = 0, iMax = questDesc.allCount; i < iMax; i++)
            {
                var list = allQueryLists[questDesc.all[i]];
                if (list == null)
                {
                    list = new List<EcsQuery>(8);
                    allQueryLists[questDesc.all[i]] = list;
                }
                list.Add(query);
            }
            for (int i = 0, iMax = questDesc.anyCount; i < iMax; i++)
            {
                var list = anyQueryLists[questDesc.any[i]];
                if (list == null)
                {
                    list = new List<EcsQuery>(8);
                    anyQueryLists[questDesc.any[i]] = list;
                }
                list.Add(query);
            }
            for (int i = 0, iMax = questDesc.noneCount; i < iMax; i++)
            {
                var list = noneQueryLists[questDesc.none[i]];
                if (list == null)
                {
                    list = new List<EcsQuery>(8);
                    noneQueryLists[questDesc.none[i]] = list;
                }
                list.Add(query);
            }
            // scan exist entities for compatibility with new filter.
            for (int i = 0, iMax = entityCount; i < iMax; i++)
            {
                ref var entityData = ref entityDatas[i];
                if (entityData.ComponentCount > 0 && query.Marches(i))
                    query.AddEntity(i);
            }
            QueryCreatedEvent?.Invoke(query);
            return query;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsQueryDesc CreateQueryDesc()
        {
            return recycledQueryDescCount > 0 ? recycledQueryDescs[--recycledQueryDescCount] : new EcsQueryDesc(this);
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
        public IEcsSystem GetSystem(Type type)
        {
            systemDict.TryGetValue(type, out var system);
            return system;
        }
        public void AddSystem(IEcsSystem system)
        {
            var type = system.GetType();
            if (systemDict.ContainsKey(type))
                return;
            systemDict.Add(type, system);
            systemList.Add(system);
            if (isInited)
                system.Init(this);
        }
        public void DestroySystem(IEcsSystem system)
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

    #endregion
}
