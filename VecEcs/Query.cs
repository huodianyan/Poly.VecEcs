using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Poly.VecEcs
{
    public sealed class QueryDesc
    {
        private readonly World world;
        internal byte[] all;
        internal byte[] any;
        internal byte[] none;
        internal int allCount;
        internal int anyCount;
        internal int noneCount;
        internal int hash;

        public QueryDesc(World world)
        {
            this.world = world;
            all = new byte[4];
            any = new byte[2];
            none = new byte[2];
            hash = allCount = anyCount = noneCount = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => hash = allCount = anyCount = noneCount = 0;
        public QueryDesc Build()
        {
            if (allCount > 1) Array.Sort(all, 0, allCount);
            if (anyCount > 1) Array.Sort(any, 0, anyCount);
            if (noneCount > 1) Array.Sort(none, 0, noneCount);
            // calculate hash.
            hash = allCount + anyCount + noneCount;
            for (int i = 0; i < allCount; i++)
                hash = unchecked(hash * 13 + all[i]);
            for (int i = 0; i < anyCount; i++)
                hash = unchecked(hash * 17 + any[i]);
            for (int i = 0; i < noneCount; i++)
                hash = unchecked(hash * 23 - none[i]);
            return this;
        }
        #region All
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WithAll(byte compId)
        {
            //if (Array.IndexOf(All, compId, 0, AllCount) != -1) { throw new Exception($"{type.Name} already in constraints list."); }
            //if (Array.IndexOf(None, compId, 0, NoneCount) != -1) { throw new Exception($"{type.Name} already in constraints list."); }
            if (allCount == all.Length) { Array.Resize(ref all, allCount << 1); }
            all[allCount++] = compId;
        }
        public QueryDesc WithAll(params byte[] compIds)
        {
            foreach (var compId in compIds) WithAll(compId);
            return this;
        }
        public QueryDesc WithAll(params Type[] types)
        {
            foreach (var type in types) WithAll(world.GetComponentId(type));
            return this;
        }
        public QueryDesc WithAll<T0>() where T0 : struct => WithAll(typeof(T0));
        public QueryDesc WithAll<T0, T1>() where T0 : struct where T1 : struct => WithAll(typeof(T0), typeof(T1));
        public QueryDesc WithAll<T0, T1, T2>() where T0 : struct where T1 : struct where T2 : struct => WithAll(typeof(T0), typeof(T1), typeof(T2));
        public QueryDesc WithAll<T0, T1, T2, T3>() where T0 : struct where T1 : struct where T2 : struct where T3 : struct => WithAll(typeof(T0), typeof(T1), typeof(T2), typeof(T3));
        #endregion

        #region Any
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WithAny(byte compId)
        {
            if (anyCount == any.Length) { Array.Resize(ref any, anyCount << 1); }
            any[anyCount++] = compId;
        }
        public QueryDesc WithAny(params byte[] compIds)
        {
            foreach (var compId in compIds) WithAny(compId);
            return this;
        }
        public QueryDesc WithAny(params Type[] types)
        {
            foreach (var type in types) WithAny(world.GetComponentId(type));
            return this;
        }
        public QueryDesc WithAny<T0>() where T0 : struct => WithAny(typeof(T0));
        public QueryDesc WithAny<T0, T1>() where T0 : struct where T1 : struct => WithAny(typeof(T0), typeof(T1));
        public QueryDesc WithAny<T0, T1, T2>() where T0 : struct where T1 : struct where T2 : struct => WithAny(typeof(T0), typeof(T1), typeof(T2));
        #endregion

        #region None
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WithNone(byte compId)
        {
            if (noneCount == none.Length) { Array.Resize(ref none, noneCount << 1); }
            none[noneCount++] = compId;
        }
        public QueryDesc WithNone(params byte[] compIds)
        {
            foreach (var compId in compIds) WithNone(compId);
            return this;
        }
        public QueryDesc WithNone(params Type[] types)
        {
            foreach (var type in types) WithNone(world.GetComponentId(type));
            return this;
        }
        public QueryDesc WithNone<T0>() where T0 : struct => WithNone(typeof(T0));
        public QueryDesc WithNone<T0, T1>() where T0 : struct where T1 : struct => WithNone(typeof(T0), typeof(T1));
        public QueryDesc WithNone<T0, T1, T2>() where T0 : struct where T1 : struct where T2 : struct => WithNone(typeof(T0), typeof(T1), typeof(T2));
        #endregion
    }
    public sealed class Query
    {
        private readonly World world;
        readonly QueryDesc queryDesc;
        private int[] entities;
        private int entitiesCount;
        internal int[] entityIndexs;
        private int lockCount;
        private DelayedOp[] delayedOps;
        private int delayedOpsCount;

        public int Hash => queryDesc.hash;
        public int EntitiesCount => entitiesCount;
        public QueryDesc QueryDesc => queryDesc;

        public event Action<int> EntityAddedEvent;
        public event Action<int> EntityRemovedEvent;

        internal Query(World world, QueryDesc desc, int queryCapacity, int entityCapacity)
        {
            this.world = world;
            this.queryDesc = desc;
            entities = new int[queryCapacity];
            entityIndexs = new int[entityCapacity];
            entitiesCount = 0;
            delayedOps = new DelayedOp[queryCapacity];
            delayedOpsCount = 0;
            lockCount = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddEntity(int entity)
        {
            if (AddDelayedOp(true, entity)) { return; }
            if (entitiesCount == entities.Length) Array.Resize(ref entities, entitiesCount << 1);
            entities[entitiesCount++] = entity;
            entityIndexs[entity] = entitiesCount;
            EntityAddedEvent?.Invoke(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveEntity(int entity)
        {
            if (AddDelayedOp(false, entity)) { return; }
            var idx = entityIndexs[entity] - 1;
            entityIndexs[entity] = 0;
            entitiesCount--;
            if (idx < entitiesCount)
            {
                entities[idx] = entities[entitiesCount];
                entityIndexs[entities[idx]] = idx + 1;
            }
            EntityRemovedEvent?.Invoke(entity);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Marches(int entity)
        {
            var compArrays = world.compArrays;
            for (int i = 0, iMax = queryDesc.allCount; i < iMax; i++)
                if (!compArrays[queryDesc.all[i]].Has(entity)) return false;
            for (int i = 0, iMax = queryDesc.noneCount; i < iMax; i++)
                if (compArrays[queryDesc.none[i]].Has(entity)) return false;
            if (queryDesc.anyCount == 0) return true;
            bool isAny = false;
            for (int i = 0; i < queryDesc.anyCount; i++)
                if (compArrays[queryDesc.none[i]].Has(entity)) { isAny = true; break; }
            return isAny;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool AddDelayedOp(bool added, int entity)
        {
            if (lockCount <= 0) { return false; }
            if (delayedOpsCount == delayedOps.Length) Array.Resize(ref delayedOps, delayedOpsCount << 1);
            ref var op = ref delayedOps[delayedOpsCount++];
            op.Added = added;
            op.Entity = entity;
            return true;
        }
        void Unlock()
        {
            if (lockCount <= 0) { throw new Exception($"Invalid lock-unlock balance for \"{GetType().Name}\"."); }
            lockCount--;
            if (lockCount == 0 && delayedOpsCount > 0)
            {
                for (int i = 0, iMax = delayedOpsCount; i < iMax; i++)
                {
                    ref var op = ref delayedOps[i];
                    if (op.Added)
                        AddEntity(op.Entity);
                    else
                        RemoveEntity(op.Entity);
                }
                delayedOpsCount = 0;
            }
        }
        public void Resize(int newSize)
        {
            Array.Resize(ref entityIndexs, newSize);
        }
        public Enumerator GetEnumerator()
        {
            lockCount++;
            return new Enumerator(this);
        }
        public struct Enumerator : IDisposable
        {
            readonly Query query;
            readonly int[] entities;
            readonly int entityCount;
            int index;

            public Enumerator(Query query)
            {
                this.query = query;
                entities = query.entities;
                entityCount = query.entitiesCount;
                index = -1;
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => entities[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++index < entityCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => query.Unlock();
        }

        struct DelayedOp
        {
            public bool Added;
            public int Entity;
        }
    }
}
