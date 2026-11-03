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
using System.Linq;

namespace PluginMaster
{
    public class ItemPropertiesWindow : UnityEditor.EditorWindow
    {
        protected IPersistentData _data = null;
        private string _itemName = string.Empty;

        public static void ShowWindow(IPersistentData data, Vector2 mousePosition)
        {
            var window = GetWindow<ItemPropertiesWindow>(true, "Item properties");
            window.Initialize(data, mousePosition);
        }
        protected virtual void Initialize(IPersistentData data, Vector2 mousePosition)
        {
            _data = data;
            _itemName = data.name;
            position = new Rect(mousePosition.x + 50, mousePosition.y + 50, 250, 50);
        }
        private void OnGUI()
        {
            if (ToolController.current == ToolController.Tool.NONE || _data == null) Close();
            UnityEditor.EditorGUIUtility.labelWidth = 50;
            UnityEditor.EditorGUIUtility.fieldWidth = 100;
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    _itemName = UnityEditor.EditorGUILayout.TextField("Name", _itemName);
                }
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var renameParentObject = UnityEditor.EditorGUILayout.ToggleLeft("Rename parent object",
                        PWBCore.staticData.ranameItemParent);
                    if (check.changed) PWBCore.staticData.ranameItemParent = renameParentObject;
                }
            }
            GUILayout.Space(10);
            ToolPropertiesGUI();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Apply", GUILayout.Width(50))) Apply();
                if (GUILayout.Button("Cancel", GUILayout.Width(50))) Close();
            }
            GUILayout.Space(10);
        }

        protected virtual void ToolPropertiesGUI() { }
        protected virtual void Apply()
        {
            _data.Rename(_itemName, PWBCore.staticData.ranameItemParent);
            PWBItemsWindow.RepainWindow();
            Close();
            UnityEditor.SceneView.RepaintAll();
        }

        public static void ShowItemProperties(IPersistentData data, Vector2 mousePosition)
        {
            if (ToolController.current == ToolController.Tool.LINE)
                LinePropertiesWindow.ShowWindow(data, mousePosition);
            else ShowWindow(data, mousePosition);
        }
    }
    public class LinePropertiesWindow : ItemPropertiesWindow
    {
        private LineData _lineData = null;
        private Vector2 _pointsScrollPosition = Vector2.zero;
        private GUISkin _skin = null;
        private GUIStyle _itemRowStyle = null;
        private GUIContent _deleteIcon = null;
        private GUIContent _deleteIconLight = null;
        private GUIStyle _itemBtnStyle = null;
        private System.Collections.Generic.HashSet<int> _pointsToDelete = new System.Collections.Generic.HashSet<int>();
        private System.Collections.Generic.Dictionary<int, Vector3> _positions
            = new System.Collections.Generic.Dictionary<int, Vector3>();
        private System.Collections.Generic.Dictionary<LinePoint, bool> _curvedSegments
            = new System.Collections.Generic.Dictionary<LinePoint, bool>();
        public static new void ShowWindow(IPersistentData data, Vector2 mousePosition)
        {
            var window = GetWindow<LinePropertiesWindow>(true, "Item properties");
            window.Initialize(data, mousePosition);
        }

        protected override void Initialize(IPersistentData data, Vector2 mousePosition)
        {
            base.Initialize(data, mousePosition);
            _lineData = _data as LineData;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        private void OnEnable()
        {
            UnityEditor.Undo.undoRedoPerformed -= Repaint;
            UnityEditor.Undo.undoRedoPerformed += Repaint;
            _skin = Resources.Load<GUISkin>("PWBSkin");
            if (_skin == null) return;
            _itemRowStyle = _skin.GetStyle("ItemRow");
            _deleteIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Delete2"), "Delete point");
            _deleteIconLight = new GUIContent(Resources.Load<Texture2D>("Sprites/LightTheme/Delete2"));
            _deleteIconLight.tooltip = _deleteIcon.tooltip;
            _itemBtnStyle = _skin.GetStyle("EyeButton");

        }
        private void OnDisable()
        {
            UnityEditor.Undo.undoRedoPerformed -= Repaint;
        }
        private GUIContent deleteIcon => UnityEditor.EditorGUIUtility.isProSkin ? _deleteIcon : _deleteIconLight;
        protected override void ToolPropertiesGUI()
        {
            if (_skin == null)
            {
                Close();
                return;
            }
            if (_data == null)
            {
                Close();
                return;
            }

            void Header()
            {
                using (new GUILayout.HorizontalScope(_itemRowStyle))
                {
                    UnityEditor.EditorGUILayout.LabelField("Idx", GUILayout.Width(20));
                    GUILayout.Space(80);
                    UnityEditor.EditorGUILayout.LabelField("Position", GUILayout.Width(120));
                    UnityEditor.EditorGUILayout.LabelField("Prev Seg Curved", GUILayout.Width(100));
                    GUILayout.FlexibleSpace();
                }
            }
            void Row(int idx, LinePoint point)
            {
                if (_pointsToDelete.Contains(idx)) return;
                using (new GUILayout.HorizontalScope(_itemRowStyle))
                {
                    UnityEditor.EditorGUILayout.LabelField(idx.ToString("D2"), GUILayout.Width(20));
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var position = _positions.ContainsKey(idx) ? _positions[idx] : point.position;
                        position = UnityEditor.EditorGUILayout.Vector3Field(string.Empty, position, GUILayout.Width(200));
                        if (check.changed)
                        {
                            if (_positions.ContainsKey(idx)) _positions[idx] = position;
                            else _positions.Add(idx, position);
                        }
                    }
                    GUILayout.Space(45);
                    using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                    {
                        var isCurved = _curvedSegments.ContainsKey(point) ? _curvedSegments[point]
                            : point.type == LineSegment.SegmentType.CURVE;
                        isCurved = UnityEditor.EditorGUILayout.Toggle(isCurved, GUILayout.Width(55));
                        if (check.changed)
                        {
                            if (_curvedSegments.ContainsKey(point)) _curvedSegments[point] = isCurved;
                            else _curvedSegments.Add(point, isCurved);
                        }
                    }
                    GUILayout.Space(10);
                    if (GUILayout.Button(deleteIcon, _itemBtnStyle)) _pointsToDelete.Add(idx);
                    GUILayout.FlexibleSpace();
                }
            }
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var scrollView = new UnityEditor.EditorGUILayout.ScrollViewScope(_pointsScrollPosition,
                alwaysShowHorizontal: false, alwaysShowVertical: false,
                GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, background: GUIStyle.none))
                {
                    _pointsScrollPosition = scrollView.scrollPosition;
                    var points = _lineData.controlPoints;
                    Header();
                    for (int i = 0; i < points.Length; i++) Row(i, points[i]);
                }
            }

            minSize = new Vector2(400, Mathf.Min(_lineData.pointsCount, 10) * 30 + 100);
            GUILayout.Space(10);
        }
        protected override void Apply()
        {
            base.Apply();
            if (_positions.Count > 0)
            {
                foreach (var p in _positions)
                    _lineData.SetPoint(p.Key, p.Value, registerUndo: true, selectAll: false, moveSelection: false);
                PWBIO.ApplyPersistentLineAndReset(_lineData);
            }
            if (_curvedSegments.Count > 0)
            {
                foreach (var p in _curvedSegments)
                    p.Key.type = (p.Value) ? LineSegment.SegmentType.CURVE : LineSegment.SegmentType.STRAIGHT;
                PWBIO.ApplyPersistentLineAndReset(_lineData);
            }
            if (_pointsToDelete.Count > 0) PWBIO.DeleteLinePoints(_lineData, _pointsToDelete.ToArray(), ToolController.editMode);
        }
    }
}
#pragma warning restore UDR0001
