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
using UnityEngine;

namespace PluginMaster
{
    public partial class BrushProperties : UnityEditor.EditorWindow, ISerializationCallbackReceiver
    {
        private const string MULTIBRUSH_SETTINGS_UNDO_MSG = "Multibrush Settings";
        private bool _multiBrushGroupOpen = false;
        private Vector2 _multiBrushScrollPosition = Vector2.zero;
        private bool _multiBrushClipped = false;

        private bool _itemPosGroupOpen = false;
        private bool _itemRotGroupOpen = false;
        private bool _itemScaleGroupOpen = false;
        private bool _itemFlipGroupOpen = false;
        private bool _frequencyGroupOpen = false;

        private void MultiBrushGroup(ref BrushInputData toggleData)
        {
            if (Event.current.control && Event.current.keyCode == KeyCode.A)
            {
                _selection.Clear();
                for (int i = 0; i < PaletteManager.selectedBrush.itemCount; ++i) _selection.Add(i);
                Repaint();
            }

            if (_moveItem.perform)
            {
                var selection = _selection.ToArray();
                PaletteManager.selectedBrush.Swap(_moveItem.from, _moveItem.to, ref selection);
                _selection = new System.Collections.Generic.List<int>(selection);
                if (selection.Length == 1) _selectedItemIdx = selection[0];
                _moveItem.perform = false;
            }

            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                var brushesRect = new Rect();
                var selectedBrush = PaletteManager.selectedBrush;
                using (var scrollView = new UnityEditor.EditorGUILayout.ScrollViewScope(
                    _multiBrushScrollPosition, false, false,
                    GUI.skin.horizontalScrollbar, GUIStyle.none, _skin.box,
                    GUILayout.Height(_multiBrushClipped ? 102 : 87)))
                {
                    _multiBrushScrollPosition = scrollView.scrollPosition;
                    using (new GUILayout.HorizontalScope())
                    {
                        BrushItems(ref toggleData);
                        GUILayout.FlexibleSpace();
                    }
                    brushesRect = GUILayoutUtility.GetLastRect();
                }
                var scrollViewRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.Repaint)
                {
                    var prev = _multiBrushClipped;
                    _multiBrushClipped = (scrollViewRect.width - 8) < brushesRect.width;
                    if (prev != _multiBrushClipped) Repaint();
                }
                if (scrollViewRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.ContextClick)
                    {
                        var menu = new UnityEditor.GenericMenu();
                        menu.AddItem(new GUIContent("New Item..."), false, AddItemAt,
                            selectedBrush.items.Length);
                        menu.AddItem(new GUIContent("New Items From Folder..."), false,
                            CreateItemsFromEachPrefabInFolder, selectedBrush.items.Length - 1);
                        menu.AddItem(new GUIContent("New Items From Selection"), false,
                            CreateItemsFromEachPrefabSelected, selectedBrush.items.Length - 1);
                        menu.ShowAsContext();
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.DragUpdated)
                    {
                        UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.DragPerform)
                    {
                        bool multiBrushChanged = false;
                        var droppedItems = PluginMaster.DropUtils.GetDroppedPrefabs();
                        foreach (var droppedItem in droppedItems)
                        {
                            var item = new MultibrushItemSettings(droppedItem.obj, selectedBrush);
                            if (_moveItem.to == -1)
                            {
                                selectedBrush.AddItem(item);
                                _selectedItemIdx = selectedBrush.items.Length - 1;
                            }
                            else
                            {
                                selectedBrush.InsertItemAt(item, _moveItem.to);
                                _selectedItemIdx = _moveItem.to;
                            }
                            multiBrushChanged = true;
                        }
                        if (multiBrushChanged) OnMultiBrushChanged();
                        Event.current.Use();
                    }
                }

                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    if (selectedBrush == null) return;
                    var selectedItem = GetSelectedItem(selectedBrush);
                    if (selectedItem.prefab == null) return;
                    var itemName = selectedItem.prefab.name;
                    var itemNameStyle = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
                    itemNameStyle.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label((_selectedItemIdx + 1) + ". " + itemName, itemNameStyle);
                    var separatorStyle = new GUIStyle(UnityEditor.EditorStyles.toolbarButton);
                    separatorStyle.fixedHeight = 1;
                    GUILayout.Box(GUIContent.none, separatorStyle);
                    _frequencyGroupOpen = UnityEditor.EditorGUILayout.Foldout(_frequencyGroupOpen, "Frequency");
                    if (_frequencyGroupOpen) FrequencyGroup();
                    UnityEditor.EditorGUIUtility.labelWidth = 150;
                    if (_selection.Count <= 1)
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            bool overwriteSettings = UnityEditor.EditorGUILayout.ToggleLeft("Overwrite Brush Settings",
                                selectedItem.overwriteSettings);
                            if (check.changed)
                            {
                                selectedItem.overwriteSettings = overwriteSettings;
                                if (selectedItem.overwriteSettings) selectedItem.Copy(selectedBrush);
                            }
                        }
                    }
                    else
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Box(GetStateGUIContent(_itemSelectionState.overwriteSettings),
                                UnityEditor.EditorStyles.label);
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                _itemSelectionSettings.overwriteSettings
                                    = UnityEditor.EditorGUILayout.ToggleLeft("Overwrite Brush Settings",
                                    _itemSelectionSettings.overwriteSettings);
                                if (check.changed) _itemSelectionState.overwriteSettings = SelectionFieldState.CHANGED;
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                    if ((_selection.Count > 1 && (_itemSelectionState.overwriteSettings == SelectionFieldState.MIXED
                        || (_itemSelectionState.overwriteSettings != SelectionFieldState.MIXED
                        && _itemSelectionSettings.overwriteSettings)))
                        || (_selection.Count <= 1 && selectedItem.overwriteSettings)) ItemSettingsGroup();
                    if (_selection.Count > 1)
                    {
                        var selection = _selection.ToArray();
                        var settingsArray = selectedBrush.items;
                        var apply = ApplyDiscardButtons(MULTIBRUSH_SETTINGS_UNDO_MSG, true, settingsArray, selection,
                            _itemSelectionSettings, _itemSelectionState);
                        if (apply)
                        {
                            foreach (var idx in selection)
                            {
                                var brush = selectedBrush.GetItemAt(idx);
                                brush.overwriteSettings = _itemSelectionSettings.overwriteSettings;
                                brush.frequency = _itemSelectionSettings.frequency;
                            }
                            if (_itemSelectionState.overwriteSettings == SelectionFieldState.CHANGED)
                                _itemSelectionState.overwriteSettings = SelectionFieldState.SAME;
                            if (_itemSelectionState.frequency == SelectionFieldState.CHANGED)
                                _itemSelectionState.frequency = SelectionFieldState.SAME;
                        }
                    }
                }
            }
        }
    }
}
#pragma warning restore UDR0001
