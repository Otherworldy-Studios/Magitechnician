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
        private Vector3 _selectedBrushPosition = Vector3.zero;
        private bool _frameSelectedBrush = false;
        private bool _newSelectedPositionSet = false;
        private Texture2D _loadingIcon = null;

        public void FrameSelectedBrush()
        {
            _frameSelectedBrush = true;
            _newSelectedPositionSet = false;
        }

        private void DoFrameSelectedBrush()
        {
            _frameSelectedBrush = false;
            if (_scrollPosition.y > _selectedBrushPosition.y
                || _scrollPosition.y + _scrollViewRect.height < _selectedBrushPosition.y)
                _scrollPosition.y = _selectedBrushPosition.y - 4;
            RepaintWindow();
        }

        private static bool _edittingThumbnail = false;
        private static int _edittingThumbnailIdx = -1;

        private void Brushes(ref BrushInputData toggleData)
        {
            if (Event.current.control && Event.current.keyCode == KeyCode.A && _filteredBrushList.Count > 0)
            {
                PaletteManager.ClearSelection();
                foreach (var brush in _filteredBrushList) PaletteManager.AddToSelection(brush.index);
                PaletteManager.selectedBrushIdx = _filteredBrushList[0].index;
                Repaint();
            }
            if (PaletteManager.selectedPalette.brushCount == 0) return;
            if (filteredBrushListCount == 0) return;

            var filteredBrushes = filteredBrushList.ToArray();
            int filterBrushIdx = 0;

            var nameStyle = GUIStyle.none;
            nameStyle.margin = new RectOffset(2, 2, 0, 1);
            nameStyle.clipping = TextClipping.Clip;
            nameStyle.fontSize = 8;
            nameStyle.normal.textColor = Color.white;

            MultibrushSettings brushSettings = null;
            int brushIdx = -1;
            Texture2D icon = null;

            void GetBrushSettings(ref GUIStyle style)
            {
                brushSettings = filteredBrushes[filterBrushIdx].brush;
                brushIdx = filteredBrushes[filterBrushIdx].index;
                if (PaletteManager.SelectionContains(brushIdx))
                    style.normal = _toggleStyle.onNormal;
                icon = brushSettings.thumbnail;
                if (icon == null) icon = _loadingIcon;
            }

            void GetInputData(ref BrushInputData inputData)
            {
                var rect = GUILayoutUtility.GetLastRect();
                void GetPaletteInputData(ref BrushInputData data)
                {
                    data = new BrushInputData(brushIdx, rect, Event.current.type,
                    Event.current.control, Event.current.shift, Event.current.mousePosition.x);
                }
                if (rect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDrag && Event.current.button == 1
                        && Event.current.delta != Vector2.zero)
                    {
                        if (!_edittingThumbnail) _edittingThumbnailIdx = brushIdx;
                        if (!Event.current.control && !Event.current.shift)
                        {
                            var brush = PaletteManager.selectedPalette.GetBrush(_edittingThumbnailIdx);
                            if (brush.thumbnailSettings.useCustomImage || PWBCore.staticData.useAssetPreview)
                            {
                                GetPaletteInputData(ref inputData);
                                return;
                            }
                            var rot = Quaternion.Euler(brush.thumbnailSettings.targetEuler);
                            brush.thumbnailSettings.targetEuler = (Quaternion.AngleAxis(Event.current.delta.y, Vector3.left)
                                 * Quaternion.AngleAxis(Event.current.delta.x, Vector3.down) * rot).eulerAngles;
                            brush.UpdateThumbnail(true, false);
                            Event.current.Use();
                            _edittingThumbnail = true;
                        }
                        else if (Event.current.control && !Event.current.shift)
                        {
                            var brush = PaletteManager.selectedPalette.GetBrush(_edittingThumbnailIdx);
                            if (brush.thumbnailSettings.useCustomImage || PWBCore.staticData.useAssetPreview)
                            {
                                GetPaletteInputData(ref inputData);
                                return;
                            }
                            var delta = Event.current.delta / PaletteManager.iconSize;
                            delta.y = -delta.y;
                            brush.thumbnailSettings.targetOffset = Vector2.Min(Vector2.one,
                                Vector2.Max(brush.thumbnailSettings.targetOffset + delta, -Vector2.one));
                            brush.UpdateThumbnail(true, false);
                            Event.current.Use();
                            _edittingThumbnail = true;
                        }
                    }
                    else if (Event.current.type == EventType.ContextClick && _edittingThumbnail)
                    {
                        var brush = PaletteManager.selectedPalette.GetBrush(brushIdx);
                        if (brush.thumbnailSettings.useCustomImage || PWBCore.staticData.useAssetPreview)
                        {
                            GetPaletteInputData(ref inputData);
                            return;
                        }
                        brush.UpdateThumbnail(true, true);
                        Event.current.Use();
                        _edittingThumbnail = false;
                    }
                    else if (Event.current.isScrollWheel && Event.current.control && !Event.current.shift)
                    {
                        var brush = PaletteManager.selectedPalette.GetBrush(brushIdx);
                        if (brush.thumbnailSettings.useCustomImage || PWBCore.staticData.useAssetPreview)
                        {
                            GetPaletteInputData(ref inputData);
                            return;
                        }
                        var scrollSign = Mathf.Sign(Event.current.delta.y);
                        brush.thumbnailSettings.zoom += scrollSign * 0.1f;
                        brush.UpdateThumbnail(true, false);
                        Event.current.Use();
                    }
                    else GetPaletteInputData(ref inputData);
                }
                if (Event.current.type != EventType.Layout && PaletteManager.selectedBrushIdx == brushIdx)
                {
                    _selectedBrushPosition = rect.position;
                    _newSelectedPositionSet = true;
                }
            }

            void GridViewRow(ref BrushInputData inputData)
            {
                using (new GUILayout.HorizontalScope())
                {
                    for (int col = 0; col < _columnCount && filterBrushIdx < filteredBrushes.Length; ++col)
                    {
                        var style = new GUIStyle(_toggleStyle);
                        GetBrushSettings(ref style);
                        using (new GUILayout.VerticalScope(style))
                        {
                            if (PaletteManager.showBrushName)
                                GUILayout.Box(new GUIContent(brushSettings.name, brushSettings.name),
                                    nameStyle, GUILayout.Width(PaletteManager.iconSize));
                            GUILayout.Box(new GUIContent(icon, brushSettings.name), GUIStyle.none,
                                GUILayout.Width(PaletteManager.iconSize),
                            GUILayout.Height(PaletteManager.iconSize));
                        }
                        GetInputData(ref inputData);
                        ++filterBrushIdx;
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            void ListView(ref BrushInputData inputData)
            {
                var style = new GUIStyle(_toggleStyle);
                style.padding = new RectOffset(0, 0, 0, 0);
                GetBrushSettings(ref style);
                using (new GUILayout.HorizontalScope(style))
                {
                    GUILayout.Box(new GUIContent(icon, brushSettings.name), GUIStyle.none,
                        GUILayout.Width(PaletteManager.iconSize),
                        GUILayout.Height(PaletteManager.iconSize));
                    GUILayout.Space(4);
                    using (new GUILayout.VerticalScope())
                    {
                        var span = (PaletteManager.iconSize - 16) / 2;
                        GUILayout.Space(span);
                        GUILayout.Box(new GUIContent(brushSettings.name, brushSettings.name), nameStyle);
                        GUILayout.Space(span);
                    }
                }
                GetInputData(ref inputData);
                ++filterBrushIdx;
            }
            nameStyle.fontSize = PaletteManager.viewList ? 12 : 8;
            nameStyle.fontSize = Mathf.Max(Mathf.RoundToInt(nameStyle.fontSize
                * ((float)PaletteManager.iconSize / (float)PrefabPalette.DEFAULT_ICON_SIZE)), nameStyle.fontSize);

            while (filterBrushIdx < filteredBrushes.Length)
            {
                if (PaletteManager.viewList) ListView(ref toggleData);
                else GridViewRow(ref toggleData);
            }
        }
    }
}
#pragma warning restore UDR0001
