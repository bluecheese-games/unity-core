//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using BlueCheese.Core.FSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace BlueCheese.Core.FSM.Editor
{
    public class FSMGraphEditorWindow : EditorWindow
    {
        private FSMGraphView _graphView;
        private VisualElement _renamePopup;

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

            AddPopups();

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

        private void AddPopups()
		{
			_renamePopup = new VisualElement();
            _renamePopup.StretchToParentSize();
			_renamePopup.AddToClassList("fsm-popup");

			var content = new VisualElement();
            content.AddToClassList("fsm-popup-content");
            _renamePopup.Add(content);

            var button = new Button(() => { _renamePopup.style.display = DisplayStyle.None; })
			{
				text = "X",
			};
            button.AddToClassList("fsm-popup-close");
			content.Add(button);

            var label = new Label("Rename node");
            label.AddToClassList("fsm-popup-text");
            content.Add(label);

            var textField = new TextField();
            textField.AddToClassList("fsm-popup-input");
			textField.Focus();

			content.Add(textField);

            var confirmButton = new Button(() => { _renamePopup.style.display = DisplayStyle.None; });
            confirmButton.AddToClassList("fsm-popup-confirm");
            confirmButton.text = "Confirm";
            content.Add(confirmButton);

            rootVisualElement.Add(_renamePopup);
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
			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/unity-core/Runtime/FSM/Editor/Styles/GraphWindow.uss");

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
