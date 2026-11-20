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
    public static partial class BrushstrokeManager
    {

        private static System.Collections.Generic.Dictionary<Transform, BrushstrokeItem> _replacerDictionary
            = new System.Collections.Generic.Dictionary<Transform, BrushstrokeItem>();
        private static System.Collections.Generic.Dictionary<BrushstrokeItem, Transform> _replacerDictionary2
           = new System.Collections.Generic.Dictionary<BrushstrokeItem, Transform>();

        public static void UpdateReplacerBrushstroke(bool clearDictionary,
            System.Collections.Generic.IEnumerable<GameObject> targets)
        {
            _brushstroke.Clear();
            if (clearDictionary) ClearReplacerDictionary();
            var toolSettings = ReplacerManager.settings;
            bool GetStrokeItem(Transform target, int itemIdx, out BrushstrokeItem item)
            {
                item = new BrushstrokeItem();
                if (itemIdx == -1) return false;
                if (PaletteManager.selectedBrush.frequencyMode == PluginMaster.MultibrushSettings.FrequencyMode.PATTERN
                    && itemIdx == -2)
                {
                    if (PaletteManager.selectedBrush.patternMachine != null)
                        PaletteManager.selectedBrush.patternMachine.Reset();
                    else return false;
                }
                var multiBrushSettings = PaletteManager.selectedBrush;
                BrushSettings brushSettings = multiBrushSettings.items[itemIdx];
                if (toolSettings.overwriteBrushProperties) brushSettings = toolSettings.brushSettings;
                var flipX = brushSettings.GetFlipX();
                var flipY = brushSettings.GetFlipY();

                var multiBrushItemSettings = PaletteManager.selectedBrush.items[itemIdx];
                var prefab = multiBrushItemSettings.prefab;
                if (prefab == null) return false;
                var itemRotation = target.rotation;
                var targetBounds = BoundsUtils.GetBoundsRecursive(target, target.rotation);
                var strokeRotation = Quaternion.identity;
                var scaleMult = Vector3.one;

                if (toolSettings.overwriteBrushProperties)
                {
                    var toolBrushSettings = toolSettings.brushSettings;
                    var additonalAngle = toolBrushSettings.addRandomRotation
                        ? toolBrushSettings.randomEulerOffset.randomVector : toolBrushSettings.eulerOffset;
                    strokeRotation *= Quaternion.Euler(additonalAngle);
                    scaleMult = toolBrushSettings.randomScaleMultiplier
                        ? toolBrushSettings.randomScaleMultiplierRange.randomVector : toolBrushSettings.scaleMultiplier;
                }
                var inverseStrokeRotation = Quaternion.Inverse(strokeRotation);
                itemRotation *= strokeRotation;
                var itemBounds = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation * strokeRotation);

                if (toolSettings.keepTargetSize)
                {
                    var targetSize = targetBounds.size;
                    var itemSize = itemBounds.size;

                    if (toolSettings.maintainProportions)
                    {
                        var targetMagnitude = Mathf.Max(targetSize.x, targetSize.y, targetSize.z);
                        var itemMagnitude = Mathf.Max(itemSize.x, itemSize.y, itemSize.z);
                        scaleMult = inverseStrokeRotation * (Vector3.one * (targetMagnitude / itemMagnitude));
                    }
                    else scaleMult = inverseStrokeRotation
                            * new Vector3(targetSize.x / itemSize.x, targetSize.y / itemSize.y, targetSize.z / itemSize.z);
                    scaleMult = new Vector3(Mathf.Abs(scaleMult.x), Mathf.Abs(scaleMult.y), Mathf.Abs(scaleMult.z));
                }
                var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                var itemPosition = targetBounds.center;

                Transform replaceSurface = null;
                if (toolSettings.positionMode == ReplacerSettings.PositionMode.ON_SURFACE)
                {
                    var TRS = Matrix4x4.TRS(itemPosition, itemRotation, itemScale);
                    var bottomDistanceToSurfce = PWBIO.GetBottomDistanceToSurface(multiBrushItemSettings.bottomVertices,
                        TRS, Mathf.Abs(multiBrushItemSettings.bottomMagnitude), paintOnPalettePrefabs: true,
                        castOnMeshesWithoutCollider: true, ignoreSceneColliders: true, out replaceSurface,
                        new System.Collections.Generic.HashSet<GameObject> { target.gameObject });
                    itemPosition += itemRotation * new Vector3(0f, -bottomDistanceToSurfce, 0f);
                }
                else
                {
                    if (toolSettings.positionMode == ReplacerSettings.PositionMode.PIVOT)
                        itemPosition = target.position;
                    itemPosition -= itemRotation * Vector3.Scale(itemBounds.center - prefab.transform.position, scaleMult);
                }

                item = new BrushstrokeItem(itemIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                    multiBrushItemSettings, itemPosition, itemRotation.eulerAngles,
                    scaleMult, flipX, flipY, surfaceDistance: 0);
                return true;
            }
            void AddItem(Transform target)
            {
                var nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                if (GetStrokeItem(target, nextIdx, out BrushstrokeItem strokeItem))
                {
                    _brushstroke.Add(strokeItem);
                    _replacerDictionary.Add(target, strokeItem);
                    _replacerDictionary2.Add(strokeItem, target);
                }
            }
            foreach (var sceneObj in targets)
            {
                var target = sceneObj.transform;
                BrushstrokeItem item;
                if (_replacerDictionary.ContainsKey(target))
                {
                    item = _replacerDictionary[target];
                    if (GetStrokeItem(target, item.index, out BrushstrokeItem strokeItem))
                    {
                        if (item == strokeItem) _brushstroke.Add(item);
                        else
                        {
                            _replacerDictionary.Remove(target);
                            _replacerDictionary2.Remove(item);
                            AddItem(target);
                        }
                    }
                }
                else AddItem(target);
            }

        }
        public static void ClearReplacerDictionary()
        {
            _replacerDictionary.Clear();
            _replacerDictionary2.Clear();
        }
        public static Transform GetReplacerTargetFromStrokeItem(BrushstrokeItem item) => _replacerDictionary2[item];
    }
}
#pragma warning restore UDR0001
