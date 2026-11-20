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
    public partial class ToolModes
    {
        private void BlockSettingsGUI(BlockToolModes.EditMode editMode,
            bool showDrawModes, bool isSelect)
        {
            if (showDrawModes)
            {
                var drawMode = BlockToolModes.selectedDrawMode;
                if (drawMode == BlockToolModes.DrawMode.BLOCK_BY_BLOCK)
                    BlockByBlockSettingsGUI();
                else if (drawMode == BlockToolModes.DrawMode.LINE)
                    LineSettingsGUI();
                else if (drawMode == BlockToolModes.DrawMode.FACE)
                    FaceSettingsGUI();
            }
            else if (editMode == BlockToolModes.EditMode.MOVE)
                MoveSettingsGUI();
            else if (isSelect
                && BlockToolModes.selectMode == BlockToolModes.SelecMode.REGION)
                RegionSelectSettingsGUI();
            else if (editMode == BlockToolModes.EditMode.REPLACE)
                ReplaceSettingsGUI();
        }

        private void BlockByBlockSettingsGUI()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox, GUILayout.Width(153)))
            {
                using (new GUILayout.HorizontalScope())
                {
                    UnityEditor.EditorGUILayout.LabelField("Size:",
                        GUILayout.Width(34));
                    if (GUILayout.Button("-", UnityEditor.EditorStyles.miniButtonMid,
                        GUILayout.Width(24)))
                    {
                        BlockToolModes.brushSize--;
                        UnityEditor.SceneView.RepaintAll();
                    }
                    using (new UnityEditor.EditorGUI.DisabledGroupScope(true))
                    {
                        GUILayout.Label(BlockToolModes.brushSize.ToString(), UnityEditor.EditorStyles.miniButtonMid,
                        GUILayout.Width(24));
                    }
                    if (GUILayout.Button("+", UnityEditor.EditorStyles.miniButtonMid,
                        GUILayout.Width(24)))
                    {
                        BlockToolModes.brushSize++;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var shape = (int)BlockToolModes.selectedBrushShape;
                    UnityEditor.EditorGUIUtility.labelWidth = 44;
                    shape = UnityEditor.EditorGUILayout.Popup("Shape:",
                        shape, _brushShapeOptions);
                    if (check.changed)
                    {
                        BlockToolModes.selectedBrushShape
                            = (BlockToolModes.BrushShape)shape;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
        }

        private void LineSettingsGUI()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox, GUILayout.Width(153)))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var projection = (int)BlockToolModes.projectionAxis;
                    UnityEditor.EditorGUIUtility.labelWidth = 64;
                    projection = UnityEditor.EditorGUILayout.Popup("Projection:",
                        projection, _projectionOptions);
                    if (check.changed)
                    {
                        BlockToolModes.projectionAxis
                            = (BlockToolModes.ProjectionAxis)projection;
                        UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
        }

        private void FaceSettingsGUI()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox, GUILayout.Width(153)))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 74;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var connectivity = (int)BlockToolModes.faceConectivity;
                    connectivity = UnityEditor.EditorGUILayout.Popup(
                        "Conectivity:", connectivity, _conectivityOptions);
                    if (check.changed)
                        BlockToolModes.faceConectivity
                            = (BlockToolModes.FaceConectivity)connectivity;
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var directions
                        = (int)BlockToolModes.faceNeighborSearchingDirections;
                    directions = UnityEditor.EditorGUILayout.Popup(
                        "Directions:", directions, _directionsOptions);
                    if (check.changed)
                        BlockToolModes.faceNeighborSearchingDirections
                            = (BlockToolModes
                                .FaceNeighborSearchingDirections)directions;
                }
            }
        }

        private void MoveSettingsGUI()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox, GUILayout.Width(153)))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 60;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var selMode = (int)BlockToolModes.moveSelectionMode;
                    UnityEditor.EditorGUILayout.LabelField("Selection Mode:");
                    selMode = UnityEditor.EditorGUILayout.Popup(selMode, _selectionModeOptions);
                    if (check.changed)
                        BlockToolModes.moveSelectionMode
                            = (BlockToolModes.MoveSelectionMode)selMode;
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var connectivity = (int)BlockToolModes.moveConectivity;
                    UnityEditor.EditorGUILayout.LabelField("Connectivity:");
                    connectivity = UnityEditor.EditorGUILayout.Popup(connectivity, _conectivityOptions);
                    if (check.changed)
                        BlockToolModes.moveConectivity
                            = (BlockToolModes.MoveConectivity)connectivity;
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var directions
                        = (int)BlockToolModes.moveNeighborSearchingDirections;
                    UnityEditor.EditorGUILayout.LabelField("Directions:");
                    directions = UnityEditor.EditorGUILayout.Popup(directions, _directionsOptions);
                    if (check.changed)
                        BlockToolModes.moveNeighborSearchingDirections
                            = (BlockToolModes
                                .MoveNeighborSearchingDirections)directions;
                }
            }
        }

        private void ReplaceSettingsGUI()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox, GUILayout.Width(153)))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 60;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var mode = (int)BlockToolModes.replaceMode;
                    UnityEditor.EditorGUILayout.LabelField("Replace Mode:");
                    mode = UnityEditor.EditorGUILayout.Popup(mode, _replaceModeOptions);
                    if (check.changed)
                        BlockToolModes.replaceMode
                            = (BlockToolModes.ReplaceMode)mode;
                }
                if (BlockToolModes.replaceMode
                    == BlockToolModes.ReplaceMode.REGION)
                {
                    using (var check
                        = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var selMode
                            = (int)BlockToolModes.replaceSelectionMode;
                        UnityEditor.EditorGUILayout.LabelField("Selection Mode:");
                        selMode = UnityEditor.EditorGUILayout.Popup(selMode, _selectionModeOptions);
                        if (check.changed)
                            BlockToolModes.replaceSelectionMode
                                = (BlockToolModes
                                    .ReplaceSelectionMode)selMode;
                    }
                    using (var check
                        = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var connectivity
                            = (int)BlockToolModes.replaceRegionConectivity;
                        UnityEditor.EditorGUILayout.LabelField("Connectivity:");
                        connectivity = UnityEditor.EditorGUILayout.Popup(connectivity, _conectivityOptions);
                        if (check.changed)
                            BlockToolModes.replaceRegionConectivity
                                = (BlockToolModes
                                    .ReplaceRegionConectivity)connectivity;
                    }
                    using (var check
                        = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var dir = (int)BlockToolModes
                            .replaceRegionSelectNeighborSearchingDirections;
                        UnityEditor.EditorGUILayout.LabelField("Directions:");
                        dir = UnityEditor.EditorGUILayout.Popup(dir, _directionsOptions);
                        if (check.changed)
                            BlockToolModes
                                .replaceRegionSelectNeighborSearchingDirections
                                = (BlockToolModes
                                    .ReplaceRegionSelectNeighborSearchingDirections)dir;
                    }
                }
            }
        }

        private void RegionSelectSettingsGUI()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox, GUILayout.Width(153)))
            {
                UnityEditor.EditorGUIUtility.labelWidth = 60;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var connectivity = (int)BlockToolModes.regionConectivity;
                    UnityEditor.EditorGUILayout.LabelField("Connectivity:");
                    connectivity = UnityEditor.EditorGUILayout.Popup(connectivity, _conectivityOptions);
                    if (check.changed)
                        BlockToolModes.regionConectivity
                            = (BlockToolModes.RegionConectivity)connectivity;
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var directions = (int)BlockToolModes
                        .regionSelectNeighborSearchingDirections;
                    UnityEditor.EditorGUILayout.LabelField("Directions:");
                    directions = UnityEditor.EditorGUILayout.Popup(directions, _regionDirectionsOptions);
                    if (check.changed)
                        BlockToolModes
                            .regionSelectNeighborSearchingDirections
                            = (BlockToolModes
                                .RegionSelectNeighborSearchingDirections)directions;
                }
            }
        }
    }
}
#pragma warning restore UDR0001
