//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;

//namespace Poly.ArcEcs.Test
//{
//    [TestClass]
//    public partial class PolyEcsBenchmarkTest
//    {
//        [ClassInitialize]
//        public static void ClassInitialize(TestContext testContext)
//        {
//            SetupPolyEcs();
//        }
//        [ClassCleanup]
//        public static void ClassCleanup()
//        {
//            world.Destroy();
//        }
//        [TestInitialize]
//        public void TestInitialize()
//        {
//        }
//        [TestCleanup]
//        public void TestCleanup()
//        {
//        }

//        private struct ComponentA
//        {
//            public int Value;
//        }
//        private struct ComponentB
//        {
//            public int Value;
//        }
//        private struct ComponentC
//        {
//            public int Value;
//        }
//        private struct ComponentD
//        {
//            public int Value;
//        }
//        private static EcsWorld world;
//        public static void SetupPolyEcs()
//        {
//            world = new EcsWorld();
//            world.RegisterComponent<ComponentA>();
//            world.RegisterComponent<ComponentB>();
//            world.RegisterComponent<ComponentC>();
//            world.RegisterComponent<ComponentD>();

//            for (int i = 0; i < 15000; ++i)
//            {
//                world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentD));
//                world.CreateEntity(typeof(ComponentA), typeof(ComponentC));
//                world.CreateEntity(typeof(ComponentB), typeof(ComponentD));
//                world.CreateEntity(typeof(ComponentB), typeof(ComponentD));
//                world.CreateEntity(typeof(ComponentB), typeof(ComponentC));
//                world.CreateEntity(typeof(ComponentA), typeof(ComponentB));
//                world.CreateEntity(typeof(ComponentA), typeof(ComponentD));
//                world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC));
//            }
//        }
//        [TestMethod]
//        public void PolyEcsStressTest()
//        {
//            //List<int> entities = new List<int>();
//            int[] entities = new int[10000];
//            int index = 0;

//            for (int i = 0; i < 1000; i++)
//            {
//                var queryDescA = world.CreateQueryDesc().WithAll<ComponentA>().Build();
//                var queryA = world.GetQuery(queryDescA);
//                var queryDescB = world.CreateQueryDesc().WithAll<ComponentB>().Build();
//                var queryB = world.GetQuery(queryDescB);
//                var queryDescC = world.CreateQueryDesc().WithAll<ComponentC>().Build();
//                var queryC = world.GetQuery(queryDescC);
//                var queryDescD = world.CreateQueryDesc().WithAll<ComponentD>().Build();
//                var queryD = world.GetQuery(queryDescD);

//                var queryDescAB = world.CreateQueryDesc().WithAll<ComponentA, ComponentB>().Build();
//                var queryAB = world.GetQuery(queryDescAB);
//                var queryDescAC = world.CreateQueryDesc().WithAll<ComponentA, ComponentC>().Build();
//                var queryAC = world.GetQuery(queryDescAC);
//                var queryDescAD = world.CreateQueryDesc().WithAll<ComponentA, ComponentD>().Build();
//                var queryAD = world.GetQuery(queryDescAD);

//                var queryDescABC = world.CreateQueryDesc().WithAll<ComponentA, ComponentB, ComponentC>().Build();
//                var queryABC = world.GetQuery(queryDescABC);
//                var queryDescABD = world.CreateQueryDesc().WithAll<ComponentA, ComponentB, ComponentD>().Build();
//                var queryABD = world.GetQuery(queryDescABD);

//                var queryDescABCD = world.CreateQueryDesc().WithAll<ComponentA, ComponentB, ComponentC, ComponentD>().Build();
//                var queryABCD = world.GetQuery(queryDescABCD);
//            }

//            for (int i = 0; i < 1000; i++)
//            {
//                var entityABD = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentD));
//                var entityAC = world.CreateEntity(typeof(ComponentA), typeof(ComponentC));
//                var entityBD0 = world.CreateEntity(typeof(ComponentB), typeof(ComponentD));
//                var entityBD1 = world.CreateEntity(typeof(ComponentB), typeof(ComponentD));
//                var entityBC = world.CreateEntity(typeof(ComponentB), typeof(ComponentC));
//                var entityAB = world.CreateEntity(typeof(ComponentA), typeof(ComponentB));
//                var entityAD = world.CreateEntity(typeof(ComponentA), typeof(ComponentD));
//                var entityABC = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC));
//                var entityABCD = world.CreateEntity(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC), typeof(ComponentD));

//                entities[index++] = entityABD;
//                entities[index++] = entityAC;
//                entities[index++] = entityBD0;
//                entities[index++] = entityBD1;
//                entities[index++] = entityBC;
//                entities[index++] = entityAB;
//                entities[index++] = entityAD;
//                entities[index++] = entityABC;
//                entities[index++] = entityABCD;
//            }

//            //for (int i = 0; i < 1000; i++)
//            //{
//            //    PolyEcsForEachOneComp();
//            //    PolyEcsForEachTwoComp();
//            //    PolyEcsForEachThreeComp();
//            //    PolyEcsForEachFourComp();
//            //}
//            for (int i = 0; i < index; i++)
//            {
//                int entity = entities[i];
//                world.DestroyEntity(entity);
//            }
//        }

