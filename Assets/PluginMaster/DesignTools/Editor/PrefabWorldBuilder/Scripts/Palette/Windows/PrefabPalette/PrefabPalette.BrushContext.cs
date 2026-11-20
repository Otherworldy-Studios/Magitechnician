/*
Copyright(c) Omar Duarte
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
    public partial class PrefabPalette : UnityEditor.EditorWindow, ISerializationCallbackReceiver
    {
        public void DeselectAllButThis(int index)
        {
            if (PaletteManager.selectedBrushIdx == index && PaletteManager.selectionCount == 1) return;
            PaletteManager.ClearSelection();
            if (index < 0) return;
            PaletteManager.AddToSelection(index);
            PaletteManager.selectedBrushIdx = index;
        }
        private void DeleteBrushSelection()
        {
            var descendingSelection = PaletteManager.idxSelection;
            System.Array.Sort<int>(descendingSelection, new System.Comparison<int>((i1, i2) => i2.CompareTo(i1)));
            foreach (var i in descendingSelection) PaletteManager.selectedPalette.RemoveBrushAt(i);
        }
        private void DeleteBrush(object idx)
        {
            RegisterUndo("Delete Brush");
            if (PaletteManager.SelectionContains((int)idx)) DeleteBrushSelection();
            else PaletteManager.selectedPalette.RemoveBrushAt((int)idx);
            PaletteManager.ClearSelection();
            OnPaletteChange();
        }
        private void CopyBrushSettings(object idx)
            => PaletteManager.clipboardSetting = PaletteManager.selectedPalette.brushes[(int)idx].CloneMainSettings();
        private void PasteBrushSettings(object idx)
        {
            RegisterUndo("Paste Brush Settings");
            PaletteManager.selectedPalette.brushes[(int)idx].Copy(PaletteManager.clipboardSetting);
            if (BrushProperties.instance != null) BrushProperties.instance.Repaint();
            PaletteManager.selectedPalette.Save();
        }
        private void DuplicateBrush(object idx)
        {
            RegisterUndo("Duplicate Brush");
            if (PaletteManager.SelectionContains((int)idx))
            {
                var descendingSelection = PaletteManager.idxSelection;
                System.Array.Sort<int>(descendingSelection, new System.Comparison<int>((i1, i2) => i2.CompareTo(i1)));
                for (int i = 0; i < descendingSelection.Length; ++i)
                {
                    PaletteManager.selectedPalette.DuplicateBrush(descendingSelection[i], out _);
                    descendingSelection[i] += descendingSelection.Length - 1 - i;
                }
                PaletteManager.idxSelection = descendingSelection;
            }
            else PaletteManager.selectedPalette.DuplicateBrush((int)idx, out _);
            OnPaletteChange();
        }

        private void CreateSingleBrushes(object idx)
        {
            RegisterUndo("Create Single Brushes From Multibrush Items");
            var index = (int)idx;
            if (PaletteManager.SelectionContains(index))
            {
                for (int i = 0; i < PaletteManager.idxSelection.Length; ++i)
                    PaletteManager.selectedPalette.CreateSingleBrushes(PaletteManager.idxSelection[i]);
            }
            else PaletteManager.selectedPalette.CreateSingleBrushes(index);
            OnPaletteChange();
        }
        private void OnMergeBrushesContext()
        {
            RegisterUndo("Merge Brushes");
            var selection = new System.Collections.Generic.List<int>(PaletteManager.idxSelection);
            selection.Sort();
            var resultIdx = selection[0];
            selection.RemoveAt(0);
            selection.Reverse();
            var result = PaletteManager.selectedPalette.GetBrush(resultIdx);
            if (result == null)
            {
                PaletteManager.selectedPalette.Cleanup();
                return;
            }
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
                    var clone = item.Clone() as MultibrushItemSettings;
                    clone.parentSettings = result;
                    result.AddItem(clone);
                }
                PaletteManager.selectedPalette.RemoveBrushAt(idx);
            }
            if (cleanupPalette) PaletteManager.selectedPalette.Cleanup();
            PaletteManager.ClearSelection();
            PaletteManager.AddToSelection(resultIdx);
            PaletteManager.selectedBrushIdx = resultIdx;
            OnPaletteChange();
        }
        private void SelectPrefabs(object idx)
        {
            var prefabs = new System.Collections.Generic.List<GameObject>();
            if (PaletteManager.SelectionContains((int)idx))
            {
                foreach (int selectedIdx in PaletteManager.idxSelection)
                {
                    var brush = PaletteManager.selectedPalette.GetBrush(selectedIdx);
                    foreach (var item in brush.items)
                    {
                        if (item.prefab != null) prefabs.Add(item.prefab);
                    }
                }
            }
            else
            {
                var brush = PaletteManager.selectedPalette.GetBrush((int)idx);
                foreach (var item in brush.items)
                {
                    if (item.prefab != null) prefabs.Add(item.prefab);
                }
            }
            UnityEditor.Selection.objects = prefabs.ToArray();
        }

        private void OpenPrefab(object idx)
            => UnityEditor.AssetDatabase.OpenAsset(PaletteManager.selectedPalette.GetBrush((int)idx).items[0].prefab);

        private void SelectReferences(object idx)
        {
            var items = PaletteManager.selectedPalette.GetBrush((int)idx).items;
#if UNITY_6000_3_OR_NEWER
            var itemsprefabIds = new System.Collections.Generic.List<EntityId>();
#else
            var itemsprefabIds = new System.Collections.Generic.List<int>();
#endif
            foreach (var item in items)
            {
                if (item.prefab != null)
#if UNITY_6000_3_OR_NEWER
                    itemsprefabIds.Add(item.prefab.GetEntityId());
#else
                    itemsprefabIds.Add(item.prefab.GetInstanceID());
#endif
            }
            var selection = new System.Collections.Generic.List<GameObject>();
#if UNITY_6000_4_OR_NEWER
            var objects = Object.FindObjectsByType<Transform>();
#elif UNITY_2022_2_OR_NEWER
            var objects = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
#else
            var objects = Object.FindObjectsOfType<Transform>();
#endif
            foreach (var obj in objects)
            {
                var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (source == null) continue;
#if UNITY_6000_3_OR_NEWER
                var sourceId = source.gameObject.GetEntityId();
#else
                var sourceId = source.gameObject.GetInstanceID();
#endif
                if (itemsprefabIds.Contains(sourceId)) selection.Add(obj.gameObject);
            }
            UnityEditor.Selection.objects = selection.ToArray();
        }

        private void UpdateThumbnail(object idx) => PaletteManager.UpdateSelectedThumbnails();

        private void EditThumbnail(object idx)
        {
            var brushIdx = (int)idx;
            var brush = PaletteManager.selectedPalette.GetBrush(brushIdx);
            ThumbnailEditorWindow.ShowWindow(brush, brushIdx);
        }

        private void CopyThumbnailSettings(object idx)
        {
            var brush = PaletteManager.selectedPalette.brushes[(int)idx];
            PaletteManager.clipboardThumbnailSettings = brush.thumbnailSettings.Clone();
            PaletteManager.clipboardOverwriteThumbnailSettings = PaletteManager.Trit.SAME;
        }

        private void PasteThumbnailSettings(object idx)
        {
            if (PaletteManager.clipboardThumbnailSettings == null) return;
            RegisterUndo("Paste Thumbnail Settings");
            void Paste(MultibrushSettings brush)
            {
                brush.thumbnailSettings.Copy(PaletteManager.clipboardThumbnailSettings);
                ThumbnailUtils.UpdateThumbnail(brushSettings: brush, updateItemThumbnails: true, savePng: true);
            }
            if (PaletteManager.SelectionContains((int)idx))
            {
                foreach (var i in PaletteManager.idxSelection) Paste(PaletteManager.selectedPalette.brushes[i]);
            }
            else Paste(PaletteManager.selectedPalette.brushes[(int)idx]);
            PaletteManager.selectedPalette.Save();
        }
        private void BrushContext(int idx)
        {
            void ShowBrushProperties(object idx)
            {
                PaletteManager.ClearSelection();
                PaletteManager.AddToSelection((int)idx);
                PaletteManager.selectedBrushIdx = (int)idx;
                BrushProperties.ShowWindow();
            }
            var menu = new UnityEditor.GenericMenu();
            menu.AddItem(new GUIContent("Brush Properties..."), false, ShowBrushProperties, idx);
            menu.AddSeparator(string.Empty);
            var brush = PaletteManager.selectedPalette.GetBrush(idx);
            menu.AddItem(new GUIContent("Select Prefab" + (PaletteManager.selectionCount > 1
                || brush.itemCount > 1 ? "s" : "")), false, SelectPrefabs, idx);
            if (brush.itemCount == 1) menu.AddItem(new GUIContent("Open Prefab"), false, OpenPrefab, idx);
            menu.AddItem(new GUIContent("Select References In Scene"), false, SelectReferences, idx);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Update Thumbnail"), false, UpdateThumbnail, idx);
            if (!PWBCore.staticData.useAssetPreview)
            {
                menu.AddItem(new GUIContent("Edit Thumbnail..."), false, EditThumbnail, idx);
                menu.AddItem(new GUIContent("Copy Thumbnail Settings"), false, CopyThumbnailSettings, idx);
                if (PaletteManager.clipboardThumbnailSettings != null)
                    menu.AddItem(new GUIContent("Paste Thumbnail Settings"), false, PasteThumbnailSettings, idx);
            }
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Delete"), false, DeleteBrush, idx);
            menu.AddItem(new GUIContent("Duplicate"), false, DuplicateBrush, idx);
            menu.AddItem(new GUIContent("Create Single Brushes From Multibrush Items"), false, CreateSingleBrushes, idx);
            if (PaletteManager.selectionCount > 1) menu.AddItem(new GUIContent("Merge"), false, OnMergeBrushesContext);
            if (PaletteManager.selectionCount == 1)
                menu.AddItem(new GUIContent("Copy Brush Settings"), false, CopyBrushSettings, idx);
            if (PaletteManager.clipboardSetting != null)
                menu.AddItem(new GUIContent("Paste Brush Settings"), false, PasteBrushSettings, idx);
            menu.AddSeparator(string.Empty);
            PaletteContextAddMenuItems(menu);
            menu.ShowAsContext();
        }
    }
}
#pragma warning restore UDR0001
