using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using BlueCheese.Core.Utils;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// Build processor for AssetBank.
	/// - Pre-build: stage selected assets into a specific Resources folder (renamed with GUIDs).
	/// - Post-build: remove the staging folder to keep the project clean.
	/// </summary>
	public class AssetBankBuildProcessor :
		IPreprocessBuildWithReport,
		IPostprocessBuildWithReport
	{
		public int callbackOrder => 0;

		// -------------------------
		// PRE BUILD: copy selected assets into Resources/<subfolder> as GUID+extension
		// -------------------------
		public void OnPreprocessBuild(BuildReport report)
		{
			string destFolder = GetDestinationResourcesFolderAssetPath();
			EnsureResourcesFolder(destFolder);

			int copied = 0, skipped = 0, errors = 0;

			foreach (var guid in SafeGuids(GetGuidsToStage()))
			{
				try
				{
					if (TryCopyAssetByGuidToFolder(guid, destFolder))
						copied++;
					else
						skipped++;
				}
				catch (Exception ex)
				{
					errors++;
					Debug.LogError($"[AssetBank] Error on GUID {guid}: {ex.Message}");
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log($"[AssetBank] Pre-build staging complete. Copied: {copied}, Skipped: {skipped}, Errors: {errors}. Folder: {destFolder}");
		}

		// -------------------------
		// POST BUILD: remove staging folder created during pre-build
		// -------------------------
		public void OnPostprocessBuild(BuildReport report)
		{
			string destFolder = GetDestinationResourcesFolderAssetPath();

			if (AssetDatabase.IsValidFolder(destFolder))
			{
				bool ok = AssetDatabase.DeleteAsset(destFolder);
				if (!ok)
				{
					// Fallback: remove from filesystem if AssetDatabase.DeleteAsset fails
					string fsPath = AssetPathToFullPath(destFolder);
					if (!string.IsNullOrEmpty(fsPath) && Directory.Exists(fsPath))
						Directory.Delete(fsPath, true);

					AssetDatabase.Refresh();
				}

				AssetDatabase.Refresh();
				Debug.Log($"[AssetBank] Post-build: staging folder removed -> {destFolder}");
			}
			else
			{
				Debug.Log($"[AssetBank] Post-build: staging folder not found (already removed?) -> {destFolder}");
			}
		}

		public static IEnumerable<string> GetGuidsToStage()
		{
			return AssetBank.GetAllAssets()
				.Where(asset => asset.LoadMode == AssetLoadMode.Resources)
				.Select(asset => asset.Guid);
		}

		public static string GetDestinationResourcesFolderAssetPath()
		{
			string assetPath = AssetBank.GetPath();
			if (string.IsNullOrEmpty(assetPath))
			{
				throw new InvalidOperationException("[AssetBank] Could not determine AssetBank asset path.");
			}

			string assetDir = Path.GetDirectoryName(assetPath);
			if (string.IsNullOrEmpty(assetDir))
			{
				throw new InvalidOperationException("[AssetBank] Could not determine AssetBank asset directory.");
			}

			// Ensure we have a Resources root; if AssetBank isn't inside one, fall back to Assets/Resources
			string normalized = assetDir.Replace("\\", "/");
			string resourcesRoot = normalized.Contains("/Resources")
				? normalized[..(normalized.LastIndexOf("/Resources") + "/Resources".Length)]
				: "Assets/Resources";

			return $"{resourcesRoot}/{AssetBank.AssetsResourcePath}";
		}

		// -------------------------
		// Helpers
		// -------------------------

		private static IEnumerable<string> SafeGuids(IEnumerable<string> guids)
		{
			if (guids == null) yield break;
			foreach (var g in guids)
			{
				if (string.IsNullOrWhiteSpace(g)) continue;
				yield return g.Trim();
			}
		}

		/// <summary>
		/// Ensures that the full destination folder (including Resources) exists.
		/// If any intermediate directories are missing, they are created.
		/// </summary>
		private static void EnsureResourcesFolder(string fullResourcesFolderPath)
		{
			if (string.IsNullOrEmpty(fullResourcesFolderPath) ||
				!fullResourcesFolderPath.StartsWith("Assets/", StringComparison.Ordinal))
			{
				throw new InvalidOperationException(
					$"Invalid Resources folder path: '{fullResourcesFolderPath}'. It must start with 'Assets/'.");
			}

			// Split into parts and create missing folders recursively
			var parts = fullResourcesFolderPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2)
				throw new InvalidOperationException($"Invalid Resources folder path: '{fullResourcesFolderPath}'.");

			string current = parts[0]; // "Assets"
			for (int i = 1; i < parts.Length; i++)
			{
				string next = parts[i];
				string combined = $"{current}/{next}";

				if (!AssetDatabase.IsValidFolder(combined))
				{
					string parent = current;
					string folderName = next;
					string guid = AssetDatabase.CreateFolder(parent, folderName);

					if (string.IsNullOrEmpty(guid))
						throw new IOException($"Failed to create folder '{combined}'.");
				}

				current = combined;
			}
		}

		private static bool TryCopyAssetByGuidToFolder(string guid, string destFolderAssetPath)
		{
			string sourceAssetPath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(sourceAssetPath))
			{
				Debug.LogWarning($"[AssetBank] GUID not found: {guid}");
				return false;
			}

			if (AssetDatabase.IsValidFolder(sourceAssetPath))
			{
				Debug.LogWarning($"[AssetBank] GUID points to a folder, skipped: {guid} ({sourceAssetPath})");
				return false;
			}

			string ext = Path.GetExtension(sourceAssetPath);
			string destAssetPath = $"{destFolderAssetPath}/{guid}{ext}";

			if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destAssetPath) != null)
			{
				AssetDatabase.DeleteAsset(destAssetPath);
			}

			bool ok = AssetDatabase.CopyAsset(sourceAssetPath, destAssetPath);
			if (!ok)
			{
				Debug.LogError($"[AssetBank] Copy failed: {sourceAssetPath} -> {destAssetPath}");
				return false;
			}

			return true;
		}

		private static string AssetPathToFullPath(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath)) return null;
			string projectPath = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
			string relative = assetPath.Replace('\\', '/');
			if (!relative.StartsWith("Assets/")) return null;
			return Path.Combine(projectPath, relative).Replace('\\', '/');
		}
	}
}
