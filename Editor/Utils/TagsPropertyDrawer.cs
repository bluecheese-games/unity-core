//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(Tags))]
	public class TagsPropertyDrawer : PropertyDrawer
	{
		private string _newTag = "";
		private GUIStyle _tagStyle;
		private List<List<TagLabel>> _labels;
		private float _maxWidth;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if( _labels != null)
			{
				return _labels.Count * (EditorGUIUtility.singleLineHeight + 2);
			}
			return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_tagStyle == null)
			{
				_tagStyle = new(EditorStyles.miniButton)
				{
					alignment = TextAnchor.MiddleLeft,
					fontStyle = FontStyle.Bold
				};
			}

			Tags tags = (Tags)property.boxedValue;

			var tagsProperty = property.FindPropertyRelative("_values");

			float maxWidth = position.width;
			if (maxWidth > 10 && maxWidth != _maxWidth)
			{
				_labels = null;
				_maxWidth = maxWidth;
			}

			if (maxWidth > 10 && _labels == null)
			{
				_labels = CreateLabels(maxWidth, tagsProperty);
			}

			if (_labels != null && _labels.Count > 0)
			{
				bool removed = false;
				Rect pos = position;
				pos.y += 2;
				foreach (var row in _labels)
				{
					foreach (var tagLabel in row)
					{
						GUI.backgroundColor = tagLabel.GetColor();
						pos.width = tagLabel.width;
						pos.height = EditorGUIUtility.singleLineHeight;
						GUI.Label(pos, tagLabel.tag, _tagStyle);
						var rect = pos;
						rect.xMin = rect.xMax - 13;
						rect.yMin += 3;
						GUI.contentColor = Color.gray;
						if (GUI.enabled && GUI.Button(rect, EditorIcon.Cross, GUIStyle.none))
						{
							tagsProperty.DeleteArrayElementAtIndex(tagLabel.index);
							removed = true;
						}
						pos.x += tagLabel.width + 2;
						GUI.contentColor = Color.white;
					}
					pos.x = position.x;
					pos.y += EditorGUIUtility.singleLineHeight + 2;
					GUI.backgroundColor = Color.white;
				}
				if (removed)
				{
					_labels = null;
				}
			}

			/*float totalWidth = 0;
			for (int i = 0; i < tagsProperty.arraySize; i++)
			{
				var tagProperty = tagsProperty.GetArrayElementAtIndex(i);
				float width = _tagStyle.CalcSize(new GUIContent(tagProperty.stringValue)).x + 15;
				Debug.Log($"tagProperty.stringValue: {tagProperty.stringValue} width: {width}");

				if (maxWidth > 0 && totalWidth + width > maxWidth)
				{
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel(" ");
					totalWidth = 0;
					maxWidth = position.width - 100;
					width = _tagStyle.CalcSize(new GUIContent(tagProperty.stringValue)).x + 15;
				}

				totalWidth += width;
				Debug.Log($"width: {width}, totalWidth: {totalWidth}, maxWidth: {maxWidth}");

				GUI.backgroundColor = GetColor(tagProperty.stringValue);
				GUILayout.Label(tagProperty.stringValue, _tagStyle, GUILayout.Width(width));
				var rect = GUILayoutUtility.GetLastRect();
				rect.xMin = rect.xMax - 13;
				rect.yMin += 3;
				GUI.contentColor = Color.gray;
				if (GUI.Button(rect, EditorIcon.Cross, GUIStyle.none))
				{
					tagsProperty.DeleteArrayElementAtIndex(i);
				}
				GUI.contentColor = Color.white;
			}*/
			//GUI.backgroundColor = Color.white;
			//EditorGUILayout.EndHorizontal();
			if (GUI.enabled)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Add Tag");
				_newTag = EditorGUILayout.TextField(_newTag);
				bool submit = Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return;
				bool tagIsValid = !string.IsNullOrEmpty(_newTag) && !tags.Contains(_newTag);
				GUI.enabled = tagIsValid;
				if ((submit || GUILayout.Button(EditorIcon.Plus, EditorStyles.miniButton)) && tagIsValid)
				{
					tagsProperty.arraySize++;
					tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = _newTag;
					_newTag = "";
					_labels = null;
				}
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal();
			}
		}

		private List<List<TagLabel>> CreateLabels(float maxWidth, SerializedProperty property)
		{
			var labels = new List<List<TagLabel>>();
			for (int i = 0; i < property.arraySize; i++)
			{
				var tagProperty = property.GetArrayElementAtIndex(i);
				var tag = tagProperty.stringValue;
				var label = TagLabel.Create(tag, _tagStyle, i);
				if (labels.Count > 0)
				{
					var lastRow = labels[^1];
					var lastRowWidth = lastRow.Sum(l => l.width + 2);
					if (lastRowWidth + label.width > maxWidth)
					{
						labels.Add(new List<TagLabel> { label });
					}
					else
					{
						lastRow.Add(label);
					}
				}
				else
				{
					labels.Add(new List<TagLabel> { label });
				}
			}
			return labels;
		}

		struct TagLabel
		{
			public string tag;
			public int index;
			public float width;

			public static TagLabel Create(string tag, GUIStyle style, int index)
			{
				return new()
				{
					tag = tag,
					index = index,
					width = style.CalcSize(new GUIContent(tag)).x + 15
				};
			}

			public Color GetColor()
			{
				var seed = tag.GetHashCode();
				Random.InitState(seed);
				var color = Color.white;
				color.r = Random.value;
				color.g = Random.value;
				color.b = Random.value;
				return color;
			}
		}
	}
}
