using System.Collections.Generic;
using System.Linq;
using BlueCheese.Core.FSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Core.FSM.Editor
{
    public class FSMGraphEditorWindow : EditorWindow
    {
        private FSMGraphView       _graphView;
        private FSMInspectorPanel  _inspectorPanel;
        private FSMBlackboardPanel _blackboardPanel;
        private FSMGraphAsset      _asset;

        private List<ISelectable> _lastSelection = new();

        // ── Static openers ───────────────────────────────────────────────────

        [MenuItem("Window/FSM/Graph Editor")]
        public static void Open() => GetWindow<FSMGraphEditorWindow>("FSM Graph Editor");

        public static void Open(FSMGraphAsset asset)
        {
            var window = GetWindow<FSMGraphEditorWindow>("FSM Graph Editor");
            window.LoadAsset(asset);
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void OnEnable()
        {
            BuildLayout();
            LoadStyles();
        }

        private void Update()
        {
            // GraphView has no built-in selection-change event — poll each frame
            if (_graphView == null || _asset == null) return;

            var current = _graphView.selection.ToList();
            if (current.Count == _lastSelection.Count &&
                current.Zip(_lastSelection, (a, b) => a == b).All(x => x))
                return;

            _lastSelection = current;
            _inspectorPanel.ShowSelection(current, _asset);
        }

        // ── Layout ───────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            // ── Toolbar ──
            var toolbar = new VisualElement();
            toolbar.AddToClassList("fsm-toolbar");

            var saveBtn = new Button(SaveChanges) { text = "Save" };
            saveBtn.AddToClassList("fsm-toolbar__save-button");
            toolbar.Add(saveBtn);

            _toolbarAssetLabel = new Label();
            _toolbarAssetLabel.AddToClassList("fsm-toolbar__asset-label");
            toolbar.Add(_toolbarAssetLabel);

            rootVisualElement.Add(toolbar);

            // ── Main area with resizable split ──
            //
            // TwoPaneSplitView(fixedPaneIndex=1, initialDimension=400, Horizontal)
            //   ├── left  (graph + blackboard, grows)
            //   └── inspector panel (fixed pane, resizable by drag)
            var splitView = new TwoPaneSplitView(1, 400f, TwoPaneSplitViewOrientation.Horizontal);
            splitView.AddToClassList("fsm-split");
            rootVisualElement.Add(splitView);

            // Left: graph container + blackboard stacked vertically
            var left = new VisualElement();
            left.AddToClassList("fsm-left");

            var graphContainer = new VisualElement();
            graphContainer.AddToClassList("fsm-graph-container");
            left.Add(graphContainer);

            _graphView = new FSMGraphView();
            _graphView.StretchToParentSize();
            graphContainer.Add(_graphView);

            _graphView.graphViewChanged += change =>
            {
                hasUnsavedChanges = true;
                return change;
            };

            _blackboardPanel = new FSMBlackboardPanel();
            _blackboardPanel.OnChanged += () => hasUnsavedChanges = true;
            left.Add(_blackboardPanel);

            splitView.Add(left);

            // Right: inspector
            _inspectorPanel = new FSMInspectorPanel();
            _inspectorPanel.OnChanged += () =>
            {
                hasUnsavedChanges = true;
                _graphView.RefreshAllExitTimeIcons();
            };
            _inspectorPanel.OnStateSetAsDefault += newDefault =>
            {
                _graphView.SetDefaultState(newDefault);
                hasUnsavedChanges = true;
            };
            splitView.Add(_inspectorPanel);
        }

        private Label _toolbarAssetLabel;

        private void LoadStyles()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/unity-core/Editor/FSM/Styles/GraphWindow.uss");
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);
        }

        // ── Asset operations ─────────────────────────────────────────────────

        private void LoadAsset(FSMGraphAsset asset)
        {
            _asset = asset;
            titleContent = new GUIContent($"FSM — {asset.name}");
            if (_toolbarAssetLabel != null) _toolbarAssetLabel.text = asset.name;

            _graphView.Load(asset);
            _blackboardPanel.Load(asset);
            _inspectorPanel.ShowSelection(new List<ISelectable>(), asset);
            _lastSelection.Clear();
            hasUnsavedChanges = false;
        }

        public override void SaveChanges()
        {
            if (_asset == null) return;

            _graphView.Save(_asset);
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            hasUnsavedChanges = false;
        }
    }
}