//        [TestMethod]
//        public void PolyEcsForEachOneComp()
//        {
//            var queryDesc = world.CreateQueryDesc().WithAll<ComponentA>().WithNone<ComponentB, ComponentD>().Build();
//            var query = world.GetQuery(queryDesc);
//            query.ForEach((int entity, ref ComponentA compA) =>
//            {
//                compA.Value++;
//            });
//        }

//        [TestMethod]
//        public void PolyEcsForEachTwoComp()
//        {
//            var queryDesc = world.CreateQueryDesc().WithAll<ComponentA, ComponentC>().WithNone<ComponentB, ComponentD>().Build();
//            var query = world.GetQuery(queryDesc);
//            query.ForEach((int entity, ref ComponentA compA, ref ComponentC compC) =>
//            {
//                compA.Value++;
//                compC.Value++;
//            });
//        }
//        [TestMethod]
//        public void PolyEcsForEachThreeComp()
//        {
//            var queryDesc = world.CreateQueryDesc().WithAll<ComponentA, ComponentB, ComponentC>().WithNone<ComponentD>().Build();
//            var query = world.GetQuery(queryDesc);
//            query.ForEach((int entity, ref ComponentA compA, ref ComponentB compB, ref ComponentC compC) =>
//            {
//                compA.Value++;
//                compB.Value++;
//                compC.Value++;
//            });
//        }
//        [TestMethod]
//        public void PolyEcsForEachFourComp()
//        {
//            var queryDesc = world.CreateQueryDesc().WithAll<ComponentA, ComponentB, ComponentC, ComponentD>().Build();
//            var query = world.GetQuery(queryDesc);
//            //Console.WriteLine($"PolyEcsForEachFourComp: {query.GetEntityCount()}");
//            query.ForEach((int entity, ref ComponentA compA, ref ComponentB compB, ref ComponentC compC, ref ComponentD compD) =>
//            {
//                compA.Value++;
//                compB.Value++;
//                compC.Value++;
//                compD.Value++;
//            });
//        }

//        [TestMethod]
//        public void PolyEcsQueryTest1()
//        {
//            var compAId = world.GetComponentId(typeof(ComponentA));
//            var compBId = world.GetComponentId(typeof(ComponentB));
//            var compCId = world.GetComponentId(typeof(ComponentC));
//            var compDId = world.GetComponentId(typeof(ComponentD));

//            var queryDescA = world.CreateQueryDesc().WithAll<ComponentA>().Build();
//            var queryA = world.GetQuery(queryDescA);
//            //queryA.ForEach((int entity, ref ComponentA compA) =>
//            queryA.ForEach((int entity) =>
//            {
//                //if(world.HasComponent<ComponentB>(entity) && world.HasComponent<ComponentC>(entity))
//                world.CreateEntity(compAId, compBId, compCId, compDId);
//            });

//            var queryDescABCD = world.CreateQueryDesc().WithAll<ComponentA, ComponentB, ComponentC, ComponentD>().Build();
//            var queryABCD = world.GetQuery(queryDescABCD);
//            //queryABCD.ForEach((int entity, ref ComponentA compA, ref ComponentB compB, ref ComponentC compC, ref ComponentD compD) =>
//            queryABCD.ForEach((int entity) =>
//            {
//                //if(world.HasComponent<ComponentB>(entity) && world.HasComponent<ComponentC>(entity))
//                world.DestroyEntity(entity);
//            });
//        }

//        [TestMethod]
//        public void PolyEcsQueryTest2()
//        {
//            var compDId = world.GetComponentId(typeof(ComponentD));
//            var queryDescABC = world.CreateQueryDesc().WithAll<ComponentA, ComponentB, ComponentC>().WithNone<ComponentD>().Build();
//            var queryABC = world.GetQuery(queryDescABC);
//            //Console.WriteLine($"{GetType().Name}.PolyEcsQueryTest2: {queryABC.GetEntityCount()}");
//            //queryABC.ForEach((int entity, ref ComponentA compA, ref ComponentB compB, ref ComponentC compC) =>
//            queryABC.ForEach((int entity) =>
//            {
//                //world.AddComponent<ComponentD>(entity);
//                world.AddComponent<ComponentD>(entity, compDId);
//            });
//            //Console.WriteLine($"{GetType().Name}.PolyEcsQueryTest2: {queryABC.GetEntityCount()}");

//            var queryDescABCD = world.CreateQueryDesc().WithAll<ComponentA, ComponentB, ComponentC, ComponentD>().Build();
//            var queryABCD = world.GetQuery(queryDescABCD);
//            //Console.WriteLine($"{GetType().Name}.PolyEcsQueryTest2: {queryABCD.GetEntityCount()}");
//            queryABCD.ForEach((int entity) =>
//            //queryABCD.ForEach((int entity, ref ComponentA compA, ref ComponentB compB, ref ComponentC compC, ref ComponentD compD) =>
//            {
//                //world.RemoveComponent<ComponentD>(entity);
//                world.RemoveComponent(entity, compDId);
//            });
//            //Console.WriteLine($"{GetType().Name}.PolyEcsQueryTest2: {queryABCD.GetEntityCount()}");
//        }
//    }
//}