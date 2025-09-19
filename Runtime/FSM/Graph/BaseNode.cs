//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BlueCheese.Core.FSM.Editor
{
    public class BaseNode : Node
    {
        public event Action OnContentValueChange;

        public BaseNode()
        {
            AddToClassList("fsm-graph-node");
        }

        public virtual void Initialize(Vector2 position)
        {
            SetPosition(new Rect(position, Vector2.zero));
        }

        public virtual void Draw() { }

        public void DispatchOnContentValueChangeEvent() => OnContentValueChange?.Invoke();
    }
}
