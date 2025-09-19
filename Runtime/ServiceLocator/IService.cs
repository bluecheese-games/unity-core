//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Core.ServiceLocator
{
    public interface IService
    {
        IService AsLazy();
        IService AsNonLazy();
        void AsSingleton();
        void AsTransient();
        void WithInstance(object instance);
        IService WithOptions(Func<IOptions> optionsFunc);
    }
}