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
    public partial class PrefabPalette : UnityEditor.EditorWindow, ISerializationCallbackReceiver
    {
        private GUIContent _newBrushIcon = null;
        private GUIContent _deleteBrushIcon = null;
        private GUIContent _pickerIcon = null;
        private GUIContent _settingsIcon = null;
        private void Bottom()
        {
            using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.toolbar, GUILayout.Height(18)))
            {
                if (PaletteManager.selectedPalette.brushCount > 0)
                {
                    var sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
                    sliderStyle.margin.top = 0;
                    PaletteManager.iconSize = (int)GUILayout.HorizontalSlider(
                        (float)PaletteManager.iconSize,
                        (float)MIN_ICON_SIZE,
                        (float)MAX_ICON_SIZE,
                        sliderStyle,
                        GUI.skin.horizontalSliderThumb,
                        GUILayout.MaxWidth(128));
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(_newBrushIcon, UnityEditor.EditorStyles.toolbarButton)) PaletteContextMenu();
                using (new UnityEditor.EditorGUI.DisabledGroupScope(PaletteManager.selectionCount == 0))
                {
                    if (GUILayout.Button(_deleteBrushIcon, UnityEditor.EditorStyles.toolbarButton)) OnDelete();
                }
                PaletteManager.pickingBrushes = GUILayout.Toggle(PaletteManager.pickingBrushes,
                    _pickerIcon, UnityEditor.EditorStyles.toolbarButton);
                if (GUILayout.Button(_settingsIcon, UnityEditor.EditorStyles.toolbarButton)) SettingsContextMenu();
            }
            var rect = GUILayoutUtility.GetLastRect();
            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.DragUpdated
                    || Event.current.type == EventType.MouseDrag || Event.current.type == EventType.DragPerform)
                    Event.current.Use();
            }
        }

        private void OnDelete()
        {
            RegisterUndo("Delete Brush");
            DeleteBrushSelection();
            PaletteManager.ClearSelection();
            OnPaletteChange();
        }

        public void Reload(bool clearSelection)
        {
            if (clearSelection) PaletteManager.ClearSelection(true);
            _updateTabSize = true;
            OnPaletteChange();
        }

        private void SettingsContextMenu()
        {
            var menu = new UnityEditor.GenericMenu();
            menu.AddItem(new GUIContent(PaletteManager.viewList ? "Grid View" : "List View"), false,
                () => PaletteManager.viewList = !PaletteManager.viewList);
            if (!PaletteManager.viewList)
                menu.AddItem(new GUIContent("Show Brush Name"), PaletteManager.showBrushName,
                () => PaletteManager.showBrushName = !PaletteManager.showBrushName);
            if (PaletteManager.selectedPalette.brushCount > 1)
            {
                menu.AddItem(new GUIContent("Ascending Sort"), false,
                    () => { PaletteManager.selectedPalette.AscendingSort(); });
                menu.AddItem(new GUIContent("Descending Sort"), false,
                    () => { PaletteManager.selectedPalette.DescendingSort(); });
            }
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Rename palette..."), false, ShowRenamePaletteWindow,
                       new RenameData(PaletteManager.selectedPalette, PaletteManager.selectedPalette.name,
                       position.position + Event.current.mousePosition));
            menu.AddItem(new GUIContent("Delete palette"), false, ShowDeleteConfirmation,
                PaletteManager.selectedPalette);
            menu.AddItem(new GUIContent("Cleanup palette"), false, () =>
            {
                PaletteManager.Cleanup();
                PaletteManager.RepairItemParentIds();
                ResetInitialization();
                OnPaletteChange();
                UpdateTabBar();
                Repaint();
            });
            menu.AddItem(new GUIContent("Load palette files"), false, () =>
            {
                PaletteManager.instance.LoadPaletteFiles(true);
                ResetInitialization();
                Reload(!ThumbnailUtils.savingImage);
            });
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Update all thumbnails"), false, UpdateAllThumbnails);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Brush creation settings..."), false,
                BrushCreationSettingsWindow.ShowWindow);
            menu.ShowAsContext();
        }
    }
}
#pragma warning restore UDR0001
