//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueCheese.Core.FSM;
using BlueCheese.Core.FSM.Graph;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Core.FSM.Editor
{
    /// <summary>
    /// Bottom panel that manages the FSM blackboard parameters.
    /// Edits go directly through the asset's Blackboard list references.
    /// </summary>
    public class FSMBlackboardPanel : VisualElement
    {
        public event Action OnChanged;

        private FSMGraphAsset _asset;
        private VisualElement _paramList;

        public FSMBlackboardPanel()
        {
            AddToClassList("blackboard");

            // ── Header ──
            var header = new VisualElement();
            header.AddToClassList("blackboard__header");

            var title = new Label("Blackboard");
            title.AddToClassList("blackboard__title");
            header.Add(title);

            var addBtn = new Button(ShowAddParamMenu) { text = "+ Add" };
            addBtn.AddToClassList("blackboard__add-button");
            header.Add(addBtn);

            Add(header);

            // ── Scrollable list ──
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("blackboard__scroll");

            _paramList = new VisualElement();
            _paramList.AddToClassList("blackboard__list");
            scroll.Add(_paramList);

            Add(scroll);
        }

        public void Load(FSMGraphAsset asset)
        {
            _asset = asset;
            RefreshList();
        }

        private void RefreshList()
        {
            _paramList.Clear();
            if (_asset?.Blackboard == null) return;

            for (int i = 0; i < _asset.Blackboard.Count; i++)
                _paramList.Add(BuildParamRow(_asset.Blackboard[i], i));
        }

        private VisualElement BuildParamRow(FSMGraphAsset.GraphParameter param, int idx)
        {
            var row = new VisualElement();
            row.AddToClassList("blackboard__row");

            // ── Badge ──
            var badge = new Label(param.Type.ToString()[0].ToString().ToUpper());
            badge.AddToClassList("blackboard__type-badge");
            badge.AddToClassList($"blackboard__type--{param.Type.ToString().ToLower()}");
            badge.tooltip = param.Type.ToString();
            row.Add(badge);

            // ── Name ──
            var nameField = new TextField { value = param.Name };
            nameField.AddToClassList("blackboard__name-field");
            nameField.RegisterValueChangedCallback(evt => { param.Name = evt.newValue; OnChanged?.Invoke(); });
            row.Add(nameField);

            // ── Value / predicate section ──
            if (param.Type == Condition.Type.Predicate)
            {
                // Type picker button
                var shortName = string.IsNullOrEmpty(param.PredicateTypeName) ? "(none)"
                    : param.PredicateTypeName.Contains(",")
                        ? param.PredicateTypeName.Split(',')[0].Split('.').Last()
                        : param.PredicateTypeName.Split('.').Last();

                var pickBtn = new Button(() => ShowPredicateTypeMenu(param, idx)) { text = shortName };
                pickBtn.AddToClassList("blackboard__predicate-btn");
                row.Add(pickBtn);

                // Inline field controls — one per public field on the predicate class
                if (!string.IsNullOrEmpty(param.PredicateTypeName))
                {
                    var type = Type.GetType(param.PredicateTypeName);
                    if (type != null)
                    {
                        var instance = Activator.CreateInstance(type);
                        if (!string.IsNullOrEmpty(param.PredicateJson))
                            JsonUtility.FromJsonOverwrite(param.PredicateJson, instance);

                        void Serialize() { param.PredicateJson = JsonUtility.ToJson(instance); OnChanged?.Invoke(); }

                        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                        {
                            var ctrl = BuildInlineFieldControl(field, instance, Serialize);
                            if (ctrl != null) row.Add(ctrl);
                        }
                    }
                }
            }
            else if (param.Type != Condition.Type.Trigger)
            {
                VisualElement defaultField = param.Type switch
                {
                    Condition.Type.Bool  => MakeBoolDefault(param),
                    Condition.Type.Int   => MakeIntDefault(param),
                    Condition.Type.Float => MakeFloatDefault(param),
                    _                    => null
                };
                if (defaultField != null)
                {
                    defaultField.AddToClassList("blackboard__default-field");
                    row.Add(defaultField);
                }
            }

            // ── Remove ──
            var removeBtn = new Button(() => { _asset.Blackboard.RemoveAt(idx); RefreshList(); OnChanged?.Invoke(); })
                { text = "✕" };
            removeBtn.AddToClassList("blackboard__remove-button");
            row.Add(removeBtn);

            return row;
        }

        /// <summary>
        /// Build a compact, label-free control for a single predicate field.
        /// The field name appears as a tooltip.
        /// </summary>
        private VisualElement BuildInlineFieldControl(FieldInfo field, object instance, Action onChanged)
        {
            var value = field.GetValue(instance);

            VisualElement ctrl = null;

            if (field.FieldType == typeof(bool))
            {
                var f = new Toggle { value = (bool)value, tooltip = field.Name };
                f.RegisterValueChangedCallback(e => { field.SetValue(instance, e.newValue); onChanged(); });
                ctrl = f;
            }
            else if (field.FieldType == typeof(int))
            {
                var f = new IntegerField { value = (int)value, tooltip = field.Name };
                f.RegisterValueChangedCallback(e => { field.SetValue(instance, e.newValue); onChanged(); });
                ctrl = f;
            }
            else if (field.FieldType == typeof(float))
            {
                var f = new FloatField { value = (float)value, tooltip = field.Name };
                f.RegisterValueChangedCallback(e => { field.SetValue(instance, e.newValue); onChanged(); });
                ctrl = f;
            }
            else if (field.FieldType == typeof(string))
            {
                var f = new TextField { value = (string)value ?? "", tooltip = field.Name };
                f.RegisterValueChangedCallback(e => { field.SetValue(instance, e.newValue); onChanged(); });
                ctrl = f;
            }
            else if (field.FieldType.IsEnum)
            {
                var names   = Enum.GetNames(field.FieldType).ToList();
                var current = Mathf.Max(0, names.IndexOf(value?.ToString() ?? ""));
                var f = new DropdownField(names, current) { tooltip = field.Name };
                f.RegisterValueChangedCallback(e =>
                {
                    field.SetValue(instance, Enum.Parse(field.FieldType, e.newValue));
                    onChanged();
                });
                ctrl = f;
            }
            else if (field.FieldType == typeof(Vector2))
            {
                var f = new Vector2Field { value = (Vector2)value, tooltip = field.Name };
                f.RegisterValueChangedCallback(e => { field.SetValue(instance, e.newValue); onChanged(); });
                ctrl = f;
            }
            else if (field.FieldType == typeof(Vector3))
            {
                var f = new Vector3Field { value = (Vector3)value, tooltip = field.Name };
                f.RegisterValueChangedCallback(e => { field.SetValue(instance, e.newValue); onChanged(); });
                ctrl = f;
            }
            else if (field.FieldType == typeof(Color))
            {
                var f = new ColorField { value = (Color)value, tooltip = field.Name };
                f.RegisterValueChangedCallback(e => { field.SetValue(instance, e.newValue); onChanged(); });
                ctrl = f;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                var f = new ObjectField { objectType = field.FieldType, value = value as UnityEngine.Object, tooltip = field.Name };
                f.RegisterValueChangedCallback(e => { field.SetValue(instance, e.newValue); onChanged(); });
                ctrl = f;
            }

            if (ctrl == null) return null;
            ctrl.AddToClassList("blackboard__predicate-inline-field");
            return ctrl;
        }

        // ── Default value field builders ─────────────────────────────────────

        private Toggle MakeBoolDefault(FSMGraphAsset.GraphParameter p)
        {
            var f = new Toggle { value = p.DefaultBoolValue };
            f.RegisterValueChangedCallback(e => { p.DefaultBoolValue = e.newValue; OnChanged?.Invoke(); });
            return f;
        }

        private IntegerField MakeIntDefault(FSMGraphAsset.GraphParameter p)
        {
            var f = new IntegerField { value = p.DefaultIntValue };
            f.style.width = 60;
            f.RegisterValueChangedCallback(e => { p.DefaultIntValue = e.newValue; OnChanged?.Invoke(); });
            return f;
        }

        private FloatField MakeFloatDefault(FSMGraphAsset.GraphParameter p)
        {
            var f = new FloatField { value = p.DefaultFloatValue };
            f.style.width = 60;
            f.RegisterValueChangedCallback(e => { p.DefaultFloatValue = e.newValue; OnChanged?.Invoke(); });
            return f;
        }

        // ── Add parameter menu ───────────────────────────────────────────────

        private void ShowPredicateTypeMenu(FSMGraphAsset.GraphParameter param, int idx)
        {
            var menu  = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<IPredicate>()
                .Where(t => !t.IsAbstract && !t.IsInterface
                            && t.GetConstructor(Type.EmptyTypes) != null);

            menu.AddItem(new GUIContent("(none)"),
                string.IsNullOrEmpty(param.PredicateTypeName), () =>
                {
                    param.PredicateTypeName = null;
                    RefreshList();
                    OnChanged?.Invoke();
                });

            foreach (var type in types)
            {
                var t       = type;
                var current = param.PredicateTypeName == t.AssemblyQualifiedName;
                menu.AddItem(new GUIContent(t.FullName.Replace('.', '/')), current, () =>
                {
                    param.PredicateTypeName = t.AssemblyQualifiedName;
                    RefreshList();
                    OnChanged?.Invoke();
                });
            }

            if (menu.GetItemCount() == 1)
                menu.AddDisabledItem(new GUIContent("No IPredicate types found"));

            menu.ShowAsContext();
        }

        private void ShowAddParamMenu()
        {
            if (_asset == null) return;

            var menu = new GenericMenu();
            foreach (Condition.Type type in Enum.GetValues(typeof(Condition.Type)))
            {
                var t = type;
                menu.AddItem(new GUIContent(type.ToString()), false, () =>
                {
                    _asset.Blackboard.Add(new FSMGraphAsset.GraphParameter
                    {
                        Name = GenerateParamName(t),
                        Type = t
                    });
                    RefreshList();
                    OnChanged?.Invoke();
                });
            }
            menu.ShowAsContext();
        }

        private string GenerateParamName(Condition.Type type)
        {
            if (_asset == null) return $"New{type}";
            var existing = new System.Collections.Generic.HashSet<string>(
                _asset.Blackboard.ConvertAll(p => p.Name));
            for (int i = 1; ; i++)
            {
                var name = $"New{type}{i}";
                if (!existing.Contains(name)) return name;
            }
        }
    }
}
