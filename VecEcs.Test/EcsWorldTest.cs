using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Poly.VecEcs.Test
{
    [TestClass]
    public partial class EcsWorldTest
    {
        public struct ComponentA : IEquatable<ComponentA>
        {
            public int Value;
            public bool Equals(ComponentA other) => Value == other.Value;
        }

        public struct ComponentB : IEquatable<ComponentB>
        {
            public int Value;
            public string Str;
            public bool Equals(ComponentB other) => Value == other.Value && Str == other.Str;
        }
        public struct ComponentC : IEquatable<ComponentC>
        {
            public int Value;
            public bool Equals(ComponentC other) => Value == other.Value;
        }
        public struct ComponentD : IEquatable<ComponentD>
        {
            public int Value;
            public bool Equals(ComponentD other) => Value == other.Value;
        }

        private static EcsWorld world;

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
            world = new EcsWorld();
            world.GetComponentArray<ComponentA>();
            world.GetComponentArray<ComponentB>();
            world.GetComponentArray<ComponentC>();
            world.GetComponentArray<ComponentD>();
        }
        [TestCleanup]
        public void TestCleanup()
        {
            world.Destroy();
            world = null;
        }
        [TestMethod]
        public void CommonTest()
        {
            var componentB = new ComponentB { Value = 12 };

            var entityId = world.CreateEntity();
            world.AddComponent(entityId, new ComponentA());
            //[A]
            world.AddComponent(entityId, componentB);
            //[A,B]
            world.AddComponent(entityId, new ComponentC());
            //[A,B,C]

            Assert.IsFalse(world.HasComponent<ComponentD>(entityId));
            Assert.IsTrue(world.HasComponent<ComponentB>(entityId));

            ref var compB = ref world.GetComponent<ComponentB>(entityId);
            var str = "huo";
            Assert.AreEqual(componentB, compB);
            compB.Value = 100;
            compB.Str = str;
            compB = ref world.GetComponent<ComponentB>(entityId);
            Assert.AreEqual(str, compB.Str);

            world.SetComponent(entityId, new ComponentB { Value = 13, Str = "dian" });
            compB = ref world.GetComponent<ComponentB>(entityId);
            Assert.AreEqual(13, compB.Value);
            Assert.AreEqual("dian", compB.Str);

            world.RemoveComponent<ComponentB>(entityId);
            //[A,C]
            Assert.IsFalse(world.HasComponent<ComponentB>(entityId));

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
}