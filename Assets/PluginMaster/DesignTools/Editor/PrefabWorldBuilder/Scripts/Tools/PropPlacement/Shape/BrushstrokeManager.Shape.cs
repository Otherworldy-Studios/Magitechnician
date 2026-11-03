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
    public static partial class BrushstrokeManager
    {
        private static Vector3 GetAngleToShapeCenter(ShapeSettings settings, Vector3 position,
            Vector3 center, Vector3 itemDir, Quaternion lookAt, Vector3 defaultValue)
        {
            if (settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.FROM_CENTER
                || settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.TO_CENTER)
            {
                var upwards = settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.FROM_CENTER
                    ? position - center : center - position;
                if (settings.shapeType == ShapeSettings.ShapeType.POLYGON) upwards -= Vector3.Project(upwards, itemDir);
                var segmentRotationToCenter = Quaternion.LookRotation(itemDir, upwards) * lookAt;
                return segmentRotationToCenter.eulerAngles;
            }
            return defaultValue;
        }

        public static void UpdateShapeBrushstroke()
        {
            _brushstroke.Clear();
            if (PaletteManager.selectedBrush == null) return;
            var shapeData = ShapeData.instance;
            if (shapeData.state < ToolController.ToolState.EDIT) return;
            var settings = ShapeManager.settings;
            var points = new System.Collections.Generic.List<Vector3>();
            var firstVertexIdx = shapeData.firstVertexIdxAfterIntersection;
            var lastVertexIdx = shapeData.lastVertexIdxBeforeIntersection;
            int sidesCount = settings.shapeType == ShapeSettings.ShapeType.POLYGON ? settings.sidesCount
                : shapeData.circleSideCount;
            int GetNextVertexIdx(int currentIdx) => currentIdx == sidesCount ? 1 : currentIdx + 1;
            int GetPrevVertexIdx(int currentIdx) => currentIdx == 1 ? sidesCount : currentIdx - 1;
            var firstPrev = GetPrevVertexIdx(firstVertexIdx);
            points.Add(shapeData.GetArcIntersection(0));
            if (lastVertexIdx != firstPrev || (lastVertexIdx == firstPrev && shapeData.arcAngle > 120))
            {
                var vertexIdx = firstVertexIdx;
                var nextVertexIdx = firstVertexIdx;
                do
                {
                    vertexIdx = nextVertexIdx;
                    points.Add(shapeData.GetPoint(vertexIdx));
                    nextVertexIdx = GetNextVertexIdx(nextVertexIdx);
                } while (vertexIdx != lastVertexIdx);
            }
            var lastPoint = shapeData.GetArcIntersection(1);
            if (points.Last() != lastPoint) points.Add(lastPoint);


            var prefabSpacingDictionary = new System.Collections.Generic.Dictionary<(int, Vector3), float>();
            void AddItemsToLine(Vector3 start, Vector3 end, ref int nextIdx)
            {
                if (nextIdx < 0) nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                var startToEnd = end - start;
                var lineLength = startToEnd.magnitude;
                float itemsSize = 0f;
                var items = new System.Collections.Generic.List<(int idx, float size, Vector3 scaleMult)>();

                do
                {
                    var nextItem = PaletteManager.selectedBrush.items[nextIdx];
                    var scaleMult = ScaleMultiplier(nextIdx, ShapeManager.settings);
                    float itemSize;
                    var key = (nextIdx, scaleMult);
                    if (nextItem.randomScaleMultiplier) itemSize = GetLineSpacing(nextIdx, settings, scaleMult);
                    else if (prefabSpacingDictionary.ContainsKey(key)) itemSize = prefabSpacingDictionary[key];
                    else
                    {
                        itemSize = GetLineSpacing(nextIdx, settings, scaleMult);
                        prefabSpacingDictionary.Add(key, itemSize);
                    }
                    itemSize = Mathf.Max(itemSize, _minLineSpacing);
                    if (itemsSize + itemSize > lineLength) break;
                    itemsSize += itemSize;
                    items.Add((nextIdx, itemSize, scaleMult));
                    nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                } while (itemsSize <= lineLength);
                var spacing = (lineLength - itemsSize) / (items.Count + 1);
                var distance = spacing;
                var direction = startToEnd.normalized;

                Vector3 itemDir = (settings.objectsOrientedAlongTheLine && direction != Vector3.zero)
                    ? direction : Vector3.forward;
                var planeUp = shapeData.planeRotation * Vector3.up;
                var lookAt = Quaternion.LookRotation((AxesUtils.SignedAxis)(settings.axisOrientedAlongTheLine), Vector3.up);
                var segmentRotation = Quaternion.LookRotation(itemDir, -settings.projectionDirection) * lookAt;
                var angle = segmentRotation.eulerAngles;
                foreach (var item in items)
                {
                    var brushItem = PaletteManager.selectedBrush.items[item.idx];
                    if (brushItem.prefab == null) continue;
                    var position = start + direction * (distance + item.size / 2);
                    var itemAngle = angle;
                    itemAngle = GetAngleToShapeCenter(settings, position, shapeData.center, itemDir, lookAt, itemAngle);
                    AddBrushstrokeItem(item.idx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                        position, itemAngle, item.scaleMult, settings);
                    distance += item.size + spacing;
                }
            }
            int nexItemItemIdx = -1;

            if (ShapeManager.settings.shapeType == ShapeSettings.ShapeType.CIRCLE)
            {
                const float TAU = 2 * Mathf.PI;
                var perimeter = TAU * shapeData.radius;
                var items = new System.Collections.Generic.List<(int idx, float size, Vector3 scaleMult)>();
                var minspacing = perimeter / 1024f;
                float itemsSize = 0f;

                var firstLocalArcIntersection = Quaternion.Inverse(shapeData.planeRotation)
                    * (shapeData.GetArcIntersection(0) - shapeData.center);
                var firstLocalAngle = Mathf.Atan2(firstLocalArcIntersection.z, firstLocalArcIntersection.x);
                if (firstLocalAngle < 0) firstLocalAngle += TAU;
                var secondLocalArcIntersection = Quaternion.Inverse(shapeData.planeRotation)
                   * (shapeData.GetArcIntersection(1) - shapeData.center);
                var secondLocalAngle = Mathf.Atan2(secondLocalArcIntersection.z, secondLocalArcIntersection.x);
                if (secondLocalAngle < 0) secondLocalAngle += TAU;
                if (secondLocalAngle <= firstLocalAngle) secondLocalAngle += TAU;
                var arcDelta = secondLocalAngle - firstLocalAngle;
                var arcPerimeter = arcDelta / TAU * perimeter;
                if (PaletteManager.selectedBrush.patternMachine != null &&
                    PaletteManager.selectedBrush.restartPatternForEachStroke)
                    PaletteManager.selectedBrush.patternMachine.Reset();
                do
                {
                    float itemSize = 0;
                    var nextIdx = PaletteManager.selectedBrush.nextItemIndex;
                    var nextItem = PaletteManager.selectedBrush.items[nextIdx];
                    var scaleMult = ScaleMultiplier(nextIdx, ShapeManager.settings);
                    var key = (nextIdx, scaleMult);
                    if (nextItem.randomScaleMultiplier) itemSize = GetLineSpacing(nextIdx, settings, scaleMult);
                    else if (prefabSpacingDictionary.ContainsKey(key)) itemSize = prefabSpacingDictionary[key];
                    else
                    {
                        itemSize = GetLineSpacing(nextIdx, settings, scaleMult);
                        prefabSpacingDictionary.Add(key, itemSize);
                    }
                    itemSize = Mathf.Max(itemSize, minspacing);
                    if (itemsSize + itemSize > arcPerimeter) break;
                    itemsSize += itemSize;
                    items.Add((nextIdx, itemSize, scaleMult));
                } while (itemsSize < arcPerimeter);

                var spacing = (arcPerimeter - itemsSize) / (items.Count);

                if (items.Count == 0) return;
                var distance = firstLocalAngle / TAU * perimeter + items[0].size / 2;

                for (int i = 0; i < items.Count; ++i)
                {
                    var item = items[i];
                    var arcAngle = distance / perimeter * TAU;
                    var LocalRadiusVector = new Vector3(Mathf.Cos(arcAngle), 0f, Mathf.Sin(arcAngle))
                        * ShapeData.instance.radius;
                    var radiusVector = ShapeData.instance.planeRotation * LocalRadiusVector;
                    var position = radiusVector + ShapeData.instance.center;
                    var itemDir = settings.objectsOrientedAlongTheLine
                        ? Vector3.Cross(ShapeData.instance.planeRotation * Vector3.up, radiusVector) : Vector3.forward;
                    var lookAt = Quaternion.LookRotation((AxesUtils.SignedAxis)(settings.axisOrientedAlongTheLine),
                        Vector3.up);
                    var segmentRotation = Quaternion.LookRotation(itemDir, -settings.projectionDirection) * lookAt;
                    var angle = segmentRotation.eulerAngles;
                    angle = GetAngleToShapeCenter(settings, position, shapeData.center, itemDir, lookAt, angle);
                    AddBrushstrokeItem(item.idx, PaletteManager.selectedBrush.GetPatternTokenIndex(),
                        position, angle, item.scaleMult, settings);
                    var next_Item = items[(i + 1) % items.Count];
                    distance += item.size / 2 + next_Item.size / 2 + spacing;
                }
            }
            else
            {
                if (PaletteManager.selectedBrush.patternMachine != null &&
                    PaletteManager.selectedBrush.restartPatternForEachStroke)
                    PaletteManager.selectedBrush.patternMachine.Reset();
                for (int i = 0; i < points.Count - 1; ++i)
                {
                    var start = points[i];
                    var end = points[i + 1];
                    AddItemsToLine(start, end, ref nexItemItemIdx);
                }
            }
        }

        public static void UpdatePersistentShapeBrushstroke(ShapeData data,
            System.Collections.Generic.List<GameObject> shapeObjects,
            out BrushstrokeObject[] objPoses)
        {
            _brushstroke.Clear();
            var objPosesList = new System.Collections.Generic.List<BrushstrokeObject>();
            var settings = data.settings;
            var prefabSpacingDictionary = new System.Collections.Generic.Dictionary<(int, Vector3), float>();
            int nextItemIdx = -1;
            objPoses = objPosesList.ToArray();

            var toolSettings = ShapeManager.settings;

            var brushSettings = toolSettings.overwriteBrushProperties ? toolSettings.brushSettings
               : PaletteManager.selectedBrush;
            Vector3 BrushScaleMultiplier() => (ShapeManager.instance.applyBrushToExisting
                  && PaletteManager.selectedBrush != null) ? brushSettings.GetScaleMultiplier() : Vector3.one;

            if (settings.shapeType == ShapeSettings.ShapeType.CIRCLE)
            {
                const float TAU = 2 * Mathf.PI;
                var perimeter = TAU * data.radius;
                var items = new System.Collections.Generic.List<(int idx, float size, bool objExist, Vector3 objScale)>();
                var minspacing = perimeter / 1024f;
                float itemsSize = 0f;

                var firstLocalArcIntersection = Quaternion.Inverse(data.planeRotation)
                    * (data.GetArcIntersection(0) - data.center);
                var firstLocalAngle = Mathf.Atan2(firstLocalArcIntersection.z, firstLocalArcIntersection.x);
                if (firstLocalAngle < 0) firstLocalAngle += TAU;
                var secondLocalArcIntersection = Quaternion.Inverse(data.planeRotation)
                   * (data.GetArcIntersection(1) - data.center);
                var secondLocalAngle = Mathf.Atan2(secondLocalArcIntersection.z, secondLocalArcIntersection.x);
                if (secondLocalAngle < 0) secondLocalAngle += TAU;
                if (secondLocalAngle <= firstLocalAngle) secondLocalAngle += TAU;
                var arcDelta = secondLocalAngle - firstLocalAngle;
                var arcPerimeter = arcDelta / TAU * perimeter;

                var objIdx = 0;
                int GetNextIdx() => PaletteManager.selectedBrush != null ? PaletteManager.selectedBrush.nextItemIndex : -1;
                if (nextItemIdx < 0) nextItemIdx = GetNextIdx();

                do
                {
                    float itemSize = 0;
                    var objectExist = objIdx < shapeObjects.Count && shapeObjects[objIdx] != null;
                    var objScale = Vector3.one;
                    var brushScaleMultiplier = BrushScaleMultiplier();
                    if (objectExist)
                    {
                        var obj = shapeObjects[objIdx];
                        if (ShapeManager.instance.applyBrushToExisting)
                        {
                            var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                            itemSize = GetLineSpacing(prefab.transform, toolSettings,
                                brushScaleMultiplier, useDictionary: true);
                            objScale = Vector3.Scale(prefab.transform.localScale, brushScaleMultiplier);
                        }
                        else
                        {
                            itemSize = GetLineSpacing(shapeObjects[objIdx].transform, settings,
                            Vector3.one, useDictionary: false);
                            objScale = obj.transform.localScale;
                        }
                    }
                    else if (PaletteManager.selectedBrush != null)
                    {
                        var nextItem = PaletteManager.selectedBrush.items[nextItemIdx];
                        var prefab = nextItem.prefab;
                        itemSize = GetLineSpacing(prefab.transform, toolSettings,
                                brushScaleMultiplier, useDictionary: true);
                        objScale = Vector3.Scale(prefab.transform.localScale, brushScaleMultiplier);
                    }
                    else break;
                    itemSize = Mathf.Max(itemSize, minspacing);
                    if (itemsSize + itemSize > arcPerimeter) break;
                    itemsSize += itemSize;
                    items.Add((objectExist ? objIdx : nextItemIdx, itemSize, objectExist, objScale));
                    nextItemIdx = GetNextIdx();
                    if (objectExist) ++objIdx;
                } while (itemsSize < arcPerimeter);
                var spacing = (arcPerimeter - itemsSize) / (items.Count);

                if (items.Count == 0)
                {
                    return;
                }
                var distance = firstLocalAngle / TAU * perimeter + items[0].size / 2;
                int itemCount = 0;
                for (int i = 0; i < items.Count; ++i)
                {
                    var item = items[i];
                    GameObject obj = null;
                    if (item.objExist) obj = shapeObjects[item.idx];
                    else if (PaletteManager.selectedBrush != null) obj = PaletteManager.selectedBrush.items[item.idx].prefab;
                    if (obj == null) continue;

                    var arcAngle = distance / perimeter * TAU;
                    var LocalRadiusVector = new Vector3(Mathf.Cos(arcAngle), 0f, Mathf.Sin(arcAngle))
                        * data.radius;
                    var radiusVector = data.planeRotation * LocalRadiusVector;
                    var position = radiusVector + data.center;
                    var itemDir = settings.objectsOrientedAlongTheLine
                        ? Vector3.Cross(data.planeRotation * Vector3.up, radiusVector) : Vector3.forward;
                    var lookAt = Quaternion.LookRotation((AxesUtils.SignedAxis)(settings.axisOrientedAlongTheLine),
                        Vector3.up);
                    var segmentRotation = Quaternion.LookRotation(itemDir, -settings.projectionDirection) * lookAt;
                    var angle = segmentRotation.eulerAngles;
                    angle = GetAngleToShapeCenter(settings, position, data.center, itemDir, lookAt, angle);
                    if (item.objExist)
                    {
                        var brushAdditionalAngle = Vector3.zero;
                        bool brushFlipX = false;
                        bool brushFlipY = false;
                        var brushSurfaceDistance = 0f;
                        if (ShapeManager.instance.applyBrushToExisting)
                        {
                            if (PaletteManager.selectedBrush != null)
                            {
                                brushAdditionalAngle = brushSettings.GetAdditionalAngle();
                                brushFlipX = brushSettings.GetFlipX();
                                brushFlipY = brushSettings.GetFlipY();
                                brushSurfaceDistance = brushSettings.GetSurfaceDistance();
                            }
                        }
                        objPosesList.Add(new BrushstrokeObject(item.idx, position, Quaternion.Euler(angle),
                            brushAdditionalAngle, item.objScale, brushFlipX, brushFlipY, brushSurfaceDistance,
                            brushstrokeDirection: Vector3.zero));
                    }
                    else AddBrushstrokeItem(item.idx, PaletteManager.selectedBrush.GetPatternTokenIndex(), position, angle,
                        item.objScale, ShapeManager.settings);
                    var next_Item = items[(i + 1) % items.Count];
                    distance += item.size / 2 + next_Item.size / 2 + spacing;
                    ++itemCount;
                }
            }
            else
            {
                var points = new System.Collections.Generic.List<Vector3>();
                var firstVertexIdx = data.firstVertexIdxAfterIntersection;
                var lastVertexIdx = data.lastVertexIdxBeforeIntersection;
                int sidesCount = settings.shapeType == ShapeSettings.ShapeType.POLYGON ? settings.sidesCount
                    : data.circleSideCount;
                int GetNextVertexIdx(int currentIdx) => currentIdx == sidesCount ? 1 : currentIdx + 1;
                int GetPrevVertexIdx(int currentIdx) => currentIdx == 1 ? sidesCount : currentIdx - 1;
                var firstPrev = GetPrevVertexIdx(firstVertexIdx);
                points.Add(data.GetArcIntersection(0));
                if (lastVertexIdx != firstPrev || (lastVertexIdx == firstPrev && data.arcAngle > 120))
                {

                    var vertexIdx = firstVertexIdx;
                    var nextVertexIdx = firstVertexIdx;

                    do
                    {
                        vertexIdx = nextVertexIdx;
                        if (vertexIdx >= data.pointsCount || points.Count >= data.pointsCount)
                        {
                            ShapeData.instance.Update(true);
                            return;
                        }
                        points.Add(data.GetPoint(vertexIdx));
                        nextVertexIdx = GetNextVertexIdx(nextVertexIdx);
                    } while (vertexIdx != lastVertexIdx);
                }
                var lastPoint = data.GetArcIntersection(1);
                if (points.Last() != lastPoint) points.Add(lastPoint);
                int firstObjInSegmentIdx = 0;

                void AddItemsToLine(Vector3 start, Vector3 end)
                {
                    int GetNextIdx() => PaletteManager.selectedBrush != null
                        ? PaletteManager.selectedBrush.nextItemIndex : -1;
                    if (nextItemIdx < 0) nextItemIdx = GetNextIdx();
                    var startToEnd = end - start;
                    var lineLength = startToEnd.magnitude;

                    float itemsSize = 0f;
                    var items = new System.Collections.Generic.List<(int idx, float size, bool objExist, Vector3 objScale)>();
                    var minspacing = (lineLength * points.Count) / 1024f;
                    int objSegmentIdx = 0;
                    var objIdx = firstObjInSegmentIdx + objSegmentIdx;
                    do
                    {
                        float itemSize = 0;
                        var objectExist = objIdx < shapeObjects.Count;
                        var objScale = Vector3.one;
                        var brushScaleMultiplier = BrushScaleMultiplier();
                        if (objectExist)
                        {
                            var obj = shapeObjects[objIdx];
                            if (obj == null)
                            {
                                ++objIdx;
                                continue;
                            }
                            if (ShapeManager.instance.applyBrushToExisting)
                            {
                                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                                itemSize = GetLineSpacing(prefab.transform, toolSettings,
                                    brushScaleMultiplier, useDictionary: true);
                                objScale = Vector3.Scale(prefab.transform.localScale, brushScaleMultiplier);
                            }
                            else
                            {
                                itemSize = GetLineSpacing(obj.transform, settings,
                                Vector3.one, useDictionary: false);
                                objScale = obj.transform.localScale;
                            }
                        }
                        else if (PaletteManager.selectedBrush != null)
                        {
                            var nextItem = PaletteManager.selectedBrush.items[nextItemIdx];
                            var prefab = nextItem.prefab;
                            itemSize = GetLineSpacing(prefab.transform, toolSettings,
                                    brushScaleMultiplier, useDictionary: true);
                            objScale = Vector3.Scale(prefab.transform.localScale, brushScaleMultiplier);
                        }
                        else break;
                        itemSize = Mathf.Max(itemSize, minspacing);
                        if (itemsSize + itemSize > lineLength) break;
                        itemsSize += itemSize;
                        items.Add((objectExist ? objIdx : nextItemIdx, itemSize, objectExist, objScale));
                        nextItemIdx = GetNextIdx();
                        if (objectExist) ++objIdx;
                    } while (itemsSize < lineLength);


                    var spacing = (lineLength - itemsSize) / (items.Count + 1);
                    var distance = spacing;
                    var direction = startToEnd.normalized;
                    Vector3 itemDir = (settings.objectsOrientedAlongTheLine && direction != Vector3.zero)
                        ? direction : Vector3.forward;
                    var lookAt = Quaternion.LookRotation(
                        (AxesUtils.SignedAxis)(settings.axisOrientedAlongTheLine), Vector3.up);
                    var segmentRotation = Quaternion.LookRotation(itemDir, -settings.projectionDirection) * lookAt;
                    var angle = segmentRotation.eulerAngles;

                    foreach (var item in items)
                    {
                        GameObject obj = null;
                        if (item.objExist) obj = shapeObjects[item.idx];
                        else if (PaletteManager.selectedBrush != null)
                            obj = PaletteManager.selectedBrush.items[item.idx].prefab;
                        if (obj == null) continue;
                        var position = start + direction * (distance + item.size / 2);
                        var itemAngle = angle;
                        itemAngle = GetAngleToShapeCenter(settings, position, data.center, itemDir, lookAt, itemAngle);
                        if (item.objExist)
                        {
                            var brushAdditionalAngle = Vector3.zero;
                            bool brushFlipX = false;
                            bool brushFlipY = false;
                            var brushSurfaceDistance = 0f;
                            if (ShapeManager.instance.applyBrushToExisting)
                            {
                                if (PaletteManager.selectedBrush != null)
                                {
                                    brushAdditionalAngle = brushSettings.GetAdditionalAngle();
                                    brushFlipX = brushSettings.GetFlipX();
                                    brushFlipY = brushSettings.GetFlipY();
                                    brushSurfaceDistance = brushSettings.GetSurfaceDistance();
                                }
                            }
                            objPosesList.Add(new BrushstrokeObject(item.idx, position, Quaternion.Euler(itemAngle),
                                brushAdditionalAngle, item.objScale, brushFlipX, brushFlipY, brushSurfaceDistance,
                            brushstrokeDirection: Vector3.zero));
                        }
                        else AddBrushstrokeItem(item.idx,
                            PaletteManager.selectedBrush == null ? 0 : PaletteManager.selectedBrush.GetPatternTokenIndex(),
                            position, itemAngle, item.objScale, settings);
                        distance += item.size + spacing;
                        ++firstObjInSegmentIdx;
                    }
                }

                for (int i = 0; i < points.Count - 1; ++i)
                {
                    var start = points[i];
                    var end = points[i + 1];
                    AddItemsToLine(start, end);
                }
            }
            objPoses = objPosesList.ToArray();
        }
    }
}
#pragma warning restore UDR0001
