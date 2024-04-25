//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using System;

namespace BlueCheese.Unity.Core.Services
{
    public interface IService
    {
        IOptions Options { get; }

        IService AsLazy();
        IService AsNonLazy();
        void AsSingleton();
        void AsTransient();
        void WithInstance(object instance);
        IService UseOptions(Func<IOptions> optionsFunc);
        object GetInstance(Type genericParameterType = null);
        void Startup();
        void Shutdown();
    }
}