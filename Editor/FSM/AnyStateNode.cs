//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BlueCheese.Core.FSM.Editor
{
    public class AnyStateNode : BaseNode
    {
        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);
            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Copiable;
        }

        public override void Draw()
        {
            title = "Any State";
            AddToClassList("any-state-node");

            AddOutputPort(); // one free port to start

            RefreshExpandedState();
        }
    }
}
