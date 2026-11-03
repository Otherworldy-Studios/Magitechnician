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
    public static partial class PWBIO
    {
        private class PaintStrokeItem
        {
            public readonly GameObject prefab = null;
            public readonly string guid = string.Empty;
            public readonly Vector3 position = Vector3.zero;
            public readonly Quaternion rotation = Quaternion.identity;
            public readonly Vector3 scale = Vector3.one;
            public readonly int layer = 0;
            public readonly bool flipX = false;
            public readonly bool flipY = false;
            public readonly int index = 0;
            private Transform _parent = null;
            private string _persistentParentId = string.Empty;


            private Transform _surface = null;
            public Transform parent { get => _parent; set => _parent = value; }
            public string persistentParentId { get => _persistentParentId; set => _persistentParentId = value; }
            public Transform surface { get => _surface; set => _surface = value; }

            public PaintStrokeItem(GameObject prefab, string guid, Vector3 position, Quaternion rotation,
                Vector3 scale, int layer, Transform parent, Transform surface, bool flipX, bool flipY, int index = -1)
            {
                this.prefab = prefab;
                this.guid = guid;
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
                this.layer = layer;
                this.flipX = flipX;
                this.flipY = flipY;
                this.index = index;
                _parent = parent;
                _surface = surface;
            }
        }

        private static System.Collections.Generic.List<PaintStrokeItem> _paintStroke
            = new System.Collections.Generic.List<PaintStrokeItem>();

        public static bool painting { get; set; }

        private const string PAINT_CMD = "Paint";

        private static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<(GameObject, int)>>
            Paint(IPaintToolSettings settings, string commandName = PAINT_CMD,
            bool addTempCollider = true, bool persistent = false, string toolObjectId = "")
        {
            painting = true;
            var paintedObjects = new System.Collections.Generic.Dictionary<string,
                System.Collections.Generic.List<(GameObject, int)>>();
            if (_paintStroke.Count == 0)
            {
                painting = false;
                if (BrushstrokeManager.brushstroke.Length == 0) BrushstrokeManager.UpdateBrushstroke();
                return paintedObjects;
            }

            var keepSourceParent = (settings is MirrorSettings mirrorS && mirrorS.sameParentAsSource)
                || (settings is ExtrudeSettings extrudeS && extrudeS.sameParentAsSource);

            foreach (var item in _paintStroke)
            {
                if (item.prefab == null) continue;
                var persistentParentId = persistent ? item.persistentParentId : toolObjectId;
                var type = UnityEditor.PrefabUtility.GetPrefabAssetType(item.prefab);
                GameObject obj = type == UnityEditor.PrefabAssetType.NotAPrefab ? GameObject.Instantiate(item.prefab)
                    : (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab
                    (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(item.prefab)
                    ? item.prefab : UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(item.prefab));
                if (PWBCore.staticData.addEnumerationToName)
                    obj.name = obj.name + "_" + PWBCore.staticData.GetPrefabCount(item.guid);
                if (settings.overwritePrefabLayer) obj.layer = settings.layer;
                obj.transform.SetPositionAndRotation(item.position, item.rotation);
                obj.transform.localScale = item.scale;
                var root = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                if (!keepSourceParent)
                    item.parent = GetParent(settings, item.prefab.name,
                        true, item.surface, persistentParentId);
                if (addTempCollider) PWBCore.AddTempCollider(obj);
                if (!paintedObjects.ContainsKey(persistentParentId))
                    paintedObjects.Add(persistentParentId, new System.Collections.Generic.List<(GameObject, int)>());
                paintedObjects[persistentParentId].Add((obj, item.index));
                var spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();

                foreach (var spriteRenderer in spriteRenderers)
                {
                    var flipX = spriteRenderer.flipX;
                    var flipY = spriteRenderer.flipY;
                    if (item.flipX) flipX = !flipX;
                    if (item.flipY) flipY = !flipY;
                    spriteRenderer.flipX = flipX;
                    spriteRenderer.flipY = flipY;
                    var center = BoundsUtils.GetBoundsRecursive(spriteRenderer.transform,
                        spriteRenderer.transform.rotation).center;
                    var pivotToCenter = center - spriteRenderer.transform.position;
                    var delta = Vector3.zero;
                    if (item.flipX) delta.x = pivotToCenter.x * -2;
                    if (item.flipY) delta.y = pivotToCenter.y * -2;
                    spriteRenderer.transform.position += delta;
                }
                AddObjectToOctree(obj);
                UnityEditor.Undo.RegisterCreatedObjectUndo(obj, commandName);

                if (isInPrefabMode)
                {
                    if (item.parent == null) UnityEditor.Undo.SetTransformParent(obj.transform,
                            prefabStage.prefabContentsRoot.transform, commandName);
                    else UnityEditor.Undo.SetTransformParent(obj.transform, item.parent, commandName);
                }
                else if (root != null) UnityEditor.Undo.SetTransformParent(root.transform, item.parent, commandName);
                else UnityEditor.Undo.SetTransformParent(obj.transform, item.parent, commandName);
            }
            if (_paintStroke.Count > 0) BrushstrokeManager.UpdateBrushstroke();
            _paintStroke.Clear();
            return paintedObjects;
        }
    }
}
#pragma warning restore UDR0001
