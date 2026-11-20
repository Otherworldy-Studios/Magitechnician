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
    public class SnapSettingsWindow : UnityEditor.EditorWindow
    {
        private static readonly string[] _gridTypeOptions = { "Rectangular", "Radial" };

        
        private static SnapSettingsWindow _instance = null;
        [UnityEditor.MenuItem("Tools/Plugin Master/Prefab World Builder/Grid and Snapping Settings...", false, 1150)]
        public static void ShowWindow() => _instance = GetWindow<SnapSettingsWindow>("Grid and Snapping Settings");

        private GameObject _activeGameObject = null;

        public static void RepaintWindow()
        {
            if (_instance != null) _instance.Repaint();
        }

        private void OnEnable()
        {
            _activeGameObject = UnityEditor.Selection.activeGameObject;
        }

        private void OnGUI()
        {
            minSize = new Vector2(350, GridManager.settings.radialGridEnabled ? 290 : 310);
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    if (GridManager.settings.radialGridEnabled)
                    {
                        GridManager.settings.radialStep = UnityEditor.EditorGUILayout.FloatField("Radial Snap Value",
                            GridManager.settings.radialStep);
                    }
                    else
                    {
                        GridManager.settings.step = UnityEditor.EditorGUILayout.Vector3Field("Snap Value",
                            GridManager.settings.step);
                        GridManager.settings.midpointSnapping = UnityEditor.EditorGUILayout.ToggleLeft("Midpoint snapping",
                            GridManager.settings.midpointSnapping);
                        GridManager.settings.autoCameraAlignment
                            = UnityEditor.EditorGUILayout.ToggleLeft("Switch grid axis on camera alignment",
                            GridManager.settings.autoCameraAlignment);
                        GridManager.settings.drawGridAsTexture
                            = UnityEditor.EditorGUILayout.ToggleLeft("Draw grid as texture",
                            GridManager.settings.drawGridAsTexture);
                    }
                    if (check.changed) UnityEditor.SceneView.RepaintAll();
                }
                if (!GridManager.settings.radialGridEnabled)
                {
                    using (new UnityEditor.EditorGUI.DisabledGroupScope(_activeGameObject == null))
                    {
                        if (GUILayout.Button("Set the snap value to the size of the active gameobject"))
                        {
                            var bounds = BoundsUtils.GetBounds(_activeGameObject.transform);
                            GridManager.settings.step = bounds.size;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }
            }
            if (GridManager.settings.radialGridEnabled)
            {
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        GridManager.settings.radialSectors = UnityEditor.EditorGUILayout.IntField("Radial Sectors",
                            GridManager.settings.radialSectors);
                        if (check.changed) UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.helpBox))
                {
                    int originIndex = 0;
                    UnityEditor.EditorGUIUtility.labelWidth = 70;
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        originIndex = UnityEditor.EditorGUILayout.Popup("Grid origin",
                            GridManager.settings.GetIndexOfSelectedOrigin(), GridManager.settings.GetOriginNames());
                        if (check.changed)
                        {
                            GridManager.settings.SelectOrigin(originIndex);
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                    if (GUILayout.Button("Reset"))
                    {
                        GridManager.settings.ResetOrigin();
                    }
                    if (GUILayout.Button("Save..."))
                    {
                        RenameWindow.ShowWindow(position.position + Event.current.mousePosition,
                            GridManager.settings.SaveGridOrigin, "Save Origin", GridManager.settings.selectedOrigin);
                    }
                    using (new UnityEditor.EditorGUI.DisabledGroupScope(originIndex == 0))
                    {
                        if (GUILayout.Button("Delete"))
                        {
                            GridManager.settings.DeleteSelectedOrigin();
                        }
                    }
                }
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var origin = GridManager.settings.origin;
                        origin = new Vector3(
                            Mathf.Round(origin.x * 100000) / 100000f,
                            Mathf.Round(origin.y * 100000) / 100000f,
                            Mathf.Round(origin.z * 100000) / 100000f);
                        GridManager.settings.origin = UnityEditor.EditorGUILayout.Vector3Field("Origin position",
                            origin);
                        if (check.changed) UnityEditor.SceneView.RepaintAll();
                    }

                    using (new UnityEditor.EditorGUI.DisabledGroupScope(_activeGameObject == null))
                    {
                        if (GUILayout.Button("Set the origin to the active gameobject position"))
                        {
                            GridManager.settings.origin = _activeGameObject.transform.position;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var euler = GridManager.settings.rotation.eulerAngles;
                        euler = new Vector3(Mathf.RoundToInt(euler.x * 100000) / 100000f,
                            Mathf.RoundToInt(euler.y * 100000) / 100000f,
                            Mathf.RoundToInt(euler.z * 100000) / 100000f);
                        GridManager.settings.rotation
                            = Quaternion.Euler(UnityEditor.EditorGUILayout.Vector3Field("Rotation", euler));
                        if (check.changed) UnityEditor.SceneView.RepaintAll();
                    }
                    using (new UnityEditor.EditorGUI.DisabledGroupScope(_activeGameObject == null))
                    {
                        if (GUILayout.Button("Set the rotation to the active gameobject rotation"))
                        {
                            GridManager.settings.rotation = _activeGameObject.transform.rotation;
                            UnityEditor.SceneView.RepaintAll();
                        }
                    }
                }
            }
            if (!GridManager.settings.radialGridEnabled)
            {
                using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        GridManager.settings.majorLinesGap
                            = UnityEditor.EditorGUILayout.Vector3IntField("Major lines every Nth grid line",
                            GridManager.settings.majorLinesGap);
                        if (check.changed) UnityEditor.SceneView.RepaintAll();
                    }
                }
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var idx = GridManager.settings.radialGridEnabled ? 1 : 0;
                    idx = UnityEditor.EditorGUILayout.Popup("Grid type", idx, _gridTypeOptions);
                    if (check.changed)
                    {
                        GridManager.settings.radialGridEnabled = idx == 0 ? false : true;
                        PWBToolbar.RepaintWindow();
                    }
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    GridManager.settings.lockedGrid = UnityEditor.EditorGUILayout.ToggleLeft("Lock the grid origin in place",
                        GridManager.settings.lockedGrid);
                    if (check.changed) PWBToolbar.RepaintWindow();
                }
                using (new UnityEditor.EditorGUI.DisabledGroupScope(!GridManager.settings.lockedGrid))
                {
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var showPositionHandle = UnityEditor.EditorGUILayout.ToggleLeft("Show position handle",
                            GridManager.settings.showPositionHandle);
                        if (check.changed) GridManager.settings.showPositionHandle = showPositionHandle;
                    }
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var showRotationHandle = UnityEditor.EditorGUILayout.ToggleLeft("Show rotation handle",
                            GridManager.settings.showRotationHandle);
                        if (check.changed) GridManager.settings.showRotationHandle = showRotationHandle;
                    }
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var showScaleHandle = UnityEditor.EditorGUILayout.ToggleLeft("Show spacing handle",
                            GridManager.settings.showScaleHandle);
                        if (check.changed) GridManager.settings.showScaleHandle = showScaleHandle;
                    }
                }
            }
        }
        private void OnSelectionChange() => _activeGameObject = UnityEditor.Selection.activeGameObject;
    }
}
#pragma warning restore UDR0001
