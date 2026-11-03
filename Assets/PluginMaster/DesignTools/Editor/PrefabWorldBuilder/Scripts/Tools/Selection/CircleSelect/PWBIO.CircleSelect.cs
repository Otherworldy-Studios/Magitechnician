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

        private static Material _transparentBlueMaterial = null;
        private static Vector2 _lastCircleSelectMousePos;
        private static System.Collections.Generic.HashSet<GameObject> _toSelect
            = new System.Collections.Generic.HashSet<GameObject>();

        public static Material transparentBlueMaterial
        {
            get
            {
                if (_transparentBlueMaterial == null)
                    _transparentBlueMaterial = new Material(Shader.Find("PluginMaster/TransparentBlue"));
                return _transparentBlueMaterial;
            }
        }

        private static void CircleSelectDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            CircleSelectMouseEvents();
            var mousePos = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos);

            var center = mouseRay.GetPoint(_lastHitDistance);
            if (PWBToolRaycast(mouseRay, out RaycastHit mouseHit, out GameObject collider,
                float.MaxValue, -1, paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true,
                ignoreSceneColliders: true, createTempColliders: true))
            {
                _lastHitDistance = mouseHit.distance;
                center = mouseHit.point;
            }
            DrawCircleTool(center, sceneView.camera, new Color(0.455f, 0.596f, 0.8f, 1f),
                CircleSelectManager.settings.radius);

            if (_lastCircleSelectMousePos != mousePos)
            {
                _lastCircleSelectMousePos = mousePos;
                _selectMeshCache.Clear();
                GetCircleToolTargets(mouseRay, sceneView.camera, CircleSelectManager.settings,
                    CircleSelectManager.settings.radius, _toSelect);
            }

            DrawObjectsToSelect(sceneView.camera);
        }

        private static readonly System.Collections.Generic.Dictionary<GameObject,
            (Mesh mesh, Matrix4x4 matrix)[]> _selectMeshCache
            = new System.Collections.Generic.Dictionary<GameObject, (Mesh, Matrix4x4)[]>();

        private static void DrawObjectsToSelect(Camera camera)
        {
            foreach (var obj in _toSelect)
            {
                if (obj == null) continue;

                if (!_selectMeshCache.TryGetValue(obj, out var meshesData))
                {
                    var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
                    var meshesDataSet = new System.Collections.Generic.HashSet<(Mesh, Matrix4x4)>
                        (meshFilters.Select(f => (f.sharedMesh, f.transform.localToWorldMatrix)));

                    var skinnedRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                    if (skinnedRenderers.Length > 0)
                    {
                        meshesDataSet.UnionWith(skinnedRenderers.Where(r => r != null && r.sharedMesh != null)
                            .Select(r => (r.sharedMesh, r.transform.localToWorldMatrix)));
                    }
                    meshesData = meshesDataSet.ToArray();
                    _selectMeshCache[obj] = meshesData;
                }

                foreach (var item in meshesData)
                {
                    for (int subMeshIdx = 0; subMeshIdx < item.mesh.subMeshCount; ++subMeshIdx)
                        EnqueueRawPreviewDraw(item.mesh, item.matrix, transparentBlueMaterial, subMeshIdx, layer: 0);
                }
            }
        }

        private static void CircleSelectMouseEvents()
        {
            if (Event.current.button == 0 && !Event.current.alt
                && (Event.current.type == EventType.MouseDown
                || (Event.current.type == EventType.MouseDrag && !Event.current.control)))
            {
#if UNITY_2021_1_OR_NEWER
                using (UnityEngine.Pool.HashSetPool<Object>
                    .Get(out System.Collections.Generic.HashSet<Object> selectedObjectsSet))
#else
                var selectedObjectsSet = new System.Collections.Generic.HashSet<Object>();
#endif
                {
                    if (Event.current.shift || Event.current.control)
                    {
                        selectedObjectsSet.UnionWith(UnityEditor.Selection.objects);
                        if (Event.current.control)
                        {
                            selectedObjectsSet.ExceptWith(_toSelect);
                            var selectedGameObjects
                                = UnityEditor.Selection.GetFiltered<GameObject>(UnityEditor.SelectionMode.Unfiltered);
                            _toSelect.ExceptWith(selectedGameObjects);
                        }
                    }
                    selectedObjectsSet.UnionWith(_toSelect);
                    UnityEditor.Selection.objects = selectedObjectsSet.ToArray();
                    Event.current.Use();
                }
            }
        }
    }
}
#pragma warning restore UDR0001
