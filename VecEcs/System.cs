using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Poly.VecEcs
{
    public interface ISystem : IDisposable
    {
        //IWorld World { get; set; }
        void Init(World world);
        void Update();
    }
    //public interface IInitSystem : IEcsSystem
    //{
    //    void Init(IWorld world);
    //}

    //public interface IRunSystem : IEcsSystem
    //{
    //    void Run(IWorld world);
    //}

    //public interface IDestroySystem : IEcsSystem
    //{
    //    void Destroy(IWorld world);
    //}
}
