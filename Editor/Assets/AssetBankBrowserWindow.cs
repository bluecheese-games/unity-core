using BlueCheese.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// UI Toolkit browser listing every asset registered in the <see cref="AssetBank"/>, one row per
	/// asset, with the same filters as <see cref="AssetBankEditor"/>. Reachable from
	/// Tools/BlueCheese/Asset Bank Browser or the "Browse Assets" button on the AssetBank inspector.
	/// </summary>
	public class AssetBankBrowserWindow : EditorWindow
	{
		private const string Title = "Asset Bank Browser";

		// Column layout shared between the header row and asset rows. Name/Tags grow to fill the
		// remaining space; the rest are fixed-width and never shrink, so they don't get squeezed out
		// when Name/Tags claim the available room.
		private const float NameFlexGrow = 1f;
		private const float NameMinWidth = 120f;
		private const float TagsFlexGrow = 1f;
		private const float TagsMinWidth = 160f;
		private const float TypeWidth = 160f;
		private const float LoadModeWidth = 110f;
		private const float BundleKeyWidth = 150f;
		private const float StatusCellWidth = 36f; // dot + ref-count label, see BuildStatusCell

		// How often the "loaded" indicators are refreshed against live AssetBaseRef state
		// (e.g. while the game is playing). Lightweight: only updates dot colors, not the whole list.
		private const double IndicatorRefreshInterval = 1.0;

		private static readonly Color RowAlternateColor = new(0f, 0f, 0f, 0.06f);
		private static readonly Color RowHoverColor = new(0.24f, 0.49f, 0.9f, 0.20f);
		private static readonly Color LoadedColor = new(0.35f, 0.85f, 0.4f);
		private static readonly Color UnloadedBorderColor = new(1f, 1f, 1f, 0.25f);

		private List<AssetBaseRef> _allRefs = new();
		private readonly List<(AssetBaseRef assetRef, VisualElement dot, Label refCountLabel)> _loadedIndicators = new();
		private double _lastIndicatorRefresh;

		private string _search = string.Empty;
		private TextField _searchField;
		private DropdownField _loadModeDropdown;
		private DropdownField _tagDropdown;
		private DropdownField _bundleDropdown;
		private ScrollView _rowsContainer;
		private Label _countLabel;

		[MenuItem("Tools/BlueCheese/Asset Bank Browser")]
		public static void Open()
		{
			var window = GetWindow<AssetBankBrowserWindow>();
			window.titleContent = new GUIContent(Title);
			window.minSize = new Vector2(760, 400);
			window.Show();
		}

		private void OnEnable()
		{
			titleContent = new GUIContent(Title);
			EditorApplication.update += OnEditorUpdate;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		// Keeps the list in sync with edits made elsewhere (inspector, regenerate) while the window is open.
		private void OnFocus() => RefreshData();

		// Refresh immediately on Play Mode transitions instead of waiting for the throttled tick, so
		// the dots don't sit green for up to a second after Stop (or gray for a second after Play).
		private void OnPlayModeStateChanged(PlayModeStateChange change) => RefreshLoadedIndicators();

		// Throttled tick so the loaded/unloaded dots stay live while assets stream in and out at
		// runtime, without rebuilding the whole row list (and its dropdown choices) every frame.
		private void OnEditorUpdate()
		{
			double now = EditorApplication.timeSinceStartup;
			if (now - _lastIndicatorRefresh < IndicatorRefreshInterval) return;
			_lastIndicatorRefresh = now;

			RefreshLoadedIndicators();
		}

		private void RefreshLoadedIndicators()
		{
			foreach (var (assetRef, dot, refCountLabel) in _loadedIndicators)
				ApplyLoadedState(dot, refCountLabel, assetRef);
		}

		private void CreateGUI()
		{
			var root = rootVisualElement;
			root.style.flexGrow = 1;

			root.Add(BuildToolbar());

			_countLabel = new Label { style = { flexShrink = 0, marginLeft = 6, marginTop = 4, marginBottom = 2, opacity = 0.7f } };
			root.Add(_countLabel);

			root.Add(BuildHeaderRow());

			_rowsContainer = new ScrollView { style = { flexGrow = 1 } };
			root.Add(_rowsContainer);

			RefreshData();
		}

		#region Toolbar / Header

		private VisualElement BuildToolbar()
		{
			var bar = new VisualElement { style = { flexShrink = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center, flexWrap = Wrap.Wrap, paddingLeft = 6, paddingRight = 6, paddingTop = 6, paddingBottom = 4 } };

			var searchIcon = new Image { image = EditorIcon.Search, style = { width = 16, height = 16, marginRight = 2 } };
			bar.Add(searchIcon);

			_searchField = new TextField { value = _search, tooltip = "Filter by name / GUID / tag", style = { flexGrow = 1, minWidth = 120, marginRight = 8 } };
			_searchField.RegisterValueChangedCallback(evt => { _search = evt.newValue; RefreshRows(); });
			bar.Add(_searchField);

			_loadModeDropdown = new DropdownField("Load Mode", AssetBankFilterUtility.LoadModeOptions.ToList(), 0) { style = { width = 260, marginRight = 8 } };
			_loadModeDropdown.RegisterValueChangedCallback(_ => RefreshRows());
			StyleFilterLabel(_loadModeDropdown);
			bar.Add(_loadModeDropdown);

			_tagDropdown = new DropdownField("Tag", new List<string> { "All" }, 0) { style = { width = 260, marginRight = 8 } };
			_tagDropdown.RegisterValueChangedCallback(_ => RefreshRows());
			StyleFilterLabel(_tagDropdown);
			bar.Add(_tagDropdown);

			_bundleDropdown = new DropdownField("Bundle", new List<string> { "All" }, 0) { style = { width = 260, marginRight = 8 } };
			_bundleDropdown.RegisterValueChangedCallback(_ => RefreshRows());
			StyleFilterLabel(_bundleDropdown);
			bar.Add(_bundleDropdown);

			bar.Add(new Button(RefreshData) { text = "Refresh", style = { marginRight = 4 } });
			bar.Add(new Button(ClearFilters) { text = "Clear" });

			return bar;
		}

		// Shrinks a filter dropdown's label to its text width and right-aligns it, so it sits flush
		// against the control instead of a fixed-width, left-aligned column with a big gap.
		private static void StyleFilterLabel(DropdownField dropdown)
		{
			dropdown.labelElement.style.minWidth = 0;
			dropdown.labelElement.style.unityTextAlign = TextAnchor.MiddleRight;
			dropdown.labelElement.style.marginRight = 4;
		}

		private VisualElement BuildHeaderRow()
		{
			var header = new VisualElement { style = { flexShrink = 0, flexDirection = FlexDirection.Row, paddingLeft = 6, paddingRight = 6, paddingBottom = 4, opacity = 0.7f } };
			header.Add(new VisualElement { tooltip = "Loaded / active reference count", style = { width = StatusCellWidth, flexShrink = 0 } }); // spacer above the status column
			header.Add(new Label("Name") { style = { flexGrow = NameFlexGrow, flexBasis = 0, minWidth = NameMinWidth, unityFontStyleAndWeight = FontStyle.Bold } });
			header.Add(new Label("Tags") { style = { flexGrow = TagsFlexGrow, flexBasis = 0, minWidth = TagsMinWidth, unityFontStyleAndWeight = FontStyle.Bold } });
			header.Add(new Label("Type") { style = { width = TypeWidth, flexShrink = 0, unityFontStyleAndWeight = FontStyle.Bold } });
			header.Add(new Label("Load Mode") { style = { width = LoadModeWidth, flexShrink = 0, unityFontStyleAndWeight = FontStyle.Bold } });
			header.Add(new Label("Bundle Key") { style = { width = BundleKeyWidth, flexShrink = 0, unityFontStyleAndWeight = FontStyle.Bold } });
			return header;
		}

		#endregion

		#region Data

		private void RefreshData()
		{
			// AssetBank lazily initializes from Resources on first static access, so this also
			// works the very first time the window is opened.
			AssetBank.Initialize();
			_allRefs = AssetBank.GetAllAssets().ToList();

			if (_tagDropdown == null) return; // CreateGUI hasn't run yet

			RebuildDropdownChoices(_tagDropdown, AssetBankFilterUtility.BuildTagOptions(_allRefs));
			RebuildDropdownChoices(_bundleDropdown, AssetBankFilterUtility.BuildBundleOptions(_allRefs));

			RefreshRows();
		}

		// Rebuilds a dropdown's choice list while preserving the current selection when it still exists.
		private static void RebuildDropdownChoices(DropdownField dropdown, string[] options)
		{
			string current = dropdown.value;
			dropdown.choices = options.ToList();
			int index = Array.IndexOf(options, current);
			dropdown.SetValueWithoutNotify(options[index >= 0 ? index : 0]);
		}

		private void ClearFilters()
		{
			_search = string.Empty;
			_searchField.SetValueWithoutNotify(string.Empty);
			_loadModeDropdown.SetValueWithoutNotify(AssetBankFilterUtility.LoadModeOptions[0]);
			_tagDropdown.SetValueWithoutNotify(_tagDropdown.choices.Count > 0 ? _tagDropdown.choices[0] : "All");
			_bundleDropdown.SetValueWithoutNotify(_bundleDropdown.choices.Count > 0 ? _bundleDropdown.choices[0] : "All");
			RefreshRows();
		}

		#endregion

		#region Rows

		private void RefreshRows()
		{
			_rowsContainer.Clear();
			_loadedIndicators.Clear();

			AssetLoadMode? loadMode = _loadModeDropdown.index <= 0 ? null : (AssetLoadMode)(_loadModeDropdown.index - 1);
			string tag = _tagDropdown.index <= 0 ? null : _tagDropdown.value;
			string bundle = _bundleDropdown.index <= 0 ? null : _bundleDropdown.value;
			string search = _search?.Trim();

			var matches = _allRefs.Where(r => AssetBankFilterUtility.Matches(r, search, loadMode, tag, bundle)).ToList();

			_countLabel.text = $"Assets ({matches.Count}/{_allRefs.Count})";

			if (matches.Count == 0)
			{
				_rowsContainer.Add(new Label("No asset matches the current filters.") { style = { opacity = 0.6f, marginLeft = 8, marginTop = 8 } });
				return;
			}

			for (int i = 0; i < matches.Count; i++)
			{
				var (row, dot, refCountLabel) = BuildRow(matches[i], alternate: i % 2 == 1);
				_rowsContainer.Add(row);
				_loadedIndicators.Add((matches[i], dot, refCountLabel));
			}
		}

		// Builds one row. The entire row is clickable (selects + pings the asset in the Project window)
		// instead of a dedicated button; returns the loaded-indicator dot/label so they can be
		// refreshed live.
		private static (VisualElement row, VisualElement dot, Label refCountLabel) BuildRow(AssetBaseRef assetRef, bool alternate)
		{
			Color baseColor = alternate ? RowAlternateColor : Color.clear;

			var row = new VisualElement
			{
				tooltip = $"Click to select \"{assetRef.Name}\" in the Project window",
				style =
				{
					flexDirection = FlexDirection.Row, alignItems = Align.Center,
					paddingLeft = 6, paddingRight = 6, paddingTop = 2, paddingBottom = 2,
					backgroundColor = baseColor,
				},
			};
			row.RegisterCallback<ClickEvent>(_ => SelectAsset(assetRef));
			row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = RowHoverColor);
			row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = baseColor);

			var statusCell = BuildStatusCell(assetRef, out var dot, out var refCountLabel);
			row.Add(statusCell);

			row.Add(new Label(assetRef.Name) { tooltip = assetRef.Name, style = { flexGrow = NameFlexGrow, flexBasis = 0, minWidth = NameMinWidth, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } });

			row.Add(BuildTagsCell(assetRef));

			string shortType = AssetBankFilterUtility.GetShortTypeName(assetRef);
			row.Add(new Label(shortType) { tooltip = assetRef.TypeName, style = { width = TypeWidth, flexShrink = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } });

			row.Add(new Label(assetRef.LoadMode.ToString()) { style = { width = LoadModeWidth, flexShrink = 0 } });

			string bundleText = string.IsNullOrEmpty(assetRef.BundleKey) ? "-" : assetRef.BundleKey;
			row.Add(new Label(bundleText) { tooltip = bundleText, style = { width = BundleKeyWidth, flexShrink = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } });

			return (row, dot, refCountLabel);
		}

		// Status cell at the start of the row: a colored dot (filled green when the asset is currently
		// loaded and cached in memory, a faint outline otherwise) plus the active reference count
		// acquired via AssetBaseRef.Load/Release (blank when there is none to show). Only lit up during
		// Play Mode -- see ApplyLoadedState for why Edit Mode is always shown as unloaded.
		private static VisualElement BuildStatusCell(AssetBaseRef assetRef, out VisualElement dot, out Label refCountLabel)
		{
			var cell = new VisualElement
			{
				style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, width = StatusCellWidth, flexShrink = 0, marginRight = 6 },
			};

			dot = new VisualElement
			{
				style =
				{
					width = 8, height = 8, marginRight = 4, flexShrink = 0,
					borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
					borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1,
					borderLeftColor = UnloadedBorderColor, borderRightColor = UnloadedBorderColor,
					borderTopColor = UnloadedBorderColor, borderBottomColor = UnloadedBorderColor,
				},
			};
			cell.Add(dot);

			refCountLabel = new Label { style = { fontSize = 10, opacity = 0.75f, unityTextAlign = TextAnchor.MiddleLeft } };
			cell.Add(refCountLabel);

			ApplyLoadedState(dot, refCountLabel, assetRef);
			return cell;
		}

		// In the Editor, AssetBaseRef.TryLoad always resolves through an "Editor shortcut" straight to
		// the AssetDatabase before ever touching Resources/Addressables, so _loadedAsset gets cached
		// almost as soon as anything looks at the asset -- it doesn't reflect real runtime memory
		// residency. Only Play Mode exercises the actual configured load path, so the indicator is
		// forced to "unloaded" outside Play Mode to avoid a misleading always-green dot in Edit Mode.
		private static void ApplyLoadedState(VisualElement dot, Label refCountLabel, AssetBaseRef assetRef)
		{
			Color color;
			string tooltip;
			string countText;

			if (!EditorApplication.isPlaying)
			{
				color = Color.clear;
				tooltip = "Not loaded (loaded state is only tracked during Play Mode)";
				countText = "-";
			}
			else
			{
				bool loaded = assetRef.IsLoaded;
				int refCount = assetRef.RefCount;
				color = loaded ? LoadedColor : Color.clear;
				// "-" rather than "0" for uncounted assets (loaded via TryLoad/Get*, which never
				// touches RefCount) -- distinguishes "nothing holds a counted reference" from an
				// actual 0 you'd expect to see moving as Load/Release calls come in.
				countText = refCount > 0 ? refCount.ToString() : "-";
				tooltip = loaded
					? (refCount > 0 ? $"Loaded in memory — {refCount} active reference(s)" : "Loaded in memory (via TryLoad/Get*, not reference-counted)")
					: "Not loaded";
			}

			// Skip no-op writes so the periodic tick doesn't needlessly touch style/tooltip every second.
			// (Tooltips not appearing during Play Mode is unrelated -- Unity disables Editor tooltips
			// globally while playing; see Preferences > General > "Enable PlayMode Tooltips".)
			if (dot.style.backgroundColor.value != color)
				dot.style.backgroundColor = color;
			if (dot.tooltip != tooltip)
				dot.tooltip = tooltip;
			if (refCountLabel.text != countText)
				refCountLabel.text = countText;
		}

		// Renders tags the same way as the AssetBase inspector (TagsPropertyDrawer): colored chips,
		// wrapping onto multiple lines, sharing the same deterministic per-tag color.
		private static VisualElement BuildTagsCell(AssetBaseRef assetRef)
		{
			var cell = new VisualElement
			{
				style =
				{
					flexGrow = TagsFlexGrow, flexBasis = 0, minWidth = TagsMinWidth,
					flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, alignItems = Align.Center,
				},
			};

			string[] tags = assetRef.Tags;
			if (tags == null || tags.Length == 0)
			{
				cell.Add(new Label("None") { style = { opacity = 0.5f } });
				return cell;
			}

			foreach (string tag in tags)
			{
				cell.Add(new Label(tag)
				{
					tooltip = tag,
					style =
					{
						backgroundColor = TagChipUtility.GetColor(tag),
						color = Color.black,
						fontSize = 11,
						unityTextAlign = TextAnchor.MiddleCenter,
						paddingLeft = 6, paddingRight = 6, paddingTop = 1, paddingBottom = 1,
						marginRight = 4, marginTop = 1, marginBottom = 1,
						borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
					},
				});
			}

			return cell;
		}

		// Resolves the asset via the AssetDatabase directly rather than AssetBaseRef.TryLoad: TryLoad
		// caches into _loadedAsset (and its Editor shortcut succeeds trivially for any project asset),
		// so using it here would make merely clicking a row look identical to the asset being
		// genuinely loaded by gameplay code through the AssetBank API — defeating the loaded indicator.
		private static void SelectAsset(AssetBaseRef assetRef)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(assetRef.Guid);
			var asset = AssetDatabase.LoadAssetAtPath<AssetBase>(assetPath);
			if (asset != null)
			{
				Selection.activeObject = asset;
				EditorGUIUtility.PingObject(asset);
			}
		}

		#endregion
	}
}
