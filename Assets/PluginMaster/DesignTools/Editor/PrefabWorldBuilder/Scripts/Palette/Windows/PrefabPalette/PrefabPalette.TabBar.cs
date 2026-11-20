/*
Copyright (c) Omar Duarte
Unauthorized copying of this file, via any medium is strictly prohibited.
Writen by Omar Duarte.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#pragma warning disable UDR0001
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    public partial class PrefabPalette : UnityEditor.EditorWindow, ISerializationCallbackReceiver
    {
        private GUIContent _pinTabIcon = null;
        private GUIContent _pinnedTabIcon = null;
        private GUIContent _pinTabLightIcon = null;
        private GUIContent _pinnedTabLightIcon = null;
        private GUIContent pinTabIcon
        {
            get
            {
                if (_pinTabIcon.image == null)
                    _pinTabIcon.image = Resources.Load<Texture2D>("Sprites/PinTab");
                if (_pinTabLightIcon.image == null)
                    _pinTabLightIcon.image = Resources.Load<Texture2D>("Sprites/LightTheme/PinTab");
                return UnityEditor.EditorGUIUtility.isProSkin ? _pinTabIcon : _pinTabLightIcon;
            }
        }
        private GUIContent pinnedTabIcon
        {
            get
            {
                if (_pinnedTabIcon.image == null)
                    _pinnedTabIcon.image = Resources.Load<Texture2D>("Sprites/PinnedTab");
                if (_pinnedTabLightIcon.image == null)
                    _pinnedTabLightIcon.image = Resources.Load<Texture2D>("Sprites/LightTheme/PinnedTab");
                return UnityEditor.EditorGUIUtility.isProSkin ? _pinnedTabIcon : _pinnedTabLightIcon;
            }
        }
        #region RENAME
        private class RenamePaletteWindow : UnityEditor.EditorWindow
        {
            private string _name = string.Empty;
            private System.Action<RenameData> _onDone;
            private bool _focusSet = false;
            private int _delayFrames = 0;
            RenameData data;

            public static void ShowWindow(RenameData data, System.Action<RenameData> onDone)
            {
                var window = GetWindow<RenamePaletteWindow>(true, "Rename Palette");
                window.data = data;
                window._name = data.newName;
                window._onDone = onDone;
                window.position = new Rect(data.mousePosition.x + 50, data.mousePosition.y + 50, 0, 0);
                window.minSize = window.maxSize = new Vector2(160, 45);
                window._focusSet = false;
                window._delayFrames = 0;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
            private void OnGUI()
            {
                _delayFrames++;

                UnityEditor.EditorGUIUtility.labelWidth = 50;
                UnityEditor.EditorGUIUtility.fieldWidth = 70;
                GUI.SetNextControlName("NameField");
                _name = UnityEditor.EditorGUILayout.TextField(_name);

                if (!_focusSet && _delayFrames > 2 && Event.current.type == EventType.Repaint)
                {
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        if (this != null)
                        {
                            UnityEditor.EditorGUI.FocusTextInControl("NameField");
                            Repaint();
                        }
                    };
                    _focusSet = true;
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
                {
                    if (!string.IsNullOrWhiteSpace(_name))
                    {
                        data.newName = _name;
                        _onDone(data);
                        Close();
                    }
                    Event.current.Use();
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                }

                using (new UnityEditor.EditorGUI.DisabledGroupScope(string.IsNullOrWhiteSpace(_name)))
                {
                    if (GUILayout.Button("Apply"))
                    {
                        data.newName = _name;
                        _onDone(data);
                        Close();
                    }
                }
            }
        }

        private struct RenameData
        {
            public readonly PaletteData palette;
            public readonly Vector2 mousePosition;
            public string newName;

            public RenameData(PaletteData palette, string newName, Vector2 mousePosition)
                => (this.palette, this.newName, this.mousePosition) = (palette, newName, mousePosition);
        }
        private void ShowRenamePaletteWindow(object obj)
        {
            if (!(obj is RenameData)) return;
            var data = (RenameData)obj;
            RenamePaletteWindow.ShowWindow(data, RenamePalette);
        }
        private void RenamePalette(RenameData data)
        {
            RegisterUndo("Rename Palette");
            data.palette.name = data.newName;
            Repaint();
        }
        #endregion

        private void ShowDeleteConfirmation(object obj)
        {
            var palette = (PaletteData)obj;
            if (UnityEditor.EditorUtility.DisplayDialog("Delete Palette: " + palette.name,
                "Are you sure you want to delete this palette?\n" + palette.name, "Delete", "Cancel"))
            {
                RegisterUndo("Remove Palette");
                var isSelected = PaletteManager.IsPaletteSelected(palette);
                PaletteManager.RemovePalette(palette);
                if (PaletteManager.allPalettesCount == 0) CreatePalette();
                else if (isSelected) PaletteManager.SelectPreviousPalette();
                PaletteManager.selectedBrushIdx = -1;

                _updateTabSize = true;
                UpdateFilteredList(false);
                Repaint();
            }
        }

        #region TAB BUTTONS
        private bool _updateTabSize = false;

        public static void UpdateTabBar()
        {
            if (instance == null) return;
            instance._updateTabSize = true;

        }
        public void SelectPalette(PaletteData palette)
        {
            if (palette == null) return;
            PaletteManager.selectedPalette = palette;
            PaletteManager.selectedBrushIdx = -1;
            PaletteManager.ClearSelection();
            _updateTabSize = true;
            OnPaletteChange();
        }

        private void SelectPalette(object obj)
        {
            SelectPalette((PaletteData)obj);
            if (PaletteManager.showTabsInMultipleRows) return;
            PaletteManager.ShowSelectedFirst();
        }

        private void CreatePalette()
        {
            var palette = new PaletteData("Palette" + (PaletteManager.nonPinnedCount + 1),
                System.DateTime.Now.ToBinary());
            PaletteManager.AddPalette(palette, save: true);
            SelectPalette(palette);
            UpdateTabBar();
        }
        private void DuplicatePalette(object obj)
        {
            var palette = (PaletteData)obj;
            PaletteManager.DuplicatePalette(palette);
            UpdateTabBar();
            RepaintWindow();
        }

        private void ToggleMultipleRows()
            => PaletteManager.showTabsInMultipleRows = !PaletteManager.showTabsInMultipleRows;
        private System.Collections.Generic.Dictionary<long, (PaletteData palette, Rect rect)> _tabRects
            = new System.Collections.Generic.Dictionary<long, (PaletteData, Rect)>();
        private System.Collections.Generic.Dictionary<long, float> _tabSize
            = new System.Collections.Generic.Dictionary<long, float>();
        private void TabBar()
        {
            HandleTabBarContextClick();
            if (Event.current.type == EventType.Repaint) _tabRects.Clear();
            DrawPinnedTabsIfAvailable();
            DrawNonPinnedTabsIfAvailable();
            RecalculateTabSizesIfNeeded();
        }

        private void HandleTabBarContextClick()
        {
            if (Event.current.type != EventType.MouseDown || Event.current.button != 1 || _updateTabSize) return;

            foreach (var tabRect in _tabRects.Values)
            {
                if (!tabRect.rect.Contains(Event.current.mousePosition)) continue;

                var palette = tabRect.palette;
                var name = palette.name;
                var menu = new UnityEditor.GenericMenu();
                menu.AddItem(new GUIContent("Rename"), false, ShowRenamePaletteWindow,
                    new RenameData(palette, name, position.position + Event.current.mousePosition));
                menu.AddItem(new GUIContent("Delete"), false, ShowDeleteConfirmation, palette);
                menu.AddItem(new GUIContent("Duplicate"), false, DuplicatePalette, palette);
                menu.ShowAsContext();
                Event.current.Use();
                break;
            }
        }

        private void DrawPinnedTabsIfAvailable()
        {
            if (_updateTabSize || PaletteManager.pinnedCount <= 0) return;

            if (!TryGetRowItemCounts(PaletteManager.pinnedPalettes, out var rowItemCount)) return;
            if (rowItemCount.Count == 0) return;

            int fromIdx = 0;
            int toIdx = 0;

            foreach (var itemCount in rowItemCount)
            {
                toIdx = fromIdx + itemCount - 1;
                using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.toolbar))
                {
                    if (fromIdx == 0) DrawDropDownButton();
                    DrawTabs(fromIdx, toIdx, pinned: true);
                }
                fromIdx = toIdx + 1;
                if (fromIdx >= PaletteManager.pinnedCount) break;
            }
        }

        private void DrawNonPinnedTabsIfAvailable()
        {
            if (_updateTabSize || PaletteManager.nonPinnedCount <= 0) return;
            if (!TryGetRowItemCounts(PaletteManager.nonPinnedPalettes, out var rowItemCount)) return;
            if (rowItemCount.Count == 0) return;

            int fromIdx = 0;
            int toIdx = 0;

            if (PaletteManager.showTabsInMultipleRows)
            {
                foreach (var itemCount in rowItemCount)
                {
                    toIdx = fromIdx + itemCount - 1;
                    using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.toolbar))
                    {
                        if (fromIdx == 0 && PaletteManager.pinnedCount == 0) DrawDropDownButton();
                        DrawTabs(fromIdx, toIdx, pinned: false);
                    }
                    fromIdx = toIdx + 1;
                    if (fromIdx >= PaletteManager.nonPinnedCount) break;
                }
            }
            else
            {
                using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.toolbar))
                {
                    if (PaletteManager.pinnedCount == 0) DrawDropDownButton();
                    DrawTabs(fromIdx, to: rowItemCount[0] - 1, pinned: false);
                }
            }

            if (PaletteManager.pinnedCount == 0)
            {
                if (!PaletteManager.nonPinnedPalettes.Exists(p => p.id == PaletteManager.selectedPalette.id))
                    SelectPalette(PaletteManager.nonPinnedPalettes[0]);
            }
            else if (!PaletteManager.nonPinnedPalettes.Exists(p => p.id == PaletteManager.selectedPalette.id))
            {
                if (!PaletteManager.pinnedPalettes.Exists(p => p.id == PaletteManager.selectedPalette.id))
                    SelectPalette(PaletteManager.pinnedPalettes[0]);
            }
        }

        private bool TryGetRowItemCounts(System.Collections.Generic.IList<PaletteData> palettes,
            out System.Collections.Generic.List<int> rowItemCount)
        {
            rowItemCount = new System.Collections.Generic.List<int>();
            float tabsWidth = 0f;
            int tabItemCount = 0;
            for (int i = 0; i < palettes.Count; ++i)
            {
                var id = palettes[i].id;
                if (!_tabSize.ContainsKey(id))
                {
                    _updateTabSize = true;
                    rowItemCount.Clear();
                    return false;
                }

                var w = _tabSize[id];
                tabsWidth += w;

                if (tabsWidth > position.width)
                {
                    rowItemCount.Add(Mathf.Max(tabItemCount, 1));
                    tabsWidth = tabItemCount > 0 ? w : 0;
                    if (tabItemCount == 0) continue;
                    tabItemCount = 0;
                }
                ++tabItemCount;
            }
            if (tabItemCount > 0) rowItemCount.Add(tabItemCount);
            return !_updateTabSize;
        }

        private void DrawTabs(int from, int to, bool pinned)
        {
            var palettes = pinned ? PaletteManager.pinnedPalettes : PaletteManager.nonPinnedPalettes;
            for (int i = from; i <= to; ++i)
            {
                var palette = palettes[i];
                var isSelected = PaletteManager.IsPaletteSelected(palette);
                var name = palette.name;
                if (GUILayout.Toggle(isSelected, name, UnityEditor.EditorStyles.toolbarButton)
                    && Event.current.button == 0 && !isSelected)
                {
                    SelectPalette(palette);
                }
                var toggleRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.Repaint)
                    _tabRects[palette.id] = (palette, toggleRect);
                if (GUILayout.Button(pinned ? pinnedTabIcon : pinTabIcon, UnityEditor.EditorStyles.toolbarButton))
                {
                    PaletteManager.TogglePinnedPalette(palette);
                    UpdateTabBar();
                    RepaintWindow();
                }
            }
            GUILayout.FlexibleSpace();
        }

        private void DrawDropDownButton()
        {
            if (!GUILayout.Button(_dropdownIcon, UnityEditor.EditorStyles.toolbarButton)) return;

            var menu = new UnityEditor.GenericMenu();
            menu.AddItem(new GUIContent("New palette"), false, CreatePalette);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Show tabs in multiple rows"),
                PaletteManager.showTabsInMultipleRows, ToggleMultipleRows);
            menu.AddSeparator(string.Empty);

            var allPalettes = PaletteManager.allPalettes;
            var sortedPalettes = allPalettes.OrderBy(p => p.name).ToList();
            var repeatedNameCount = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var palette in sortedPalettes)
            {
                var name = palette.name;
                var isRepeated = repeatedNameCount.ContainsKey(name);
                var displayName = isRepeated ? name + "(" + repeatedNameCount[name] + ")" : name;
                var isSelected = PaletteManager.IsPaletteSelected(palette);
                menu.AddItem(new GUIContent(displayName), isSelected, SelectPalette, palette);
                if (isRepeated) repeatedNameCount[name] += 1;
                else repeatedNameCount.Add(name, 1);
            }
            menu.ShowAsContext();
        }

        private void RecalculateTabSizesIfNeeded()
        {
            if (!_updateTabSize || Event.current.type != EventType.Repaint) return;

            var allPalettes = PaletteManager.allPalettes;
            _tabSize.Clear();
            for (int i = 0; i < allPalettes.Count; ++i)
            {
                var palette = allPalettes[i];
                var name = palette.name;
                var content = UnityEditor.EditorGUIUtility.TrTempContent(name);
                var size = UnityEditor.EditorStyles.toolbarButton.CalcSize(content).x + 18f; // 18 for pin icon
                var id = palette.id;
                if (_tabSize.ContainsKey(id)) _tabSize[id] = size;
                else _tabSize.Add(id, size);
            }
            _updateTabSize = false;
            Repaint();
        }
        #endregion
    }
}
#pragma warning restore UDR0001
