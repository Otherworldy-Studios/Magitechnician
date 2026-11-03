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
        private int _selectedItemIdx = 0;
        private int _currentPickerId = -1;
        private bool _itemAdded = false;
        private MultibrushItemSettings _newItem = null;
        private int _newItemIdx = -1;

        [SerializeField]
        private System.Collections.Generic.List<int> _selection
            = new System.Collections.Generic.List<int>() { 0 };

        private (int from, int to, bool perform) _moveItem = (0, 0, false);
        private bool _draggingItem = false;
        private Rect _cursorRect = Rect.zero;
        private bool _showCursor = false;

        private bool draggingItem
        {
            get => _draggingItem;
            set
            {
                _draggingItem = value;
                wantsMouseMove = value;
                wantsMouseEnterLeaveWindow = value;
            }
        }

        private class ItemSelectionState : BrushSelectionState
        {
            public SelectionFieldState overwriteSettings = SelectionFieldState.SAME;
            public SelectionFieldState frequency = SelectionFieldState.SAME;
            public override bool changed => base.changed || embedInSurface == SelectionFieldState.CHANGED
                || frequency == SelectionFieldState.CHANGED;
            public override void Reset()
            {
                base.Reset();
                overwriteSettings = SelectionFieldState.SAME;
                frequency = SelectionFieldState.SAME;
            }
        }

        private ItemSelectionState _itemSelectionState = new ItemSelectionState();
        private MultibrushItemSettings _itemSelectionSettings = new MultibrushItemSettings();

        private void ItemSelectionFields(bool checkSelectionIndexes = true)
        {
            var selection = _selection.ToArray();
            var settingsArray = PaletteManager.selectedBrush.items;

            if (checkSelectionIndexes)
            {
                for (int i = 0; i < selection.Length - 1; ++i)
                {
                    var brushIdx = selection[i];
                    var nextBrushIdx = selection[i + 1];
                    if (brushIdx >= settingsArray.Length || nextBrushIdx >= settingsArray.Length)
                    {
                        _selection.Clear();
                        _selection.Add(0);
                        _selectedItemIdx = 0;
                        UpdateBrushSelectionSettings(_selection.ToArray(), settingsArray,
                            _itemSelectionState, _itemSelectionSettings);
                        ItemSelectionFields(false);
                        Repaint();
                        return;
                    }
                }
            }
            UpdateSelectionState(settingsArray, selection, _itemSelectionState);
            _itemSelectionState.overwriteSettings = SelectionFieldState.SAME;
            _itemSelectionState.frequency = SelectionFieldState.SAME;
            for (int i = 0; i < selection.Length - 1; ++i)
            {
                var brushIdx = selection[i];
                var nextBrushIdx = selection[i + 1];
                var brush = settingsArray[brushIdx];
                var nextBrush = settingsArray[nextBrushIdx];
                if (_itemSelectionState.overwriteSettings != SelectionFieldState.CHANGED
                   && brush.overwriteSettings != nextBrush.overwriteSettings)
                    _itemSelectionState.overwriteSettings = SelectionFieldState.MIXED;
                if (_itemSelectionState.frequency != SelectionFieldState.CHANGED
                   && brush.frequency != nextBrush.frequency)
                    _itemSelectionState.frequency = SelectionFieldState.MIXED;
            }

            BrushSelectionFields(ref _itemPosGroupOpen, ref _itemRotGroupOpen, ref _itemScaleGroupOpen,
                    ref _itemFlipGroupOpen, MULTIBRUSH_SETTINGS_UNDO_MSG, true, false, settingsArray, selection,
                    _itemSelectionSettings, _itemSelectionState);
        }
        private void BrushItems(ref BrushInputData toggleData)
        {
            var brush = PaletteManager.selectedBrush;
            var items = brush.items;
            for (int i = 0; i < items.Length; ++i)
            {
                var item = items[i];
                BrushItem(item, i, ref toggleData);
            }
            if (_showCursor) GUI.Box(_cursorRect, string.Empty, _cursorStyle);
        }
        private void SelectPrefabs(object idx)
        {
            var prefabs = new System.Collections.Generic.List<GameObject>();
            if (_selection.Contains((int)idx))
                foreach (int selectedIdx in _selection)
                {
                    var prefab = PaletteManager.selectedBrush.GetItemAt(selectedIdx).prefab;
                    if (prefab != null) prefabs.Add(prefab);
                }
            else
            {
                var prefab = PaletteManager.selectedBrush.GetItemAt((int)idx).prefab;
                if (prefab != null) prefabs.Add(prefab);
            }
            UnityEditor.Selection.objects = prefabs.ToArray();
        }
        private void OpenPrefab(object idx)
        {
            var prefab = PaletteManager.selectedBrush.GetItemAt((int)idx).prefab;
            if (prefab != null) UnityEditor.AssetDatabase.OpenAsset(prefab);
        }
        private void UpdateThumbnail(object idx)
        {
            var item = PaletteManager.selectedBrush.GetItemAt((int)idx);
            item.UpdateThumbnail(updateItemThumbnails: true, savePng: true);
        }
        private void EditThumbnail(object idx)
        {
            var itemIdx = (int)idx;
            var item = PaletteManager.selectedBrush.GetItemAt(itemIdx);
            ThumbnailEditorWindow.ShowWindow(item, itemIdx);
        }
        private void CopyThumbnailSettings(object idx)
        {
            var item = PaletteManager.selectedBrush.GetItemAt((int)idx);
            PaletteManager.clipboardThumbnailSettings = item.thumbnailSettings.Clone();
            PaletteManager.clipboardOverwriteThumbnailSettings = item.overwriteThumbnailSettings
                ? PaletteManager.Trit.TRUE : PaletteManager.Trit.FALSE;
        }
        private void PasteThumbnailSettings(object idx)
        {
            if (PaletteManager.clipboardThumbnailSettings == null) return;
            void Paste(MultibrushItemSettings item)
            {
                if (PaletteManager.clipboardOverwriteThumbnailSettings != PaletteManager.Trit.SAME)
                {
                    item.overwriteThumbnailSettings
                        = PaletteManager.clipboardOverwriteThumbnailSettings == PaletteManager.Trit.TRUE;
                }
                item.thumbnailSettings.Copy(PaletteManager.clipboardThumbnailSettings);
                ThumbnailUtils.UpdateThumbnail(item, savePng: true, updateParent: true);
            }
            if (_selection.Contains((int)idx))
            {
                foreach (var i in _selection) Paste(PaletteManager.selectedBrush.GetItemAt(i));
            }
            else Paste(PaletteManager.selectedBrush.GetItemAt((int)idx));
            PaletteManager.selectedPalette.Save();
        }
        private void DeleteItem(object obj)
        {
            var idx = (int)obj;
            if (_selection.Contains(idx))
            {
                var descendingSelection = _selection.ToArray();
                System.Array.Sort<int>(descendingSelection, new System.Comparison<int>((i1, i2) => i2.CompareTo(i1)));
                foreach (var i in descendingSelection) PaletteManager.selectedBrush.RemoveItemAt(i);
            }
            else PaletteManager.selectedBrush.RemoveItemAt(idx);
            _selectedItemIdx = Mathf.Clamp(_selectedItemIdx, 0, PaletteManager.selectedBrush.itemCount - 1);
            _selection.Clear();
            _selection.Add(_selectedItemIdx);
            OnMultiBrushChanged();
        }
        private void AddItemAt(object obj)
        {
            _newItemIdx = (int)obj;
            _openObjectPicker = true;
        }

        private bool _openObjectPicker = false;

        private void OpenObjectPickerIfRequested()
        {
            if (!_openObjectPicker) return;
            _openObjectPicker = false;
            _currentPickerId = GUIUtility.GetControlID(FocusType.Passive) + 100;
            UnityEditor.EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:Prefab", _currentPickerId);
        }
        private void CreateItemsFromEachPrefabInFolder(object obj)
        {
            _newItemIdx = (int)obj;
            var items = PluginMaster.DropUtils.GetFolderItems();
            if (items == null) return;
            for (int i = 0; i < items.Length; ++i)
            {
                var item = items[i];
                if (item.obj == null) continue;
                _newItem = new MultibrushItemSettings(item.obj, PaletteManager.selectedBrush);
                PaletteManager.selectedBrush.InsertItemAt(_newItem, _newItemIdx + 1 + i);
            }
            OnMultiBrushChanged();
        }
        public void CreateItemsFromEachPrefabSelected(object obj)
        {
            _newItemIdx = (int)obj;
            var selectionPrefabs = SelectionManager.GetSelectionPrefabs();
            if (selectionPrefabs.Length == 0) return;
            for (int i = 0; i < selectionPrefabs.Length; ++i)
            {
                var selectedObj = selectionPrefabs[i];
                if (selectedObj == null) continue;
                _newItem = new MultibrushItemSettings(selectedObj, PaletteManager.selectedBrush);
                PaletteManager.selectedBrush.InsertItemAt(_newItem, _newItemIdx + 1 + i);
            }
            OnMultiBrushChanged();
        }
        private void BrushItem(MultibrushItemSettings item, int index, ref BrushInputData data)
        {
            var style = new GUIStyle(_itemStyle);
            var selection = _selection.ToArray();
            if (PaletteManager.selectedBrush == null) return;
            var settingsArray = PaletteManager.selectedBrush.items;

            for (int i = 0; i < selection.Length; ++i)
            {
                if (selection[i] >= settingsArray.Length)
                {
                    _selection.Clear();
                    _selection.Add(0);
                    _selectedItemIdx = 0;
                    UpdateBrushSelectionSettings(_selection.ToArray(), settingsArray,
                        _itemSelectionState, _itemSelectionSettings);
                    break;
                }
            }

            if (_selection.Contains(index)) style.normal = _itemStyle.onNormal;
            using (new GUILayout.VerticalScope(style))
            {
                var nameStyle = GUIStyle.none;
                nameStyle.margin = new RectOffset(2, 2, 0, 1);
                nameStyle.clipping = TextClipping.Clip;
                nameStyle.fontSize = 8;
                if (item.prefab == null) return;
                GUILayout.Box(new GUIContent((index + 1).ToString() + ". " + item.prefab.name, item.prefab.name),
                    nameStyle, GUILayout.Width(56));
                GUILayout.Box(new GUIContent(item.thumbnail, item.prefab.name), GUIStyle.none,
                    GUILayout.Width(64), GUILayout.Height(64));
            }

            var rect = GUILayoutUtility.GetLastRect();
            var toggleRect = new Rect(rect.xMax - 16, rect.yMax - 16, 14, 14);
            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
            {
                var include = GUI.Toggle(toggleRect, item.includeInThumbnail, GUIContent.none, _thumbnailToggleStyle);
                if (check.changed)
                {
                    item.includeInThumbnail = include;
                    PaletteManager.selectedPalette.Save();
                    ThumbnailUtils.UpdateThumbnail(item.parentSettings, updateItemThumbnails: false, savePng: true);
                }
            }
            if (rect.Contains(Event.current.mousePosition))
                data = new BrushInputData(index, rect, Event.current.type, Event.current.control,
                    Event.current.shift, Event.current.mousePosition.x);
        }
        private void CopyItemSettings(object idx)
            => PaletteManager.clipboardSetting = PaletteManager.selectedBrush.items[(int)idx].Clone();
        private void PasteItemSettings(object idx)
        {
            PaletteManager.selectedBrush.items[(int)idx].Copy(PaletteManager.clipboardSetting);
            PaletteManager.selectedPalette.Save();
        }
        private void DuplicateItem(object obj)
        {
            var idx = (int)obj;
            if (_selection.Contains(idx))
            {
                var descendingSelection = _selection.ToArray();
                System.Array.Sort<int>(descendingSelection, new System.Comparison<int>((i1, i2) => i2.CompareTo(i1)));
                for (int i = 0; i < descendingSelection.Length; ++i)
                {
                    PaletteManager.selectedBrush.Duplicate(descendingSelection[i]);
                    descendingSelection[i] += descendingSelection.Length - 1 - i;
                }
                _selection.Clear();
                _selection.AddRange(descendingSelection);
            }
            else PaletteManager.selectedBrush.Duplicate(idx);
            OnMultiBrushChanged();
            BrushstrokeManager.UpdateBrushstroke();
        }
        private void ItemMouseEventHandler(BrushInputData data)
        {
            if (data == null) return;
            if (data.eventType == EventType.MouseUp && Event.current.button == 0)
            {
                void SelectionChanged()
                {
                    var selection = _selection.ToArray();
                    var settingsArray = PaletteManager.selectedBrush.items;
                    UpdateBrushSelectionSettings(_selection.ToArray(), settingsArray,
                            _itemSelectionState, _itemSelectionSettings);
                    _itemSelectionState.overwriteSettings = SelectionFieldState.SAME;

                    for (int i = 0; i < selection.Length - 1; ++i)
                    {
                        var brushIdx = selection[i];
                        var nextBrushIdx = selection[i + 1];
                        var brush = settingsArray[brushIdx];
                        var nextBrush = settingsArray[nextBrushIdx];
                        if (brush.overwriteSettings != nextBrush.overwriteSettings)
                        {
                            _itemSelectionState.overwriteSettings = SelectionFieldState.MIXED;
                            _itemSelectionSettings.overwriteSettings = true;
                        }
                    }
                    if (_itemSelectionState.overwriteSettings == SelectionFieldState.SAME)
                        _itemSelectionSettings.overwriteSettings = settingsArray[selection[0]].overwriteSettings;
                    _itemSelectionSettings.frequency = settingsArray[selection[0]].frequency;
                }
                void DeselectAllButCurrent()
                {
                    _selection.Clear();
                    _selection.Add(data.index);
                    _selectedItemIdx = data.index;
                    SelectionChanged();
                }
                void ToggleCurrent()
                {
                    if (_selection.Contains(data.index))
                    {
                        if (_selection.Count <= 1) return;
                        _selectedItemIdx = Mathf.Clamp(_selection.IndexOf(data.index), 0,
                            PaletteManager.selectedBrush.itemCount - 2);
                        _selection.Remove(data.index);
                    }
                    else
                    {
                        _selection.Add(data.index);
                        _selectedItemIdx = data.index;
                    }
                    SelectionChanged();
                }
                if (data.shift)
                {
                    var sign = (int)Mathf.Sign(data.index - _selectedItemIdx);
                    if (sign != 0)
                    {
                        _selection.Clear();
                        for (int i = _selectedItemIdx; i != data.index; i += sign) _selection.Add(i);
                        _selection.Add(data.index);
                        SelectionChanged();
                    }
                    else DeselectAllButCurrent();
                }
                else if (data.control) ToggleCurrent();
                else DeselectAllButCurrent();

                Repaint();
                Event.current.Use();
            }
            else if (data.eventType == EventType.ContextClick)
            {
                var menu = new UnityEditor.GenericMenu();
                menu.AddItem(new GUIContent("Select Prefab" + (_selection.Count > 1 ? "s" : "")),
                    false, SelectPrefabs, data.index);
                if (_selection.Count == 1)
                    menu.AddItem(new GUIContent("Open Prefab"), false, OpenPrefab, data.index);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Update Thumbnail"), false, UpdateThumbnail, data.index);
                menu.AddItem(new GUIContent("Edit Thumbnail"), false, EditThumbnail, data.index);
                menu.AddItem(new GUIContent("Copy Thumbnail Settings"), false, CopyThumbnailSettings, data.index);
                if (PaletteManager.clipboardThumbnailSettings != null)
                    menu.AddItem(new GUIContent("Paste Thumbnail Settings"), false, PasteThumbnailSettings, data.index);
                menu.AddSeparator(string.Empty);
                if (PaletteManager.selectedBrush.items.Length > 1
                    && _selection.Count < PaletteManager.selectedBrush.items.Length)
                    menu.AddItem(new GUIContent("Delete"), false, DeleteItem, data.index);
                menu.AddItem(new GUIContent("Duplicate"), false, DuplicateItem, data.index);
                if (_selection.Count == 1)
                    menu.AddItem(new GUIContent("Copy Brush Settings"), false, CopyItemSettings, data.index);
                if (PaletteManager.clipboardSetting != null)
                    menu.AddItem(new GUIContent("Paste Brush Settings"), false, PasteItemSettings, data.index);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("New Item..."), false, AddItemAt, data.index);
                menu.AddItem(new GUIContent("New Items From Folder..."),
                    false, CreateItemsFromEachPrefabInFolder, data.index);
                menu.AddItem(new GUIContent("New Items From Selection"),
                    false, CreateItemsFromEachPrefabSelected, data.index);
                menu.ShowAsContext();
                Event.current.Use();
            }
            else if (data.eventType == EventType.MouseDrag)
            {
                UnityEditor.DragAndDrop.PrepareStartDrag();
                UnityEditor.DragAndDrop.StartDrag("Dragging brush");
                UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Copy;
                draggingItem = true;
                _moveItem.from = data.index;
                _moveItem.perform = false;
                _moveItem.to = -1;
                Event.current.Use();
            }
            else if (data.eventType == EventType.DragUpdated)
            {
                UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Copy;
                var size = new Vector2(4, data.rect.height);
                var min = data.rect.min;
                var toTheRight = data.mouseX - data.rect.center.x > 0;
                min.x = toTheRight ? data.rect.max.x : min.x - size.x;
                _cursorRect = new Rect(min, size);
                _showCursor = true;
                _moveItem.to = data.index;
                if (toTheRight) ++_moveItem.to;
                Event.current.Use();
            }
            else if (data.eventType == EventType.DragPerform)
            {
                var toTheRight = data.mouseX - data.rect.center.x > 0;
                _moveItem.to = data.index;
                if (toTheRight) ++_moveItem.to;
                if (draggingItem)
                {
                    _moveItem.perform = _moveItem.from != _moveItem.to;
                    draggingItem = false;
                }
                _showCursor = false;
                Event.current.Use();
            }
            else if (data.eventType == EventType.DragExited)
            {
                _showCursor = false;
                draggingItem = false;
                _moveItem.to = -1;
            }
        }
        private void OnObjectSelectorClosed()
        {
            if (Event.current.commandName == "ObjectSelectorClosed"
                && UnityEditor.EditorGUIUtility.GetObjectPickerControlID() == _currentPickerId)
            {
                var obj = UnityEditor.EditorGUIUtility.GetObjectPickerObject();
                if (obj != null)
                {
                    var prefabType = UnityEditor.PrefabUtility.GetPrefabAssetType(obj);
                    if (prefabType == UnityEditor.PrefabAssetType.Regular
                        || prefabType == UnityEditor.PrefabAssetType.Variant)
                    {
                        _itemAdded = true;
                        _newItem = new MultibrushItemSettings(obj as GameObject, PaletteManager.selectedBrush);
                    }
                }
                _currentPickerId = -1;
            }
        }
        private void OnMultiBrushChanged()
        {
            if (PrefabPalette.instance != null) PrefabPalette.instance.OnPaletteChange();
        }
        private void ItemSettingsGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                var item = GetSelectedItem(PaletteManager.selectedBrush);
                if (_selection.Count <= 1)
                    BrushFields(item, ref _itemPosGroupOpen, ref _itemRotGroupOpen,
                        ref _itemScaleGroupOpen, ref _itemFlipGroupOpen);
                else ItemSelectionFields();
            }
        }
        private MultibrushItemSettings GetSelectedItem(MultibrushSettings brush)
        {
            if (brush == null) return null;
            var item = brush.GetItemAt(_selectedItemIdx);
            if (item == null)
            {
                _selectedItemIdx = 0;
                item = brush.GetItemAt(_selectedItemIdx);
            }
            return item;
        }
    }
}
#pragma warning restore UDR0001
