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
    public partial class PaletteManager : ISerializationCallbackReceiver
    {
        #region BRUSH SELECTION
        [SerializeField] private int _selectedBrushIdx = -1;
        private System.Collections.Generic.HashSet<int> _idxSelection = new System.Collections.Generic.HashSet<int>();

        private bool _pickingBrushes = false;
        private PaletteData _movingBrushesFrom = null;
        public static bool movingBrushes => instance._movingBrushesFrom != null;
        private System.Collections.Generic.List<MultibrushSettings> _brushesToMove
            = new System.Collections.Generic.List<MultibrushSettings>();


        public static int selectedBrushIdx
        {
            get => instance._selectedBrushIdx;
            set
            {
                if (instance._selectedBrushIdx == value) return;
                instance._selectedBrushIdx = value;
                if (selectedBrush != null)
                {
                    selectedBrush.UpdateBottomVertices();
                    selectedBrush.UpdateAssetTypes();
                }
                else instance._selectedBrushIdx = -1;
                if (PWBCore.staticData.openBrushPropertiesWhenABrushIsSelected && instance._selectedBrushIdx >= 0)
                    BrushProperties.ShowWindow();
                if (ToolController.current == ToolController.Tool.PIN)
                {
                    BrushstrokeManager.UpdateBrushstroke(true);
                    PWBIO.ResetPinValues();
                }
                if (OnBrushSelectionChanged != null) OnBrushSelectionChanged();
                BrushstrokeManager.UpdateBrushstroke(true);
            }
        }

        public static bool pickingBrushes
        {
            get => instance._pickingBrushes;
            set
            {
                if (instance._pickingBrushes == value) return;
                instance._pickingBrushes = value;
                if (instance._pickingBrushes)
                {
                    PWBIO.repaint = true;
                    UnityEditor.SceneView.RepaintAll();
                    if (UnityEditor.SceneView.sceneViews.Count > 0)
                        ((UnityEditor.SceneView)UnityEditor.SceneView.sceneViews[0]).Focus();
                }
                PrefabPalette.RepaintWindow();
            }
        }


        public static MultibrushSettings selectedBrush
            => (instance._selectedBrushIdx < 0 || selectedPalette == null) ? null
            : selectedPalette.GetBrush(instance._selectedBrushIdx);

        public static void SelectBrush(int idx)
        {
            if (PrefabPalette.instance == null) return;
            if (selectedPalette.brushCount == 0) return;
            if (!PrefabPalette.instance.FilteredBrushListContains(idx)) return;
            instance._idxSelection.Clear();
            selectedBrushIdx = idx;
            if (selectedBrush != null)
            {
                selectedBrush.UpdateBottomVertices();
                selectedBrush.UpdateAssetTypes();
            }
            AddToSelection(selectedBrushIdx);
            PrefabPalette.instance.FrameSelectedBrush();
            PrefabPalette.RepaintWindow();
        }

        public static void SelectNextBrush()
        {
            if (PrefabPalette.instance == null) return;
            if (selectedPalette.brushCount <= 1) return;
            instance._idxSelection.Clear();
            int selectedIdx = instance._selectedBrushIdx;
            int count = 0;
            do
            {
                selectedIdx = (selectedIdx + 1) % selectedPalette.brushCount;
                if (++count > selectedPalette.brushCount) return;
            }
            while (!PrefabPalette.instance.FilteredBrushListContains(selectedIdx));
            selectedBrushIdx = selectedIdx;
            if (selectedBrush != null)
            {
                selectedBrush.UpdateBottomVertices();
                selectedBrush.UpdateAssetTypes();
            }
            AddToSelection(selectedBrushIdx);
            PrefabPalette.instance.FrameSelectedBrush();
        }

        public static void SelectPreviousBrush()
        {
            if (PrefabPalette.instance == null) return;
            if (selectedPalette.brushCount <= 1) return;
            instance._idxSelection.Clear();
            int selectedIdx = instance._selectedBrushIdx;
            int count = 0;
            do
            {
                selectedIdx = (selectedIdx == 0 ? selectedPalette.brushCount : selectedIdx) - 1;
                if (++count > selectedPalette.brushCount) return;
            }
            while (!PrefabPalette.instance.FilteredBrushListContains(selectedIdx));
            selectedBrushIdx = selectedIdx;
            if (selectedBrush != null)
            {
                selectedBrush.UpdateBottomVertices();
                selectedBrush.UpdateAssetTypes();
            }
            AddToSelection(selectedBrushIdx);
            PrefabPalette.instance.FrameSelectedBrush();
        }
        public static PaletteData InitializeSelectedPalette()
        {
            if (instance._selectedPalette != null) return instance._selectedPalette;
            bool createEmptyPalette = true;
            PaletteData SelectFirstPalette()
            {
                createEmptyPalette = false;
                if (instance._selectedPaletteId != -1)
                {
                    instance._selectedPalette = GetPalette(instance._selectedPaletteId);
                    if (instance._selectedPalette == null)
                    {
                        instance._selectedPaletteId = -1;
                        return null;
                    }
                    if (PrefabPalette.instance) PrefabPalette.instance.OnPaletteChange();
                    return instance._selectedPalette;
                }
                else if (!PWBCore._loadedFromFile)
                {
                    return null;
                }
                if (instance._pinnedPaletteDataList.Count > 0)
                {
                    instance._selectedPalette = instance._pinnedPaletteDataList.First();
                    return instance._selectedPalette;
                }
                if (instance._nonPinnedPaletteDataList.Count > 0)
                {
                    instance._selectedPalette = instance._nonPinnedPaletteDataList.First();
                    return instance._selectedPalette;
                }
                createEmptyPalette = true;
                return null;
            }
            var selectedPalette = SelectFirstPalette();
            if (selectedPalette != null) return selectedPalette;
            instance._pinnedPaletteDataList.Clear();
            instance._nonPinnedPaletteDataList.Clear();
            instance.LoadPaletteFiles(true);
            selectedPalette = SelectFirstPalette();
            if (selectedPalette != null) return selectedPalette;
            if (createEmptyPalette)
                instance.CreateEmptyPalette();
            return instance._selectedPalette;
        }

        
        public static PaletteData selectedPalette
        {
            get => instance._selectedPalette;
            set
            {
                if (value == null) return;
                if (value == instance._selectedPalette) return;
                bool isInAList = false;
                if (instance._pinnedPaletteDataList.Contains(value)) isInAList = true;
                if (!isInAList && instance._nonPinnedPaletteDataList.Contains(value)) isInAList = true;
                if (!isInAList) return;
                instance._selectedPalette = value;
                instance._selectedPaletteId = value.id;
                OnPaletteChanged();
                PWBCore.SetSavePending();
            }
        }

        public static bool IsPaletteSelected(PaletteData palette)
            => instance._selectedPalette != null && palette != null && instance._selectedPalette.id == palette.id;

        public static void ShowSelectedFirst()
        {
            var selected = selectedPalette;
            instance.nonPinnedPaletteDataList.RemoveAll(p => p.id == selected.id);
            instance.nonPinnedPaletteDataList.Insert(0, selected);
        }
        #endregion
        #region MULTI-SELECTION
        
        public static int[] idxSelection
        {
            get => instance._idxSelection.ToArray();
            set
            {
                instance._idxSelection = new System.Collections.Generic.HashSet<int>(value);
                if (OnSelectionChanged != null) OnSelectionChanged();
            }
        }

        public static int selectionCount
        {
            get
            {
                if (instance._idxSelection.Count == 0 && instance._selectedBrushIdx > 0 && selectedBrush != null)
                {
                    instance._idxSelection.Add(instance._selectedBrushIdx);
                    if (OnSelectionChanged != null) OnSelectionChanged();
                }
                return instance._idxSelection.Count;
            }
        }

        public static void AddToSelection(int index)
        {
            instance._idxSelection.Add(index);
            if (OnSelectionChanged != null) OnSelectionChanged();
        }

        public static bool SelectionContains(int index) => instance._idxSelection.Contains(index);

        public static void RemoveFromSelection(int index)
        {
            instance._idxSelection.Remove(index);
            if (OnSelectionChanged != null) OnSelectionChanged();
        }

        public static void ClearSelection(bool updateBrushProperties = true)
        {
            selectedBrushIdx = -1;
            instance._idxSelection.Clear();
            if (!updateBrushProperties) return;
            if (OnSelectionChanged != null) OnSelectionChanged();
            BrushProperties.RepaintWindow();
        }
        #endregion
        #region BRUSH MOVING OPERATIONS
        public static void SelectBrushesToMove()
        {
            instance._movingBrushesFrom = selectedPalette;
            instance._brushesToMove.Clear();
            foreach (var idx in instance._idxSelection) instance._brushesToMove.Add(selectedPalette.GetBrush(idx));
        }

        public static void MoveBrushesToAnotherPalette(PaletteData destinationPalette, bool removeFromSource)
        {
            if (instance._movingBrushesFrom == null) return;
            if (instance._movingBrushesFrom != destinationPalette)
            {
                var sourcePalette = instance._movingBrushesFrom;
                foreach (var brush in instance._brushesToMove)
                {
                    destinationPalette.AddBrush(brush);
                    if (removeFromSource) sourcePalette.RemoveBrush(brush);
                }
            }
            instance._brushesToMove.Clear();
            instance._movingBrushesFrom = null;
        }

        public static void MoveBrushesToSelectedPalette() => MoveBrushesToAnotherPalette(selectedPalette, true);

        public static void PasteBrushesToSelectedPalette() => MoveBrushesToAnotherPalette(selectedPalette, false);
        #endregion
    }
}
#pragma warning restore UDR0001
