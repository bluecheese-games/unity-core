//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEditor;

namespace BlueCheese.Core.Utils.Editor
{
	[CustomEditor(typeof(PrefabCollection))]
	public class PrefabCollectionEditor : CollectionEditor
	{
		public override void OnInspectorGUI() => base.OnInspectorGUI();
	}
}
