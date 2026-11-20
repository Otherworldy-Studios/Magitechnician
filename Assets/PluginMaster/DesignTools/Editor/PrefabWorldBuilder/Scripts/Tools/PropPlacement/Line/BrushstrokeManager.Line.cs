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
        
        public static float _minLineSpacing = float.MaxValue;
        public static float GetLineSpacing(int itemIdx, LineSettings settings, Vector3 scaleMult)
        {
            float spacing = 0;
            if (itemIdx >= 0) spacing = settings.spacing;

            if (settings.spacingType == LineSettings.SpacingType.BOUNDS && itemIdx >= 0)
            {
                var item = PaletteManager.selectedBrush.items[itemIdx];
                if (item.prefab == null) return spacing;
                var bounds = BoundsUtils.GetBoundsRecursive(item.prefab.transform);
                var size = Vector3.Scale(bounds.size, scaleMult);
                var axis = settings.axisOrientedAlongTheLine;
                var sceneView = UnityEditor.SceneView.currentDrawingSceneView ?? UnityEditor.SceneView.lastActiveSceneView;
                if (item.isAsset2D && sceneView != null && sceneView.in2DMode
                    && axis == AxesUtils.Axis.Z) axis = AxesUtils.Axis.Y;
                spacing = AxesUtils.GetAxisValue(size, axis);
                if (spacing <= 0.0001) spacing = 0.5f;
            }
            spacing += settings.gapSize;
            _minLineSpacing = Mathf.Min(spacing, _minLineSpacing);
            return spacing;
        }
        private static void UpdateLineBrushstroke(Vector3[] points, LineSettings settings)
        {
            _brushstroke.Clear();
            if (PaletteManager.selectedBrush == null) return;

            float lineLength = 0f;
            var lengthFromFirstPoint = new float[points.Length];
            var segmentLength = new float[points.Length];
            lengthFromFirstPoint[0] = 0f;
            for (int i = 1; i < points.Length; ++i)
            {
                segmentLength[i - 1] = (points[i] - points[i - 1]).magnitude;
                lineLength += segmentLength[i - 1];
                lengthFromFirstPoint[i] = lineLength;
            }

            float length = 0f;
            int segment = 0;
            if (PaletteManager.selectedBrush.patternMachine != null)
                PaletteManager.selectedBrush.patternMachine.Reset();
            var prefabSpacingDictionary = new System.Collections.Generic.Dictionary<(int, Vector3), float>();

            var brush = PaletteManager.selectedBrush;

            float Spacing(int itemIdx, Vector3 scale)
            {
                float spacing = 0;
                var item = brush.items[itemIdx];

                if (settings.spacingType == LineSettings.SpacingType.BOUNDS)
                {
                    var key = (itemIdx, scale);
                    if (item.randomScaleMultiplier) spacing = GetLineSpacing(itemIdx, settings, scale);
                    else if (prefabSpacingDictionary.ContainsKey(key)) spacing = prefabSpacingDictionary[key];
                    else
                    {
                        spacing = GetLineSpacing(itemIdx, settings, scale);
                        prefabSpacingDictionary.Add(key, spacing);
                    }
                }
                else spacing = GetLineSpacing(itemIdx, settings, scale);
                return spacing;
            }

            float endLenght = 0f;
            int[] endIndexes = null;
            if (brush.frequencyMode == MultibrushSettings.FrequencyMode.PATTERN)
            {
                endIndexes = brush.patternMachine.GetEndIndexes();
                foreach (var i in endIndexes)
                {
                    var idx = i - 1;
                    var item = brush.items[idx];
                    var scale = ScaleMultiplier(idx, LineManager.settings);
                    endLenght += Spacing(idx, scale);
                }
            }
            int currentEndIdx = 0;
            bool useEndIndexes = false;
            do
            {
                var nextIdx = brush.nextItemIndex;
                if (nextIdx < 0) break;
                if (useEndIndexes)
                {
                    if (currentEndIdx >= endIndexes.Length) break;
                    nextIdx = endIndexes[currentEndIdx] - 1;
                    ++currentEndIdx;
                    if (currentEndIdx == 1 && endIndexes.Length > 1)
                    {
                        while (endLenght > lineLength - length)
                        {
                            nextIdx = endIndexes[currentEndIdx] - 1;
                            var s = ScaleMultiplier(nextIdx, LineManager.settings);
                            endLenght -= Spacing(nextIdx, s);
                            ++currentEndIdx;
                            if (currentEndIdx >= endIndexes.Length) break;
                        }
                        if (currentEndIdx >= endIndexes.Length) break;
                    }
                }
                while (lengthFromFirstPoint[segment + 1] < length)
                {
                    ++segment;
                    if (segment >= points.Length - 1) break;
                }
                if (segment >= points.Length - 1) break;
                var segmentDirection = (points[segment + 1] - points[segment]).normalized;
                var distance = length - lengthFromFirstPoint[segment];
                var position = points[segment] + segmentDirection * distance;
                var scale = ScaleMultiplier(nextIdx, LineManager.settings);
                float spacing = Spacing(nextIdx, scale);

                var delta = Mathf.Max(spacing, _minLineSpacing);
                if (delta <= 0) break;
                spacing = Mathf.Max(spacing, _minLineSpacing);
                if (!useEndIndexes && brush.frequencyMode == MultibrushSettings.FrequencyMode.PATTERN
                    && endLenght > 0 && length + spacing > lineLength - endLenght && currentEndIdx == 0)
                {
                    useEndIndexes = true;
                    continue;
                }

                length += spacing;

                if (length > lineLength) break;
                AddBrushstrokeItem(nextIdx, PaletteManager.selectedBrush.GetPatternTokenIndex(), position,
                    angle: Vector3.zero, scale, settings);
            } while (length < lineLength);
        }
        public static void UpdateLineBrushstroke(Vector3[] pathPoints)
            => UpdateLineBrushstroke(pathPoints, LineManager.settings);
        private static float GetLineSpacing(Transform transform, LineSettings settings, Vector3 scale, bool useDictionary)
        {
            float spacing = settings.spacing;
            if (settings.spacingType == LineSettings.SpacingType.BOUNDS && transform != null)
            {
                var bounds = BoundsUtils.GetBoundsRecursive(transform, transform.rotation, ignoreDissabled: false,
                     BoundsUtils.ObjectProperty.BOUNDING_BOX, recursive: true, useDictionary);
                var size = Vector3.Scale(bounds.size, scale);
                var axis = settings.axisOrientedAlongTheLine;
                var sceneView = UnityEditor.SceneView.currentDrawingSceneView ?? UnityEditor.SceneView.lastActiveSceneView;
                if (Utils2D.Is2DAsset(transform.gameObject) && sceneView != null
                    && sceneView.in2DMode && axis == AxesUtils.Axis.Z)
                    axis = AxesUtils.Axis.Y;
                spacing = AxesUtils.GetAxisValue(size, axis);
            }
            spacing += settings.gapSize;
            _minLineSpacing = Mathf.Min(spacing, _minLineSpacing);
            return spacing;
        }

        public static void UpdatePersistentLineBrushstroke(Vector3[] pathPoints,
            LineSettings toolSettings, System.Collections.Generic.List<GameObject> lineObjects,
            out BrushstrokeObject[] objPositions, out Vector3[] strokePositions, out int firstNewObjectIdx)
        {
            _brushstroke.Clear();
            firstNewObjectIdx = 0;
            var objPositionsList = new System.Collections.Generic.List<BrushstrokeObject>();
            var strokePositionsList = new System.Collections.Generic.List<Vector3>();
            float lineLength = 0f;
            var lengthFromFirstPoint = new float[pathPoints.Length];
            var segmentLength = new float[pathPoints.Length];
            lengthFromFirstPoint[0] = 0f;
            for (int i = 1; i < pathPoints.Length; ++i)
            {
                segmentLength[i - 1] = (pathPoints[i] - pathPoints[i - 1]).magnitude;
                lineLength += segmentLength[i - 1];
                lengthFromFirstPoint[i] = lineLength;
            }

            float length = 0f;
            int segment = 0;
            if (PaletteManager.selectedBrush != null)
                if (PaletteManager.selectedBrush.patternMachine != null)
                    PaletteManager.selectedBrush.patternMachine.Reset();
            int objIdx = 0;
            var prefabSpacingDictionary = new System.Collections.Generic.Dictionary<(int, Vector3), float>();

            var brush = PaletteManager.selectedBrush;
            float endLenght = 0f;
            int BeginningObjectCount = lineObjects.Count;
            int endingObjectCount = 0;

            var brushSettings = toolSettings.overwriteBrushProperties ? toolSettings.brushSettings
               : PaletteManager.selectedBrush;
            Vector3 BrushScaleMultiplier() => (LineManager.instance.applyBrushToExisting
                && PaletteManager.selectedBrush != null) ? brushSettings.GetScaleMultiplier() : Vector3.one;
            if (brush != null && brush.frequencyMode == MultibrushSettings.FrequencyMode.PATTERN)
            {
                var endIndexes = brush.patternMachine.GetEndIndexes();
                endingObjectCount = Mathf.Min(endIndexes.Length, lineObjects.Count);
                BeginningObjectCount = lineObjects.Count - endingObjectCount;
                for (int i = 0; i < endingObjectCount; ++i)
                {
                    var obj = lineObjects[lineObjects.Count - 1 - i];
                    var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    endLenght += GetLineSpacing(prefab.transform, toolSettings, BrushScaleMultiplier(), useDictionary: true);
                }
                endLenght = Mathf.Min(endLenght, lineLength);
            }
            var itemCount = 0;
            float newItemSpacing(int itemIdx, Vector3 scale)
            {
                if (PaletteManager.selectedBrush == null)
                {
                    if (Mathf.Approximately(_minLineSpacing, float.MaxValue)) return 0f;
                    return _minLineSpacing;
                }
                var item = PaletteManager.selectedBrush.items[itemIdx];
                var key = (itemIdx, scale);
                if (toolSettings.spacingType == LineSettings.SpacingType.BOUNDS && itemIdx >= 0)
                {
                    if (item.randomScaleMultiplier) return GetLineSpacing(itemIdx, toolSettings, scale);
                    else if (prefabSpacingDictionary.ContainsKey(key)) return prefabSpacingDictionary[key];
                    else
                    {
                        var spacing = GetLineSpacing(itemIdx, toolSettings, scale);
                        prefabSpacingDictionary.Add(key, spacing);
                        return spacing;
                    }
                }
                else return GetLineSpacing(itemIdx, toolSettings, scale);
            }

            var prevAtTheEnd = false;
            bool firstNewObjectAdded = false;
            do
            {
                var nextIdx = PaletteManager.selectedBrush != null ? PaletteManager.selectedBrush.nextItemIndex : -1;

                while (lengthFromFirstPoint[segment + 1] < length)
                {
                    ++segment;
                    if (segment >= pathPoints.Length - 1) break;
                }
                if (segment >= pathPoints.Length - 1) break;

                var segmentDirection = (pathPoints[segment + 1] - pathPoints[segment]).normalized;
                var distance = length - lengthFromFirstPoint[segment];


                var itemScaleMultiplier = ScaleMultiplier(nextIdx, LineManager.settings);
                var itemSpacing = newItemSpacing(nextIdx, itemScaleMultiplier);
                var isAtTheEnd = lineLength - length - itemSpacing <= endLenght;
                if (objIdx < lineObjects.Count && isAtTheEnd && !prevAtTheEnd)
                    objIdx = lineObjects.Count - endingObjectCount;
                prevAtTheEnd = isAtTheEnd;
                var addExistingObject = objIdx < lineObjects.Count && (itemCount < BeginningObjectCount || isAtTheEnd);
                if (addExistingObject && lineObjects[objIdx] == null) addExistingObject = false;
                float spacing = 0;

                var objScale = Vector3.one;
                if (addExistingObject)
                {
                    var obj = lineObjects[objIdx];
                    if (LineManager.instance.applyBrushToExisting)
                    {
                        var brushScaleMultiplier = BrushScaleMultiplier();
                        var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                        spacing = GetLineSpacing(prefab.transform, toolSettings, brushScaleMultiplier, useDictionary: true);
                        objScale = Vector3.Scale(prefab.transform.localScale, brushScaleMultiplier);
                    }
                    else
                    {
                        spacing = GetLineSpacing(obj.transform, toolSettings, Vector3.one, useDictionary: true);
                        objScale = obj.transform.localScale;
                    }
                }
                else if (PaletteManager.selectedBrush != null) spacing = itemSpacing;
                if (spacing == 0) break;
                spacing = Mathf.Max(spacing, _minLineSpacing);
                int nearestPathointIdx;
                var position = pathPoints[segment] + segmentDirection * distance;
                float distanceFromNearestPoint;
                var intersection = LineData.NearestPathPoint(segment, position, spacing, pathPoints, out nearestPathointIdx,
                    out distanceFromNearestPoint);

                if (nearestPathointIdx > segment)
                    spacing = (pathPoints[nearestPathointIdx] - position).magnitude
                        + (intersection - pathPoints[nearestPathointIdx]).magnitude;
                length = Mathf.Max(length + spacing, lengthFromFirstPoint[nearestPathointIdx] + distanceFromNearestPoint);
                if (lineLength < length) break;
                if (addExistingObject)
                {
                    var brushAdditionalAngle = Vector3.zero;
                    bool brushFlipX = false;
                    bool brushFlipY = false;
                    var brushSurfaceDistance = 0f;
                    if (LineManager.instance.applyBrushToExisting)
                    {
                        if (PaletteManager.selectedBrush != null)
                        {
                            brushAdditionalAngle = brushSettings.GetAdditionalAngle();
                            brushFlipX = brushSettings.GetFlipX();
                            brushFlipY = brushSettings.GetFlipY();
                            brushSurfaceDistance = brushSettings.GetSurfaceDistance();
                        }
                    }
                    var startToEnd = intersection - position;
                    var centerPosition = startToEnd / 2 + position;

                    var brushstrokeDirection = toolSettings.objectsOrientedAlongTheLine ? startToEnd.normalized : Vector3.left;
                    objPositionsList.Add(new BrushstrokeObject(objIdx, centerPosition, objRotation: Quaternion.identity,
                        brushAdditionalAngle, objScale, brushFlipX, brushFlipY, brushSurfaceDistance, brushstrokeDirection));
                    ++objIdx;
                    if (isAtTheEnd && objIdx >= lineObjects.Count) break;
                }
                else if (PaletteManager.selectedBrush == null) break;
                else
                {
                    AddBrushstrokeItem(nextIdx, PaletteManager.selectedBrush == null ? 0
                        : PaletteManager.selectedBrush.GetPatternTokenIndex(), position,
                        angle: Vector3.zero, itemScaleMultiplier, LineManager.settings);
                    strokePositionsList.Add(position);
                    if (!firstNewObjectAdded)
                    {
                        firstNewObjectAdded = true;
                        firstNewObjectIdx = itemCount;
                    }
                }
                ++itemCount;

            } while (lineLength > length);
            objPositions = objPositionsList.ToArray();
            strokePositions = strokePositionsList.ToArray();
        }
    }
}
#pragma warning restore UDR0001
