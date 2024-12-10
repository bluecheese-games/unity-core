//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEditor;

namespace BlueCheese.Core.Utils.Editor
{
	[CustomEditor(typeof(StringCollection))]
	public class StringCollectionEditor : CollectionEditor
	{
		public override void OnInspectorGUI() => base.OnInspectorGUI();
	}
}
