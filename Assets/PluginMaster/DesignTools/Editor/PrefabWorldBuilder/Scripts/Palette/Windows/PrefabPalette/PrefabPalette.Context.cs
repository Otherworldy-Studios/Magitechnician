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
        private int _currentPickerId = -1;
        private bool _contextBrushAdded = false;
        private MultibrushSettings _newContextBrush = null;

        private void PaletteContext()
        {
            if (_scrollViewRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.ContextClick)
                {
                    PaletteContextMenu();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    PaletteManager.ClearSelection();
                    Repaint();
                }
            }

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
                        _contextBrushAdded = true;
                        var gameObj = obj as GameObject;
                        AddLabels(gameObj);
                        _newContextBrush = MultibrushSettings.Create(gameObj, PaletteManager.selectedPalette);
                    }
                }
                _currentPickerId = -1;
            }
        }

        private void PaletteContextAddMenuItems(UnityEditor.GenericMenu menu)
        {
            menu.AddItem(new GUIContent("New Brush From Prefab..."), false, CreateBrushFromPrefab);
            menu.AddItem(new GUIContent("New MultiBrush From Folder..."), false, CreateBrushFromFolder);
            menu.AddItem(new GUIContent("New Brush From Each Prefab In Folder..."), false,
                CreateBrushFromEachPrefabInFolder);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("New MultiBrush From Selection"), false, CreateBrushFromSelection);
            menu.AddItem(new GUIContent("New Brush From Each Prefab Selected"), false,
                CreateBushFromEachPrefabSelected);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Update all thumbnails"), false, UpdateAllThumbnails);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Brush Creation And Drop Settings..."), false,
                BrushCreationSettingsWindow.ShowWindow);
            if (PaletteManager.selectedBrushIdx > 0 || PaletteManager.movingBrushes)
            {
                menu.AddSeparator(string.Empty);
                if (PaletteManager.selectedBrushIdx > 0)
                    menu.AddItem(new GUIContent("Copy Selected brushes"), false, PaletteManager.SelectBrushesToMove);
                if (PaletteManager.movingBrushes)
                {
                    menu.AddItem(new GUIContent("Paste brushes and keep originals"),
                        false, PasteBrushesToSelectedPalette);
                    menu.AddItem(new GUIContent("Paste brushes and delete originals"),
                        false, MoveBrushesToSelectedPalette);
                }
            }
        }

        private void PasteBrushesToSelectedPalette()
        {
            PaletteManager.PasteBrushesToSelectedPalette();
            OnPaletteChange();
        }
        private void MoveBrushesToSelectedPalette()
        {
            PaletteManager.MoveBrushesToSelectedPalette();
            OnPaletteChange();
        }
        private void PaletteContextMenu()
        {
            var menu = new UnityEditor.GenericMenu();
            PaletteContextAddMenuItems(menu);
            menu.ShowAsContext();
        }

        private void CreateBrushFromPrefab()
        {
            _currentPickerId = GUIUtility.GetControlID(FocusType.Passive) + 100;
            UnityEditor.EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:Prefab", _currentPickerId);
        }

        private void CreateBrushFromFolder()
        {
            var items = PluginMaster.DropUtils.GetFolderItems();
            if (items == null) return;
            RegisterUndo("Add Brush");
            var brush = MultibrushSettings.Create(items[0].obj, PaletteManager.selectedPalette);
            if(brush == null) return;
            AddLabels(items[0].obj);
            PaletteManager.selectedPalette.AddBrush(brush);
            DeselectAllButThis(PaletteManager.selectedPalette.brushes.Length - 1);
            for (int i = 1; i < items.Length; ++i)
            {
                var item = new MultibrushItemSettings(items[i].obj, brush);
                AddLabels(items[i].obj);
                brush.AddItem(item);
            }
            OnPaletteChange();
        }

        private void CreateBrushFromEachPrefabInFolder()
        {
            var items = PluginMaster.DropUtils.GetFolderItems();
            if (items == null) return;
            foreach (var item in items)
            {
                if (item.obj == null) continue;
                RegisterUndo("Add Brush");
                AddLabels(item.obj);
                var brush = MultibrushSettings.Create(item.obj, PaletteManager.selectedPalette);
                if (brush == null) continue;
                PaletteManager.selectedPalette.AddBrush(brush);
            }
            DeselectAllButThis(PaletteManager.selectedPalette.brushes.Length - 1);
            OnPaletteChange();
        }

        private string GetPrefabFolder(GameObject obj)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            var folders = path.Split(new char[] { '\\', '/' });
            var subFolder = folders[folders.Length - 2];
            return subFolder;
        }

        public void CreateBrushFromSelection()
        {
            if (PaletteManager.selectionCount > 1)
            {
                MergeBrushes();
                return;
            }

            var selectionPrefabs = SelectionManager.GetSelectionPrefabs();
            CreateBrushFromSelection(selectionPrefabs);
        }

        private void MergeBrushes()
        {
            RegisterUndo("Merge Brushes");
            var selection = new System.Collections.Generic.List<int>(PaletteManager.idxSelection);
            selection.Sort();
            var resultIdx = selection[0];
            var lastIdx = selection.Last() + 1;
            PaletteManager.selectedPalette.DuplicateBrushAt(resultIdx, lastIdx, out MultibrushSettings duplicate);
            if (duplicate == null)
            {
                PaletteManager.selectedPalette.Cleanup();
                return;
            }
            resultIdx = lastIdx;
            var firstItem = duplicate.GetItemAt(0);
            if (!firstItem.overwriteSettings) firstItem.Copy(duplicate);
            firstItem.overwriteSettings = true;
            duplicate.name += "_merged";

            selection.RemoveAt(0);
            bool cleanupPalette = false;
            for (int i = 0; i < selection.Count; ++i)
            {
                var idx = selection[i];
                var other = PaletteManager.selectedPalette.GetBrush(idx);
                if (other == null)
                {
                    cleanupPalette = true;
                    continue;
                }
                var otherItems = other.items;
                foreach (var item in otherItems)
                {
                    if (item == null)
                    {
                        cleanupPalette = true;
                        continue;
                    }
                    var clone = new MultibrushItemSettings(item.prefab, duplicate);
                    if (item.overwriteSettings) clone.Copy(item);
                    else clone.Copy(other);
                    clone.overwriteSettings = true;
                    duplicate.AddItem(clone);
                }
            }
            if (cleanupPalette) PaletteManager.selectedPalette.Cleanup();
            duplicate.Reset();
            PaletteManager.ClearSelection();
            PaletteManager.AddToSelection(resultIdx);
            PaletteManager.selectedBrushIdx = resultIdx;
            OnPaletteChange();
        }

        public void CreateBrushFromSelection(GameObject[] selectionPrefabs)
        {
            if (selectionPrefabs.Length == 0) return;

            RegisterUndo("Add Brush");
            AddLabels(selectionPrefabs[0]);
            var brush = MultibrushSettings.Create(selectionPrefabs[0], PaletteManager.selectedPalette);
            if (brush == null) return;
            PaletteManager.selectedPalette.AddBrush(brush);
            DeselectAllButThis(PaletteManager.selectedPalette.brushes.Length - 1);
            for (int i = 1; i < selectionPrefabs.Length; ++i)
            {
                AddLabels(selectionPrefabs[i]);
                brush.AddItem(new MultibrushItemSettings(selectionPrefabs[i], brush));
            }
            OnPaletteChange();
        }

        public void CreateBrushFromSelection(GameObject selectedPrefab)
            => CreateBrushFromSelection(new GameObject[] { selectedPrefab });

        public void CreateBushFromEachPrefabSelected()
        {
            var selectionPrefabs = SelectionManager.GetSelectionPrefabs();
            if (selectionPrefabs.Length == 0) return;
            foreach (var obj in selectionPrefabs)
            {
                if (obj == null) continue;
                RegisterUndo("Add Brush");
                var brush = MultibrushSettings.Create(obj, PaletteManager.selectedPalette);
                if (brush == null) continue;
                AddLabels(obj);
                PaletteManager.selectedPalette.AddBrush(brush);
            }
            DeselectAllButThis(PaletteManager.selectedPalette.brushes.Length - 1);
            OnPaletteChange();
        }
    }
}
#pragma warning restore UDR0001
