using BlueCheese.Core.FSM.Graph;
using UnityEditor.Experimental.GraphView;

namespace BlueCheese.Core.FSM.Editor
{
    public class FSMEdge : Edge
    {
        // Live reference to the serialized data in the asset.
        public FSMGraphAsset.GraphTransition GraphTransition { get; set; }

        public bool IsFromAnyState => output?.node is AnyStateNode;
    }
}
