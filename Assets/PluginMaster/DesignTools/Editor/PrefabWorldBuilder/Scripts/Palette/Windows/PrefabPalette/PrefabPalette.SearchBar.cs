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
        private string _filterText = string.Empty;
        private GUIContent _labelIcon = null;
        private GUIContent _selectionFilterIcon = null;
        private GUIContent _folderFilterIcon = null;
        private GUIContent _clearFilterIcon = null;

        private struct FilteredBrush
        {
            public readonly MultibrushSettings brush;
            public readonly int index;
            public FilteredBrush(MultibrushSettings brush, int index) => (this.brush, this.index) = (brush, index);
        }
        private System.Collections.Generic.List<FilteredBrush> _filteredBrushList
            = new System.Collections.Generic.List<FilteredBrush>();
        private System.Collections.Generic.List<FilteredBrush> filteredBrushList
        {
            get
            {
                if (_filteredBrushList == null) _filteredBrushList = new System.Collections.Generic.List<FilteredBrush>();
                return _filteredBrushList;
            }
        }
        public bool FilteredBrushListContains(int index) => _filteredBrushList.Exists(brush => brush.index == index);
        private System.Collections.Generic.Dictionary<string, bool> _labelFilter
            = new System.Collections.Generic.Dictionary<string, bool>();
        public System.Collections.Generic.Dictionary<string, bool> labelFilter
        {
            get
            {
                if (_labelFilter == null) _labelFilter = new System.Collections.Generic.Dictionary<string, bool>();
                return _labelFilter;
            }
            set => _labelFilter = value;
        }

        private bool _updateLabelFilter = true;
        public int filteredBrushListCount => filteredBrushList.Count;

        public string filterText
        {
            get
            {
                if (_filterText == null) _filterText = string.Empty;
                return _filterText;
            }
            set => _filterText = value;
        }

        private System.Collections.Generic.Dictionary<long, string[]> _hiddenFolders
            = new System.Collections.Generic.Dictionary<long, string[]>();

        private string[] hiddenFolders
        {
            get
            {
                if (_hiddenFolders.Count == 0 || !_hiddenFolders.ContainsKey(PaletteManager.selectedPalette.id))
                    return new string[] { };
                return _hiddenFolders[PaletteManager.selectedPalette.id];
            }
        }
        public static string[] GetHiddenFolders()
        {
            if (instance == null) return new string[] { };
            return instance.hiddenFolders;
        }

        public static void SetHiddenFolders(string[] value)
        {
            if (instance == null) return;
            if (instance._hiddenFolders.ContainsKey(PaletteManager.selectedPalette.id))
                instance._hiddenFolders[PaletteManager.selectedPalette.id] = value;
            else instance._hiddenFolders.Add(PaletteManager.selectedPalette.id, value);
            instance.UpdateFilteredList(false);
            RepaintWindow();
        }
        private void ClearLabelFilter()
        {
            foreach (var key in labelFilter.Keys.ToArray()) labelFilter[key] = false;
        }

        private void SearchBar()
        {
            if (_clearFilterIcon == null) 
                _clearFilterIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Clear"));
            if (_labelIcon == null) 
                _labelIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Label"), "Filter by label");
            if (_selectionFilterIcon == null) 
                _selectionFilterIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/SelectionFilter"), "Filter by selection");
            if (_folderFilterIcon == null) 
                _folderFilterIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/FolderFilter"), "Filter by folder");

            if (_labelIcon == null || _selectionFilterIcon == null 
                || _folderFilterIcon == null || _clearFilterIcon == null)
            {
                GUIUtility.ExitGUI();
                return;
            }

            using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();

                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
#if UNITY_2019_1_OR_NEWER
                    var searchFieldStyle = UnityEditor.EditorStyles.toolbarSearchField;
#else
                    var searchFieldStyle = EditorStyles.toolbarTextField;
#endif
                    GUILayout.Space(2);
                    filterText = UnityEditor.EditorGUILayout.TextField(filterText, searchFieldStyle).Trim();
                    if (check.changed) UpdateFilteredList(true);
                }
                
                using (new UnityEditor.EditorGUI.DisabledGroupScope(filterText == string.Empty))
                {
                    if (GUILayout.Button(_clearFilterIcon, UnityEditor.EditorStyles.toolbarButton) 
                        && filterText != string.Empty)
                    {
                        filterText = string.Empty;
                        ClearLabelFilter();
                        UpdateFilteredList(true);
                        GUI.FocusControl(null);
                    }
                }

                if (GUILayout.Button(_labelIcon, UnityEditor.EditorStyles.toolbarButton))
                {
                    GUI.FocusControl(null);
                    UpdateLabelFilter();
                    var menu = new UnityEditor.GenericMenu();
                    if (labelFilter.Count == 0)
                        menu.AddItem(new GUIContent("No labels Found"), false, null);
                    else
                        foreach (var labelItem in labelFilter.OrderBy(item => item.Key))
                            menu.AddItem(new GUIContent(labelItem.Key), labelItem.Value,
                                SelectLabelFilter, labelItem.Key);
                    menu.ShowAsContext();
                }

                if (GUILayout.Button(_selectionFilterIcon, UnityEditor.EditorStyles.toolbarButton))
                {
                    GUI.FocusControl(null);
                    FilterBySelection();
                }
                if (GUILayout.Button(_folderFilterIcon, UnityEditor.EditorStyles.toolbarButton))
                {
                    FilterByFolderWindow.ShowWindow();
                }
            }
            if (_updateLabelFilter)
            {
                _updateLabelFilter = false;
                UpdateLabelFilter();
            }
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                GUI.FocusControl(null);
                Repaint();
            }
        }

        private bool FilteredListContains(int index)
        {
            foreach (var filtered in filteredBrushList)
            {
                if (filtered.index == index) return true;
            }
            return false;
        }

        private void UpdateFilteredList(bool textCanged)
        {
            filteredBrushList.Clear();
            var selectedPalette = PaletteManager.selectedPalette;
            if (selectedPalette == null) return;

            void RemoveFromSelection(int index)
            {
                PaletteManager.RemoveFromSelection(index);
                if (PaletteManager.selectedBrushIdx == index) PaletteManager.selectedBrushIdx = -1;
                if (PaletteManager.selectionCount == 1)
                    PaletteManager.selectedBrushIdx = PaletteManager.idxSelection[0];
            }

            //filter by label
            var filterTextArray = filterText.Split(',');
            var filterTextSet = new System.Collections.Generic.List<string>();
            ClearLabelFilter();
            bool filterByLabel = false;
            for (int i = 0; i < filterTextArray.Length; ++i)
            {
                var filterText = filterTextArray[i].Trim();
                if (filterText.Length >= 2 && filterText.Substring(0, 2) == "l:")
                {
                    filterText = filterText.Substring(2);
                    if (labelFilter.ContainsKey(filterText))
                    {
                        labelFilter[filterText] = true;
                        filterByLabel = true;
                    }
                    else return;
                    continue;
                }
                filterTextSet.Add(filterText);
            }

            var tempFilteredBrushList = new System.Collections.Generic.HashSet<FilteredBrush>();
            var brushes = PaletteManager.selectedPalette.brushes;
            if (!filterByLabel)
                for (int i = 0; i < brushes.Length; ++i)
                {
                    if (brushes[i].containMissingPrefabs) continue;
                    tempFilteredBrushList.Add(new FilteredBrush(brushes[i], i));
                }
            else
            {
                for (int i = 0; i < brushes.Length; ++i)
                {
                    var brush = brushes[i];
                    if (brush.containMissingPrefabs) continue;
                    bool itemContainsFilter = false;
                    foreach (var item in brush.items)
                    {
                        if (item.prefab == null) continue;
                        var labels = UnityEditor.AssetDatabase.GetLabels(item.prefab);
                        foreach (var label in labels)
                        {
                            if (labelFilter[label])
                            {
                                itemContainsFilter = true;
                                break;
                            }
                        }
                        if (itemContainsFilter) break;
                    }
                    if (itemContainsFilter) tempFilteredBrushList.Add(new FilteredBrush(brush, i));
                    else RemoveFromSelection(i);
                }
            }
            if (tempFilteredBrushList.Count == 0) return;
            //filter by name
            var listIsEmpty = filterTextSet.Count == 0;
            if (!listIsEmpty)
            {
                listIsEmpty = true;
                foreach (var filter in filterTextSet)
                {
                    if (filter != string.Empty)
                    {
                        listIsEmpty = false;
                        break;
                    }
                }
            }

            if (!listIsEmpty)
            {
                foreach (var filteredItem in tempFilteredBrushList.ToArray())
                {
                    for (int i = 0; i < filterTextSet.Count; ++i)
                    {
                        var filterText = filterTextSet[i].Trim();
                        bool wholeWordOnly = false;
                        if (filterText == string.Empty) continue;
                        if (filterText.Length >= 2 && filterText.Substring(0, 2) == "w:")
                        {
                            wholeWordOnly = true;
                            filterText = filterText.Substring(2);
                        }
                        if (filterText == string.Empty) continue;
                        filterText = filterText.ToLower();
                        var brush = filteredItem.brush;
                        if ((!wholeWordOnly && brush.name.ToLower().Contains(filterText))
                            || (wholeWordOnly && brush.name.ToLower() == filterText))
                            tempFilteredBrushList.Add(filteredItem);
                        else
                        {
                            if (tempFilteredBrushList.Contains(filteredItem)) tempFilteredBrushList.Remove(filteredItem);
                            RemoveFromSelection(filteredItem.index);
                        }
                    }
                }
            }
            if (tempFilteredBrushList.Count == 0) return;
            // Filter by folder
            foreach (var filteredItem in tempFilteredBrushList.ToArray())
            {
                var brushItems = filteredItem.brush.items;
                foreach (var brushItem in brushItems)
                {
                    if (filteredBrushList.Contains(filteredItem)) continue;
                    if (hiddenFolders.Any(filter => brushItem.prefabPath.StartsWith(filter)))
                        RemoveFromSelection(filteredItem.index);
                    else filteredBrushList.Add(filteredItem);
                }
            }
        }

        private void UpdateLabelFilter()
        {
            var selectedPalette = PaletteManager.selectedPalette;
            if (selectedPalette == null) return;
            foreach (var brush in selectedPalette.brushes)
            {
                foreach (var item in brush.items)
                {
                    if (item.prefab == null) continue;
                    var labels = UnityEditor.AssetDatabase.GetLabels(item.prefab);
                    foreach (var label in labels)
                    {
                        if (labelFilter.ContainsKey(label)) continue;
                        labelFilter.Add(label, false);
                    }
                }
            }
        }

        private void SelectLabelFilter(object key)
        {
            labelFilter[(string)key] = !labelFilter[(string)key];
            foreach (var pair in labelFilter)
            {
                if (!pair.Value) continue;
                var labelFilter = "l:" + pair.Key;
                if (filterText.Contains(labelFilter)) continue;
                if (filterText.Length > 0) filterText += ", ";
                filterText += labelFilter;
            }
            var filterTextArray = filterText.Split(',');
            filterText = string.Empty;
            for (int i = 0; i < filterTextArray.Length; ++i)
            {
                var filter = filterTextArray[i].Trim();
                if (filter.Length >= 2 && filter.Substring(0, 2) == "l:")
                {
                    var label = filter.Substring(2);
                    if (!labelFilter.ContainsKey(label)) continue;
                    if (!labelFilter[label]) continue;
                    if (filterText.Contains(filter)) continue;
                }
                if (filter == string.Empty) continue;
                filterText += filter + ", ";
            }
            if (filterText != string.Empty) filterText = filterText.Substring(0, filterText.Length - 2);
            UpdateFilteredList(false);
            Repaint();
        }

        public int FilterBySelection()
        {
            var selection = SelectionManager.GetSelectionPrefabs();
            filterText = string.Empty;
            for (int i = 0; i < selection.Length; ++i)
            {
                filterText += "w:" + selection[i].name;
                if (i < selection.Length - 1) filterText += ", ";
            }
            UpdateFilteredList(false);
            return filteredBrushListCount;
        }

        public void SelectFirstBrush()
        {
            if (filteredBrushListCount == 0) return;
            DeselectAllButThis(filteredBrushList[0].index);
        }
    }
}
#pragma warning restore UDR0001
