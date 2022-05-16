using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Poly.VecEcs
{
    #region System
    public interface IEcsSystem : IDisposable
    {
        //IWorld World { get; set; }
        void Init(EcsWorld world);
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
    #endregion
}
