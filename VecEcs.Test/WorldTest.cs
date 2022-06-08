using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Poly.VecEcs.Test
{
    [TestClass]
    public partial class WorldTest
    {
        public struct CompA : IEquatable<CompA>
        {
            public int Value;
            public bool Equals(CompA other) => Value == other.Value;
        }

        public struct CompB : IEquatable<CompB>
        {
            public int Value;
            public string Str;
            public bool Equals(CompB other) => Value == other.Value && Str == other.Str;
        }
        public struct CompC : IEquatable<CompC>
        {
            public int Value;
            public bool Equals(CompC other) => Value == other.Value;
        }
        public struct CompD : IEquatable<CompD>
        {
            public int Value;
            public bool Equals(CompD other) => Value == other.Value;
        }
        public interface INetComp
        {
            ulong DirtyFlag { get; }
            void OnPropertyChanged(byte index);
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct NetVar1<T>// : IEquatable<NetVar<T>>
        {
            private T value;
            private readonly byte index;
            private readonly short index1;
            internal Action<byte> onValueChanged;
        }
        public struct NetVar<T>// : IEquatable<NetVar<T>>
        {
            private T value;
            private readonly byte index;
            //private readonly INetComp comp;
            internal Action<byte> onValueChanged;
            public T Value
            {
                get => value;
                set
                {
                    if (object.Equals(this.value, value)) return;
                    this.value = value;
                    onValueChanged?.Invoke(index);
                }
            }
            public NetVar(byte index, T value = default)
            {
                this.index = index;
                this.onValueChanged = null;
                this.value = value;
            }

            //public bool Equals(NetVar<T> other)
            //{

            //}
        }
        public struct CompE
        {
            public NetVar<int> Value0;
            public NetVar<string> Value1;

            internal ComponentArray<CompE> compArray;
            internal ulong dirtyFlag;

            public CompE(ComponentArray<CompE> compArray)
            {
                this.compArray = compArray;
                dirtyFlag = 0;
                Value0 = new NetVar<int>(0, 11);
                Value1 = new NetVar<string>(1, "huo");
                Value0.onValueChanged = OnPropertyChanged;
                Value1.onValueChanged = OnPropertyChanged;
            }
            internal void OnPropertyChanged(byte index)
            {
                Console.WriteLine($"CompE: {index}");
            }
        }
        public struct CompF
        {
            private int value0;
            private string value1;

            //internal ComponentArray<CompE> compArray;
            internal ulong dirtyFlag;
            public int Value0
            {
                get => value0;
                set
                {
                    if (object.Equals(value0, value)) return;
                    value0 = value;
                    OnPropertyChanged(0);
                }
            }
            public string Value1
            {
                get => value1;
                set
                {
                    if (object.Equals(value1, value)) return;
                    value1 = value;
                    OnPropertyChanged(1);
                }
            }

            //public CompF(ComponentArray<CompE> compArray)
            //{
            //    this.compArray = compArray;
            //    dirtyFlag = 0;
            //    value0 = 0;
            //}
            internal void OnPropertyChanged(byte index)
            {
                Console.WriteLine($"CompF: {index}");
            }
        }
        public struct BufferElementA
        {
            public float Value;
        }

        private static World world;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
        }
        [ClassCleanup]
        public static void ClassCleanup()
        {
        }
        [TestInitialize]
        public void TestInitialize()
        {
            world = new World();
            //world.GetComponentArray<CompA>();
            //world.GetComponentArray<CompB>();
            //world.GetComponentArray<CompC>();
            //world.GetComponentArray<CompD>();
        }
        [TestCleanup]
        public void TestCleanup()
        {
            world.Destroy();
            world = null;
        }
        [TestMethod]
        public void BufferTest()
        {
            var bufferAArray = world.GetBufferArray<BufferElementA>();
            var entityId = world.CreateEntity();
            ref var bufferA = ref bufferAArray.Add(entityId);
            //bufferA = new DynamicBuffer<BufferElementA>(4);
            bufferA.Add(new BufferElementA { Value = 13});
            bufferA.Insert(0, new BufferElementA { Value = 12 });
            bufferA.Insert(0, new BufferElementA { Value = 11 });
            Assert.AreEqual(3, bufferA.Length);
            Assert.AreEqual(11, bufferA[0].Value);
            bufferA.RemoveAt(1);
            Assert.AreEqual(2, bufferA.Length);
            Assert.AreEqual(13, bufferA[1].Value);

            ref var bufferA1 = ref bufferAArray.Get(entityId);
            Assert.AreEqual(2, bufferA1.Length);
            Assert.AreEqual(11, bufferA1[0].Value);
        }
        [TestMethod]
        public void StructTest()
        {
            Console.WriteLine($"NetVar1<int>: {SizeHelper.SizeOf(typeof(NetVar1<int>))}");
            Console.WriteLine($"Action<byte>: {SizeHelper.SizeOf(typeof(Action<byte>))}");

            Console.WriteLine($"NetVar<int>: {SizeHelper.SizeOf(typeof(NetVar<int>))}");
            Console.WriteLine($"NetVar<string>: {SizeHelper.SizeOf(typeof(NetVar<string>))}");
            Console.WriteLine($"CompA: {SizeHelper.SizeOf(typeof(CompA))},{Marshal.SizeOf(typeof(CompA))}");
            Console.WriteLine($"CompB: {SizeHelper.SizeOf(typeof(CompB))},{Marshal.SizeOf(typeof(CompB))}");
            Console.WriteLine($"CompE: {SizeHelper.SizeOf(typeof(CompE))}");
            Console.WriteLine($"CompF: {SizeHelper.SizeOf(typeof(CompF))},{Marshal.SizeOf(typeof(CompF))}");
            var compEArray = world.GetComponentArray<CompE>();
            var compE = new CompE(compEArray);

            //compE.Value0.Value = 11;

            var entityId = world.CreateEntity();
            //world.AddComponent(entityId, compE);
            compEArray.Add(entityId, compE);

            //ref var compE1 = ref world.GetComponent<CompE>(entityId);
            ref var compE1 = ref compEArray.Get(entityId);
            Assert.AreEqual(11, compE1.Value0.Value);

            compE1.Value0.Value = 12;
            compE1.Value1.Value = "dian";
            //compEArray.Set(entityId, new CompE { Value = 12 });
            //compE1 = ref world.GetComponent<CompE>(entityId);
            compE1 = ref compEArray.Get(entityId);
            Assert.AreEqual(12, compE1.Value0.Value);
            var compE2 = compE1;
            Assert.AreEqual("dian", compE2.Value1.Value);

        }
        [TestMethod]
        public void CommonTest()
        {
            var compAArray = world.GetComponentArray<CompA>();
            var compBArray = world.GetComponentArray<CompB>();
            var compCArray = world.GetComponentArray<CompC>();
            var compDArray = world.GetComponentArray<CompD>();

            var compB = new CompB { Value = 12 };

            var entityId = world.CreateEntity();
            //world.AddComponent(entityId, new CompA());
            compAArray.Add(entityId);
            //[A]
            //world.AddComponent(entityId, compB);
            compBArray.Add(entityId, compB);
            //[A,B]
            //world.AddComponent(entityId, new CompC());
            compCArray.Add(entityId);
            //[A,B,C]

            Assert.IsFalse(compDArray.Has(entityId));
            Assert.IsTrue(compBArray.Has(entityId));

            ref var compB1 = ref compBArray.Get(entityId);
            var str = "huo";
            Assert.AreEqual(compB, compB1);
            compB1.Value = 100;
            compB1.Str = str;
            compB1 = ref compBArray.Get(entityId);
            Assert.AreEqual(str, compB1.Str);

            compBArray.Set(entityId, new CompB { Value = 13, Str = "dian" });
            compB1 = ref compBArray.Get(entityId);
            Assert.AreEqual(13, compB1.Value);
            Assert.AreEqual("dian", compB1.Str);

            compBArray.Remove(entityId);
            //[A,C]
            Assert.IsFalse(compBArray.Has(entityId));
        }

        //[TestMethod]
        //public void QueryTest()
        //{
        //    var entityA = world.CreateEntity(typeof(ComponentA));
        //    var entityB = world.CreateEntity(typeof(ComponentB));
        //    var entityABD = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentD));
        //    var entityABC = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC));
        //    var entityAC = world.CreateEntity(typeof(ComponentA), typeof(ComponentC));
        //    var entityBD0 = world.CreateEntity(typeof(ComponentB), typeof(ComponentD));
        //    var entityBD1 = world.CreateEntity(typeof(ComponentB), typeof(ComponentD));
        //    var entityBC = world.CreateEntity(typeof(ComponentB), typeof(ComponentC));
        //    var entityAB = world.CreateEntity(typeof(ComponentA), typeof(ComponentB));
        //    var entityAD = world.CreateEntity(typeof(ComponentA), typeof(ComponentD));

        //    ref readonly var archetypeBD = ref world.GetArchetype(typeof(ComponentD), typeof(ComponentB));
        //    Assert.AreEqual(2, archetypeBD.EntityCount);

        //    var queryDesc = world.CreateQueryDesc().WithAll<ComponentB, ComponentA>().WithNone<ComponentC>().Build();
        //    var query = world.GetQuery(queryDesc);
        //    Assert.AreEqual(1, world.QueryCount);
        //    //entityABD,entityAB
        //    Assert.AreEqual(2, query.GetEntityCount());
        //    Assert.IsFalse(query.Matchs(entityBC));
        //    Assert.IsTrue(query.Matchs(entityABD));

        //    //Set comp
        //    query.ForEach((int entity, ref ComponentB compB) =>
        //    {
        //        compB.Value = 13;
        //    });
        //    ref var compB = ref world.GetComponent<ComponentB>(entityABD);
        //    Assert.AreEqual(13, compB.Value);

        //    //Remove comp: entityABD -> entityBD
        //    query.ForEach((int entityId, ref ComponentB compB) =>
        //    {
        //        if (world.HasComponent<ComponentD>(entityId))
        //        {
        //            world.RemoveComponent<ComponentA>(entityId);
        //        }
        //    });
        //    Assert.AreEqual(1, query.GetEntityCount());
        //    Assert.AreEqual(3, archetypeBD.EntityCount);//BD

        //    Assert.IsFalse(world.HasComponent<ComponentA>(entityABD));
        //    world.AddComponent<ComponentA>(entityABD);
        //    Assert.IsTrue(query.Matchs(entityABD));
        //    //var entityABCD = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC), typeof(ComponentD));
        //    //entityAB,entityABD
        //    Assert.AreEqual(2, query.GetEntityCount());

        //    //TODO
        //    var index = 0;
        //    query.ForEach((int entityId, ref ComponentB compB) =>
        //    {
        //        index++;
        //        world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentD));
        //    });
        //    Assert.AreEqual(3, index);
        //    Assert.AreEqual(5, query.GetEntityCount());
        //}

        //public class System : IEcsSystem
        //{
        //    private EcsQuery query;
        //    public void Init(EcsWorld world)
        //    {
        //        var queryDesc = world.CreateQueryDesc().WithAll<ComponentA>().WithNone<ComponentB>().Build();
        //        query = world.GetQuery(queryDesc);
        //    }
        //    public void Dispose()
        //    {
        //    }
        //    public void Update()
        //    {
        //        query.ForEach((int entity, ref ComponentA compA) =>
        //        {
        //            compA.Value++;
        //        });
        //    }
        //}
    }

    static class SizeHelper
    {
        private static Dictionary<Type, int> sizes = new Dictionary<Type, int>();

        public static int SizeOf(Type type)
        {
            int size;
            if (sizes.TryGetValue(type, out size))
            {
                return size;
            }

            size = SizeOfType(type);
            sizes.Add(type, size);
            return size;
        }

        private static int SizeOfType(Type type)
        {
            var dm = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Ret);
            return (int)dm.Invoke(null, null);
        }
    }
}