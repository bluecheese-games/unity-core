//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Unity.Core.FSM
{
    public class State : IState
    {
        public string Name { get; private set; }

        public State(string name)
        {
            Name = name;
        }

        virtual public void OnEnter() { }

        virtual public void OnExit() { }

        virtual public void OnUpdate(float deltaTime) { }

        public override string ToString() => Name;
    }
}
