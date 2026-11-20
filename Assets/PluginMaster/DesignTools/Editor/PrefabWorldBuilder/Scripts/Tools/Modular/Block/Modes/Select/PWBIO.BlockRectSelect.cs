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
    public static partial class PWBIO
    {

        private static bool _rectSelectDragging = false;
        private static Vector2 _rectSelectStart = Vector2.zero;
        private static Vector2 _rectSelectEnd = Vector2.zero;

        private static void PreviewBlockRectSelect(Camera camera,
            out Vector3 mousePos3D, out Vector3 localMousePos3D)
        {
            mousePos3D = Vector3.zero;
            localMousePos3D = Vector3.zero;

            if (_rectSelectDragging)
            {
                DrawSelectionRect(_rectSelectStart, _rectSelectEnd);
                DrawBlockRectSelectPreview(camera, _rectSelectStart, _rectSelectEnd);
            }
        }

        private static Rect GetScreenRect(Vector2 start, Vector2 end)
        {
            var topLeft = new Vector2(Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y));
            var size = new Vector2(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
            return new Rect(topLeft, size);
        }

        private static void DrawSelectionRect(Vector2 start, Vector2 end)
        {
            var rect = GetScreenRect(start, end);
            UnityEditor.Handles.BeginGUI();
            var fillColor = new Color(0.3f, 0.5f, 0.8f, 0.15f);
            var borderColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
            UnityEditor.EditorGUI.DrawRect(rect, fillColor);
            var top = new Rect(rect.x, rect.y, rect.width, 1);
            var bottom = new Rect(rect.x, rect.yMax - 1, rect.width, 1);
            var left = new Rect(rect.x, rect.y, 1, rect.height);
            var right = new Rect(rect.xMax - 1, rect.y, 1, rect.height);
            UnityEditor.EditorGUI.DrawRect(top, borderColor);
            UnityEditor.EditorGUI.DrawRect(bottom, borderColor);
            UnityEditor.EditorGUI.DrawRect(left, borderColor);
            UnityEditor.EditorGUI.DrawRect(right, borderColor);
            UnityEditor.Handles.EndGUI();
            UnityEditor.HandleUtility.Repaint();
        }

        private static System.Collections.Generic.List<GameObject> GetBlockObjectsInScreenRect(
            Camera camera, Vector2 start, Vector2 end)
        {
            var result = new System.Collections.Generic.List<GameObject>();
            var rect = GetScreenRect(start, end);
            var allObjects = boundsOctree.GetWithinFrustum(camera);
            var cellSize = GridManager.settings.step;
            var cellRotation = GridManager.settings.rotation;
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                if (!obj.activeInHierarchy) continue;
                var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                var snapped = SnapPositionToBlockCellCenter(objCenter);
                if (Vector3.Distance(objCenter, snapped) > Mathf.Min(cellSize.x, cellSize.y, cellSize.z) * 0.5f)
                    continue;
                var screenPoint = UnityEditor.HandleUtility.WorldToGUIPoint(objCenter);
                if (rect.Contains(screenPoint))
                    result.Add(obj);
            }
            return result;
        }

        private static void DrawBlockRectSelectPreview(Camera camera, Vector2 start, Vector2 end)
        {
            var objects = GetBlockObjectsInScreenRect(camera, start, end);
            var cellSize = BlockManager.settings.moduleSize;
            var cellRotation = GridManager.settings.rotation;
            foreach (var obj in objects)
            {
                var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                var snapped = SnapPositionToBlockCellCenter(objCenter);
                var TRS = Matrix4x4.TRS(snapped, cellRotation, cellSize);
                Graphics.DrawMesh(cubeMesh, TRS, snapBoxMaterial, 0, camera);
            }
        }

        private static void SelectBlocksInRect(Camera camera, Vector2 start, Vector2 end, bool additive)
        {
            var objects = GetBlockObjectsInScreenRect(camera, start, end);
            if (objects.Count == 0 && !additive)
            {
                UnityEditor.Selection.objects = new Object[0];
                return;
            }
            if (additive)
            {
                var current = UnityEditor.Selection.objects.ToList();
                foreach (var obj in objects)
                {
                    if (!current.Contains(obj))
                        current.Add(obj);
                }
                UnityEditor.Selection.objects = current.ToArray();
            }
            else
            {
                UnityEditor.Selection.objects = objects.ToArray();
            }
        }

        private static void BlockRectSelectInput(Camera camera, Vector3 mousePos3D)
        {
            var evt = Event.current;
            var mousePos2D = evt.mousePosition;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                _rectSelectDragging = true;
                _rectSelectStart = mousePos2D;
                _rectSelectEnd = mousePos2D;
                evt.Use();
            }
            else if (_rectSelectDragging && evt.type == EventType.MouseDrag && evt.button == 0)
            {
                _rectSelectEnd = mousePos2D;
                evt.Use();
            }
            else if (_rectSelectDragging && evt.type == EventType.MouseUp && evt.button == 0)
            {
                _rectSelectEnd = mousePos2D;
                _rectSelectDragging = false;
                SelectBlocksInRect(camera, _rectSelectStart, _rectSelectEnd, evt.shift);
                evt.Use();
            }
        }
    }
}
#pragma warning restore UDR0001
