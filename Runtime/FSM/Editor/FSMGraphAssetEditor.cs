//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Unity.Core.FSM.Graph;
using UnityEditor.Callbacks;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Unity.Core.FSM.Editor
{
    [CustomEditor(typeof(FSMGraphAsset))]
    public class FSMGraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open in Graph Editor"))
            {
                FSMGraphAsset asset = (FSMGraphAsset)target;
                FSMGraphEditorWindow.Open(asset);
            }

            base.OnInspectorGUI();
        }

        [OnOpenAsset]
        //Handles opening the editor window when double-clicking project files
        public static bool OnOpenAsset(int instanceID, int line)
        {
            FSMGraphAsset asset = EditorUtility.InstanceIDToObject(instanceID) as FSMGraphAsset;
            if (asset != null)
            {
                FSMGraphEditorWindow.Open(asset);
                return true;
            }
            return false;
        }
    }
}
