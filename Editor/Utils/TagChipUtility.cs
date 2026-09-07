using UnityEngine;

namespace BlueCheese.Core.Editor
{
	// Shared tag chip coloring, used wherever tags are rendered as colored chips
	// (TagsPropertyDrawer, Asset Bank Browser) so the color for a given tag is always the same.
	internal static class TagChipUtility
	{
		// Deterministic pastel color derived from the tag's hash code.
		// Does not modify Unity's global Random state.
		public static Color GetColor(string tag)
		{
			uint hash = (uint)tag.GetHashCode();
			float h = (hash % 360u) / 360f;
			return Color.HSVToRGB(h, 0.42f, 0.90f);
		}
	}
}
