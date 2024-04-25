using System;
using BlueCheese.Unity.Core.FSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace BlueCheese.Unity.Core.FSM.Editor
{
    public class FSMGraphEditorWindow : EditorWindow
    {
        private FSMGraphView _graphView;

        [MenuItem("Window/FSM/Graph Editor")]
        public static void Open()
        {
            GetWindow<FSMGraphEditorWindow>("FSM Graph Editor");
        }

        public static void Open(FSMGraphAsset asset)
        {
            var window = GetWindow<FSMGraphEditorWindow>("FSM Graph Editor");
            window.Load(asset);
        }

        private void OnEnable()
        {
            AddGraphView();

            AddHeader();

            AddStyles();
        }

        private void AddHeader()
        {
            var header = new VisualElement();
            //header.StretchToParentWidth();
            var saveButton = new Button(SaveChanges)
            {
                text = "Save",
            };
            header.Add(saveButton);
            rootVisualElement.Add(header);
        }

        private void AddGraphView()
        {
            _graphView = new FSMGraphView();

            _graphView.StretchToParentSize();

            rootVisualElement.Add(_graphView);

            _graphView.graphViewChanged += OnGraphViewChanged;
        }

        private void AddStyles()
        {
            StyleSheet styleSheet = (StyleSheet)EditorGUIUtility.Load("FSM/Variables.uss");

            rootVisualElement.styleSheets.Add(styleSheet);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            hasUnsavedChanges = true;
            return graphViewChange;
        }

        private void Load(FSMGraphAsset asset)
        {
            _graphView.Load(asset);
            hasUnsavedChanges = false;
        }

        public override void SaveChanges()
        {
            hasUnsavedChanges = false;
            _graphView.Save();
        }
    }
}
