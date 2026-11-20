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
        private const float MIN_OCTREE_NODE_SIZE = 0.5f;

        private static BoundsOctree _boundsOctree = new BoundsOctree(initialWorldSize: 10,
            initialWorldPos: Vector3.zero, MIN_OCTREE_NODE_SIZE, MIN_OCTREE_NODE_SIZE);

        private static bool _octreeIsDirty = false;

        public static void SetOctreeDirty() => _octreeIsDirty = true;

        public static BoundsOctree boundsOctree
        {
            get
            {
                if (_boundsOctree == null || _octreeIsDirty) UpdateOctree();
                return _boundsOctree;
            }
        }

        public static void UpdateOctree(GameObject[] allObjects)
        {
            if (tool == ToolController.Tool.ERASER || tool == ToolController.Tool.REPLACER
                || tool == ToolController.Tool.CIRCLE_SELECT)
            {
                var allPrefabsPaths = new System.Collections.Generic.HashSet<string>();
                bool AddPrefabPath(MultibrushItemSettings item)
                {
                    if (item.prefab == null) return false;
                    var path = UnityEditor.AssetDatabase.GetAssetPath(item.prefab);
                    allPrefabsPaths.Add(path);
                    return true;
                }
                ISelectionBrushTool SelectionBrushSettings = EraserManager.settings;
                if (tool == ToolController.Tool.REPLACER) SelectionBrushSettings = ReplacerManager.settings;
                else if (tool == ToolController.Tool.CIRCLE_SELECT) SelectionBrushSettings = CircleSelectManager.settings;
                if (SelectionBrushSettings.command == ISelectionBrushTool.Command.SELECT_PALETTE_PREFABS)
                    foreach (var brush in PaletteManager.selectedPalette.brushes)
                        foreach (var item in brush.items) AddPrefabPath(item);
                else if (PaletteManager.selectedBrush != null
                    && SelectionBrushSettings.command == ISelectionBrushTool.Command.SELECT_BRUSH_PREFABS)
                    foreach (var item in PaletteManager.selectedBrush.items) AddPrefabPath(item);
                SelectionManager.UpdateSelection();
                bool modifyAll = SelectionBrushSettings.command == ISelectionBrushTool.Command.SELECT_ALL;
                bool modifyAllButSelected = false;
                if (tool == ToolController.Tool.ERASER || tool == ToolController.Tool.REPLACER)
                {
                    IModifierTool modifierSettings = tool == ToolController.Tool.ERASER
                        ? EraserManager.settings as IModifierTool : ReplacerManager.settings;
                    modifyAllButSelected = modifierSettings.modifyAllButSelected;
                }
                foreach (var obj in allObjects)
                {
                    if (!obj.activeInHierarchy) continue;
                    if (!modifyAll && !UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(obj)) continue;
                    var prefabPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
                    bool isBrush = allPrefabsPaths.Contains(prefabPath);
                    if (!isBrush && !modifyAll) continue;
                    if (modifyAllButSelected && SelectionManager.selection.Contains(obj)) continue;
                    AddObjectToOctree(obj);
                }
            }
            else
            {
                foreach (var obj in allObjects)
                {
                    if (!obj.activeInHierarchy) continue;
                    AddObjectToOctree(obj);
                }
            }
        }

        public static void UpdateOctree()
        {
            if (_boundsOctree != null && _boundsOctree.Count > 0 && !_octreeIsDirty) return;

            _octreeIsDirty = false;
            _boundsOctree = new BoundsOctree(initialWorldSize: 10,
           initialWorldPos: Vector3.zero, MIN_OCTREE_NODE_SIZE, MIN_OCTREE_NODE_SIZE);
            GameObject[] allObjects;
            if (isInPrefabMode)
            {
                var transforms = prefabStage.prefabContentsRoot.GetComponentsInChildren<Transform>();
                allObjects = transforms.Select(t => t.gameObject).ToArray();
                UpdateOctree(allObjects);
            }
            else
            {
#if UNITY_6000_4_OR_NEWER
                allObjects = Object.FindObjectsByType<GameObject>();
#elif UNITY_2022_2_OR_NEWER
                allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
                allObjects = Object.FindObjectsOfType<GameObject>();
#endif
                var allObjectsRoots = new System.Collections.Generic.HashSet<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj == null) continue;
                    var outermost = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                    if (outermost == null)
                    {
                        var components = obj.GetComponents<Component>();
                        if (components.Length <= 1) continue;
                        var colliders = obj.GetComponents<Collider>();
                        var renderers = obj.GetComponents<Renderer>();
                        var filters = obj.GetComponents<MeshFilter>();
                        if (colliders.Length == 0 && renderers.Length == 0 && filters.Length == 0) continue;
                        allObjectsRoots.Add(obj);
                    }
                    else allObjectsRoots.Add(outermost);
                }
                UpdateOctree(allObjectsRoots.ToArray());
            }
        }

        public static void AddObjectToOctree(GameObject obj)
        {
            if (_boundsOctree == null) _boundsOctree = new BoundsOctree(initialWorldSize: 10,
           initialWorldPos: Vector3.zero, MIN_OCTREE_NODE_SIZE, MIN_OCTREE_NODE_SIZE);
            Bounds bounds;
            if (ToolController.current == ToolController.Tool.FLOOR)
                bounds = BoundsUtils.GetBoundsRecursive(obj.transform, GridManager.settings.rotation);
            else bounds = BoundsUtils.GetBoundsRecursive(obj.transform);
            _boundsOctree.Add(obj, bounds);
        }
    }
}
#pragma warning restore UDR0001
