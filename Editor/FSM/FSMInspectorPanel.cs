//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using BlueCheese.Core.FSM;
using BlueCheese.Core.FSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Core.FSM.Editor
{
    /// <summary>
    /// Right-side panel that reflects the properties of the selected graph element.
    /// Edits go directly through GraphState / GraphTransition references.
    /// </summary>
    public class FSMInspectorPanel : VisualElement
    {
        // Display labels for Condition.Operator — order must match the enum exactly.
        private static readonly List<string> OperatorLabels = new() { "=", "≠", ">", "≥", "<", "≤" };

        public event Action OnChanged;

        /// <summary>Fired when the user marks a state as Default — caller should clear other defaults.</summary>
        public event Action<StateNode> OnStateSetAsDefault;

        private FSMGraphAsset _asset;

        public FSMInspectorPanel()
        {
            ShowEmpty();
        }

        public void ShowSelection(IList<ISelectable> selection, FSMGraphAsset asset)
        {
            _asset = asset;
            Clear();

            if (selection == null || selection.Count == 0 || asset == null)
            { ShowEmpty(); return; }

            var first = selection[0];
            if (first is StateNode stateNode)
                DrawStateInspector(stateNode);
            else if (first is FSMEdge edge)
                DrawTransitionInspector(edge);
            else
                ShowEmpty();
        }

        // ── Empty ────────────────────────────────────────────────────────────

        private void ShowEmpty()
        {
            Clear();
            var label = new Label("Select a state or transition");
            label.AddToClassList("inspector__empty-label");
            Add(label);
        }

        // ── State inspector ──────────────────────────────────────────────────

        private void DrawStateInspector(StateNode node)
        {
            var gs = node.GraphState;

            AddSectionHeader("STATE");

            // Name — read-only; editing is done by double-clicking the node title
            var nameRow = MakeRow("Name");
            var nameLabel = new Label(gs.Name);
            nameLabel.AddToClassList("inspector__name-label");
            node.OnRenamed += renamed => nameLabel.text = renamed.GraphState.Name;
            nameRow.Add(nameLabel);
            Add(nameRow);

            // Is Default toggle
            var defaultRow = MakeRow("Default");
            var defaultToggle = new Toggle { value = gs.IsDefault };
            defaultToggle.RegisterValueChangedCallback(evt =>
            {
                gs.IsDefault = evt.newValue;
                node.UpdateDefaultStyle();
                node.DispatchOnContentValueChangeEvent();
                if (evt.newValue)
                    OnStateSetAsDefault?.Invoke(node);
                OnChanged?.Invoke();
            });
            defaultRow.Add(defaultToggle);
            Add(defaultRow);

            // ── Handlers ──
            AddSectionHeader("HANDLERS");

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("inspector__scroll");
            Add(scroll);

            var handlerList = new VisualElement();
            handlerList.AddToClassList("inspector__list");
            scroll.Add(handlerList);
            RefreshHandlerList(handlerList, gs);

            var addHandlerBtn = new Button(() => ShowAddHandlerMenu(gs, handlerList))
            { text = "+ Add Handler" };
            addHandlerBtn.AddToClassList("inspector__add-button");
            Add(addHandlerBtn);
        }

        private void RefreshHandlerList(VisualElement container, FSMGraphAsset.GraphState gs)
        {
            container.Clear();
            for (int i = 0; i < gs.HandlerTypeNames.Count; i++)
            {
                var idx       = i;
                var typeName  = gs.HandlerTypeNames[i];
                var shortName = typeName.Contains(",")
                    ? typeName.Split(',')[0].Split('.').Last()
                    : typeName.Split('.').Last();

                container.Add(MakeRemovableRow(shortName, () =>
                {
                    gs.HandlerTypeNames.RemoveAt(idx);
                    RefreshHandlerList(container, gs);
                    OnChanged?.Invoke();
                }));
            }
        }

        private void ShowAddHandlerMenu(FSMGraphAsset.GraphState gs, VisualElement handlerList)
        {
            var menu  = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<IStateHandler>()
                .Where(t => !t.IsAbstract && !t.IsInterface
                            && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in types)
            {
                var t = type;
                menu.AddItem(new GUIContent(t.FullName), false, () =>
                {
                    gs.HandlerTypeNames.Add(t.AssemblyQualifiedName);
                    RefreshHandlerList(handlerList, gs);
                    OnChanged?.Invoke();
                });
            }

            if (menu.GetItemCount() > 0)
            {
                menu.ShowAsContext();
            }
            else
            {
                var noTypesLabel = new Label("No IStateHandler types found.");
                noTypesLabel.AddToClassList("inspector__hint");
                handlerList.Add(noTypesLabel);
            }
        }

        // ── Transition inspector ─────────────────────────────────────────────

        private void DrawTransitionInspector(FSMEdge edge)
        {
            var gt = edge.GraphTransition;
            if (gt == null) return;

            AddSectionHeader("TRANSITION");

            string fromName = edge.output?.node is AnyStateNode ? "Any State"
                : (edge.output?.node as StateNode)?.GraphState.Name ?? "?";
            string toName = (edge.input?.node as StateNode)?.GraphState.Name ?? "?";

            Add(MakeInfoRow("From", fromName));
            Add(MakeInfoRow("To",   toName));

            // ── Exit time ──
            AddSectionHeader("EXIT TIME");

            var useExitRow = MakeRow("Enable");
            var useExitToggle = new Toggle { value = gt.UseExitTime };

            // ExitTime float field — shown only when enabled
            var exitRow = MakeRow("Duration");
            var exitField = new FloatField { value = gt.ExitTime };
            exitField.style.flexGrow = 1;
            exitRow.style.display = gt.UseExitTime ? DisplayStyle.Flex : DisplayStyle.None;

            useExitToggle.RegisterValueChangedCallback(evt =>
            {
                gt.UseExitTime = evt.newValue;
                exitRow.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                OnChanged?.Invoke();
            });
            useExitRow.Add(useExitToggle);
            Add(useExitRow);

            exitField.RegisterValueChangedCallback(evt =>
            {
                gt.ExitTime = Mathf.Max(0f, evt.newValue);
                OnChanged?.Invoke();
            });
            exitRow.Add(exitField);
            Add(exitRow);

            // ── Conditions ──
            AddSectionHeader("CONDITIONS");

            // condBlock holds the list + the add button together so the button
            // is always directly below the last condition row.
            var condBlock = new VisualElement();
            condBlock.AddToClassList("inspector__cond-block");
            Add(condBlock);

            var condList = new VisualElement();
            condList.AddToClassList("inspector__list");
            condBlock.Add(condList);
            RefreshConditionList(condList, edge);

            if (_asset?.Blackboard?.Count > 0)
            {
                var addBtn = new Button(() =>
                {
                    gt.Conditions.Add(new FSMGraphAsset.GraphCondition
                    {
                        ParameterName = _asset.Blackboard[0].Name
                    });
                    RefreshConditionList(condList, edge);
                    OnChanged?.Invoke();
                }) { text = "+ Add Condition" };
                addBtn.AddToClassList("inspector__add-button");
                condBlock.Add(addBtn);
            }
            else
            {
                var hint = new Label("Add parameters to the Blackboard first.");
                hint.AddToClassList("inspector__hint");
                condBlock.Add(hint);
            }
        }

        private void RefreshConditionList(VisualElement container, FSMEdge edge)
        {
            container.Clear();
            var conditions = edge.GraphTransition?.Conditions;
            if (conditions == null) return;

            for (int i = 0; i < conditions.Count; i++)
                container.Add(BuildConditionRow(conditions, i, container, edge));
        }

        private VisualElement BuildConditionRow(
            List<FSMGraphAsset.GraphCondition> conditions, int idx,
            VisualElement container, FSMEdge edge)
        {
            var cond = conditions[idx];
            var row  = new VisualElement();
            row.AddToClassList("inspector__condition-row");

            var paramNames = _asset?.Blackboard?.Select(p => p.Name).ToList() ?? new List<string>();
            if (paramNames.Count == 0)
            {
                row.Add(new Label("(no parameters)"));
                return row;
            }

            var paramIndex    = Mathf.Max(0, paramNames.IndexOf(cond.ParameterName));
            var paramDropdown = new DropdownField(paramNames, paramIndex);
            paramDropdown.AddToClassList("inspector__condition-param");
            paramDropdown.RegisterValueChangedCallback(evt =>
            {
                cond.ParameterName = evt.newValue;
                RefreshConditionList(container, edge);
                OnChanged?.Invoke();
            });
            row.Add(paramDropdown);

            var param = _asset?.Blackboard?.Find(p => p.Name == cond.ParameterName);
            if (param != null)
            {
                if (param.Type == Condition.Type.Predicate)
                {
                    // The IPredicate implementation is chosen in the Blackboard panel.
                    // Nothing extra to configure here.
                    var hint = new Label("→ set in Blackboard");
                    hint.AddToClassList("inspector__hint");
                    hint.style.flexGrow = 1;
                    row.Add(hint);
                }
                else if (param.Type != Condition.Type.Trigger)
                {
                    var opDropdown = new DropdownField(OperatorLabels, (int)cond.Operator);
                    opDropdown.AddToClassList("inspector__condition-op");
                    opDropdown.RegisterValueChangedCallback(evt =>
                    {
                        cond.Operator = (Condition.Operator)OperatorLabels.IndexOf(evt.newValue);
                        OnChanged?.Invoke();
                    });
                    row.Add(opDropdown);

                    VisualElement valueField = param.Type switch
                    {
                        Condition.Type.Bool  => MakeBoolConditionField(cond),
                        Condition.Type.Int   => MakeIntConditionField(cond),
                        Condition.Type.Float => MakeFloatConditionField(cond),
                        _                    => null
                    };
                    if (valueField != null)
                    {
                        valueField.AddToClassList("inspector__condition-value");
                        row.Add(valueField);
                    }
                }
            }

            var removeBtn = new Button(() =>
            {
                conditions.RemoveAt(idx);
                RefreshConditionList(container, edge);
                OnChanged?.Invoke();
            }) { text = "✕" };
            removeBtn.AddToClassList("inspector__remove-button");
            row.Add(removeBtn);

            return row;
        }

        private Toggle MakeBoolConditionField(FSMGraphAsset.GraphCondition cond)
        {
            var f = new Toggle { value = cond.TargetBoolValue };
            f.RegisterValueChangedCallback(e => { cond.TargetBoolValue = e.newValue; OnChanged?.Invoke(); });
            return f;
        }

        private IntegerField MakeIntConditionField(FSMGraphAsset.GraphCondition cond)
        {
            var f = new IntegerField { value = cond.TargetIntValue };
            f.style.width = 60;
            f.RegisterValueChangedCallback(e => { cond.TargetIntValue = e.newValue; OnChanged?.Invoke(); });
            return f;
        }

        private FloatField MakeFloatConditionField(FSMGraphAsset.GraphCondition cond)
        {
            var f = new FloatField { value = cond.TargetFloatValue };
            f.style.width = 60;
            f.RegisterValueChangedCallback(e => { cond.TargetFloatValue = e.newValue; OnChanged?.Invoke(); });
            return f;
        }

        // ── Layout helpers ───────────────────────────────────────────────────

        private void AddSectionHeader(string text)
        {
            var label = new Label(text);
            label.AddToClassList("inspector__section-header");
            Add(label);
        }

        private VisualElement MakeRow(string labelText)
        {
            var row = new VisualElement();
            row.AddToClassList("inspector__row");
            var lbl = new Label(labelText);
            lbl.AddToClassList("inspector__row-label");
            row.Add(lbl);
            return row;
        }

        private VisualElement MakeInfoRow(string labelText, string value)
        {
            var row = MakeRow(labelText);
            var val = new Label(value);
            val.AddToClassList("inspector__row-value");
            row.Add(val);
            return row;
        }

        private VisualElement MakeRemovableRow(string text, Action onRemove)
        {
            var row = new VisualElement();
            row.AddToClassList("inspector__removable-row");
            var label = new Label(text);
            label.AddToClassList("inspector__removable-label");
            row.Add(label);
            var btn = new Button(onRemove) { text = "✕" };
            btn.AddToClassList("inspector__remove-button");
            row.Add(btn);
            return row;
        }
    }

}
