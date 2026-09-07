using BlueCheese.Core.FSM.Graph;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BlueCheese.Core.FSM.Editor
{
    [CustomEditor(typeof(FSMGraphAsset))]
    public class FSMGraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open in Graph Editor"))
                FSMGraphEditorWindow.Open((FSMGraphAsset)target);

            // Show raw data for debug purposes only in developer mode
            if (EditorPrefs.GetBool("FSM.ShowRawAsset", false))
                base.OnInspectorGUI();
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as FSMGraphAsset;
            if (asset == null) return false;
            FSMGraphEditorWindow.Open(asset);
            return true;
        }
    }
}
