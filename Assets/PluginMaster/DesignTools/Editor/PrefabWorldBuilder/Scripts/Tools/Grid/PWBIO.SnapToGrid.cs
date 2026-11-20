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
        private static Vector3 SnapPosition(Vector3 position, bool onGrid, bool applySettings,
            Vector3 snapStepFactor, bool ignoreMidpoints = false)
        {
            var result = position;
            if (GridManager.settings.radialGridEnabled)
            {
                var rotation = GridManager.settings.rotation;
                if (GridManager.settings.gridOnX) rotation *= Quaternion.AngleAxis(-90, Vector3.forward);
                else if (GridManager.settings.gridOnZ) rotation *= Quaternion.AngleAxis(-90, Vector3.right);
                var localPosition = Quaternion.Inverse(rotation) * (position - GridManager.settings.origin);
                var snappedDirOnPlane = new Vector3(localPosition.x, 0, localPosition.z).normalized;
                if (GridManager.settings.snapToRadius)
                {
                    var sectorAngleRad = TAU / GridManager.settings.radialSectors;
                    var angleRad = Mathf.Atan2(localPosition.z, localPosition.x);
                    var snappedAngleRad = Mathf.Round(angleRad / sectorAngleRad) * sectorAngleRad;
                    snappedDirOnPlane = new Vector3(Mathf.Cos(snappedAngleRad), 0, Mathf.Sin(snappedAngleRad));
                    var sizeOnplane = Mathf.Sqrt(localPosition.x * localPosition.x
                        + localPosition.z * localPosition.z);
                    var snappedOnPlane = snappedDirOnPlane * sizeOnplane;
                    var localSnapedPosition = new Vector3(snappedOnPlane.x, localPosition.y, snappedOnPlane.z);
                    result = rotation * localSnapedPosition + GridManager.settings.origin;
                }
                if (GridManager.settings.snapToCircunference)
                {
                    var sizeOnplane = Mathf.Sqrt(localPosition.x * localPosition.x
                       + localPosition.z * localPosition.z);
                    var sizeOnPlaneSnapped = Mathf.Round(sizeOnplane / GridManager.settings.radialStep)
                        * GridManager.settings.radialStep;
                    var localSnapedPosition = snappedDirOnPlane * sizeOnPlaneSnapped
                        + new Vector3(0, localPosition.y, 0);
                    result = rotation * localSnapedPosition + GridManager.settings.origin;
                }
            }
            else
            {
                var localPosition = Quaternion.Inverse(GridManager.settings.rotation)
                * (position - GridManager.settings.origin);
                float Snap(float step, float value)
                {
                    if (!ignoreMidpoints && GridManager.settings.midpointSnapping) step *= 0.5f;
                    return Mathf.Round(value / step) * step;
                }
                var localSnappedPosition = new Vector3(
                    Snap(GridManager.settings.step.x * snapStepFactor.x, localPosition.x),
                    Snap(GridManager.settings.step.y * snapStepFactor.y, localPosition.y),
                    Snap(GridManager.settings.step.z * snapStepFactor.z, localPosition.z));
                result = GridManager.settings.rotation * (applySettings ? new Vector3(
                    GridManager.settings.snappingOnX ? localSnappedPosition.x : onGrid ? 0 : localPosition.x,
                    GridManager.settings.snappingOnY ? localSnappedPosition.y : onGrid ? 0 : localPosition.y,
                    GridManager.settings.snappingOnZ ? localSnappedPosition.z : onGrid ? 0 : localPosition.z)
                    : localSnappedPosition) + GridManager.settings.origin;
            }
            return result;
        }
        private static Vector3 SnapPositionToCellFaceCenter(Vector3 position)
        {
            var step = GridManager.settings.step;
            var rotation = GridManager.settings.rotation;
            var cellCenter = SnapPositionToBlockCellCenter(position);
            var localPos = Quaternion.Inverse(rotation) * (position - cellCenter);
            var absX = Mathf.Abs(localPos.x);
            var absY = Mathf.Abs(localPos.y);
            var absZ = Mathf.Abs(localPos.z);
            var faceOffset = Vector3.zero;
            if (absX >= absY && absX >= absZ) faceOffset.x = (localPos.x >= 0 ? 1f : -1f) * step.x * 0.5f;
            else if (absZ >= absX && absZ >= absY) faceOffset.z = (localPos.z >= 0 ? 1f : -1f) * step.z * 0.5f;
            else faceOffset.y = (localPos.y >= 0 ? 1f : -1f) * step.y * 0.5f;
            return cellCenter + rotation * faceOffset;
        }
        private static Vector3 SnapAndUpdateGridOrigin(Vector3 point, bool snapToGrid,
            bool paintOnPalettePrefabs, bool paintOnMeshesWithoutCollider, bool ignoresceneColliders, bool paintOnTheGrid,
            Vector3 projectionDirection)
        {
            if (snapToGrid)
            {
                point = SnapPosition(point, paintOnTheGrid, applySettings: true, snapStepFactor: Vector3.one);
                var direction = GridManager.settings.TransformToGridDirection(GridManager.settings.rotation
                    * projectionDirection);
                if (!paintOnTheGrid && !GridManager.settings.IsSnappingEnabledInThisDirection(direction))
                {
                    var ray = new Ray(point - direction, direction);
                    if (PWBToolRaycast(ray, out RaycastHit hit, out GameObject collider, float.MaxValue, -1,
                       paintOnPalettePrefabs, paintOnMeshesWithoutCollider, ignoreSceneColliders: ignoresceneColliders))
                        point = hit.point;
                }
            }
            UpdateGridOrigin(point);
            return point;
        }
        private static Vector3 SnapFloorTilePosition(Vector3 position, out Vector3 localPosition)
        {
            var toolSettings = FloorManager.settings;
            var brushOffset = Vector3.zero;
            if (toolSettings.subtractBrushOffset)
            {
                BrushSettings brush = PaletteManager.selectedBrush;
                if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                if (brush != null) brushOffset = brush.localPositionOffset;
                if (FloorManager.quarterTurns > 0)
                    brushOffset = Quaternion.AngleAxis(FloorManager.quarterTurns * 90,
                        FloorManager.settings.upwardAxis) * brushOffset;
            }
            var localOriginOffset = (GridManager.settings.step - brushOffset) * 0.5f
               - Vector3.up * GridManager.settings.step.y;
            var origin = GridManager.settings.origin + GridManager.settings.rotation * localOriginOffset;
            var localPos = Quaternion.Inverse(GridManager.settings.rotation) * (position - origin);
            float Snap(float step, float value) => Mathf.Round(value / step) * step;
            var localSnappedPos = new Vector3(Snap(GridManager.settings.step.x, localPos.x), 0f,
                    Snap(GridManager.settings.step.z, localPos.z));
            localPosition = localSnappedPos;
            var result = GridManager.settings.rotation * localSnappedPos + origin;
            return result;
        }


        private enum CellSide { R, L, F, B };
        private static CellSide GetCellSide(Vector3 pointToGridLocal, AxesUtils.Axis axis)
        {
            CellSide cellSide;
            if (axis == AxesUtils.Axis.Z)
                cellSide = pointToGridLocal.x < 0 ? CellSide.L : CellSide.R;
            else cellSide = pointToGridLocal.z < 0 ? CellSide.B : CellSide.F;
            return cellSide;
        }
        private static Vector3 GetWallLocalBrushOffset(CellSide cellSide)
        {
            var toolSettings = WallManager.settings;
            var brushOffset = Vector3.zero;
            if (toolSettings.subtractBrushOffset)
            {
                BrushSettings brush = PaletteManager.selectedBrush;
                if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                if (brush != null) brushOffset = brush.localPositionOffset;
                if (cellSide == CellSide.L || cellSide == CellSide.R)
                {
                    var angle = cellSide == CellSide.L ? -90 : 90;
                    brushOffset = Quaternion.AngleAxis(angle, FloorManager.settings.upwardAxis) * brushOffset;
                }
                else if (cellSide == CellSide.B)
                    brushOffset = Quaternion.AngleAxis(180, FloorManager.settings.upwardAxis) * brushOffset;
            }
            return brushOffset;
        }
        private static Vector3 SnapWallPosition(Vector3 position, out AxesUtils.Axis axis,
            out bool rotateHalfTurn, out Vector3 localPosition)
        {
            var toolSettings = WallManager.settings;

            var snappedPoint = SnapPosition(position, onGrid: true, applySettings: true, snapStepFactor: Vector3.one);
            var localSnappedPoint = Quaternion.Inverse(GridManager.settings.rotation)
                * (snappedPoint - GridManager.settings.origin);
            var pointToGrid = snappedPoint - position;
            var pointToGridLocal = Quaternion.Inverse(GridManager.settings.rotation) * pointToGrid;
            axis = Mathf.Abs(pointToGridLocal.x) < Mathf.Abs(pointToGridLocal.z) ? AxesUtils.Axis.Z : AxesUtils.Axis.X;

            CellSide cellSide = GetCellSide(pointToGridLocal, axis);
            var localBrushOffset = GetWallLocalBrushOffset(cellSide);
            var localOriginOffset = GridManager.settings.step * 0.5f;
            localOriginOffset.y = 0f;
            localOriginOffset -= localBrushOffset * 0.5f;
            var origin = GridManager.settings.origin + GridManager.settings.rotation * localOriginOffset;

            var localPos = Quaternion.Inverse(GridManager.settings.rotation) * (position - origin);
            float Snap(float step, float value) => Mathf.Round(value / step) * step;
            var xSnappedToCenter = Snap(GridManager.settings.step.x, localPos.x);
            var zSnappedToCenter = Snap(GridManager.settings.step.z, localPos.z);

            var xSnappedToBorder = xSnappedToCenter;
            var zSnappedToBorder = zSnappedToCenter;
            rotateHalfTurn = false;
            if (cellSide == CellSide.L || cellSide == CellSide.R)
            {
                if (cellSide == CellSide.L)
                {
                    xSnappedToBorder = localSnappedPoint.x
                        + (WallManager.wallThickness - GridManager.settings.step.x) * 0.5f;
                    rotateHalfTurn = true;
                }
                else xSnappedToBorder = localSnappedPoint.x
                        - (WallManager.wallThickness + GridManager.settings.step.x) * 0.5f;
            }
            else
            {
                if (cellSide == CellSide.B)
                {
                    zSnappedToBorder = localSnappedPoint.z
                        + (WallManager.wallThickness - GridManager.settings.step.z) * 0.5f;
                    rotateHalfTurn = true;
                }
                else zSnappedToBorder = localSnappedPoint.z
                        - (WallManager.wallThickness + GridManager.settings.step.x) * 0.5f;
            }
            var yOffset = toolSettings.moduleSize.y / 2;

            var localSnappedPos = new Vector3(xSnappedToBorder, yOffset, zSnappedToBorder);

            localPosition = localSnappedPos;
            var result = GridManager.settings.rotation * localSnappedPos + origin;
            return result;
        }

        private static Vector3 SnapWallPosition(Vector3 startPoint, Vector3 endPoint,
            out AxesUtils.Axis axis, out int cellsCount, out bool rotateHalfTurn, out Vector3 localPosition)
        {
            float Snap(float step, float value) => Mathf.Round(value / step) * step;
            Vector3 SnapToCenter(Vector3 origin, Vector3 point)
            {
                var localPoint = Quaternion.Inverse(GridManager.settings.rotation) * (point - origin);
                var localXSnappedToCenter = Snap(GridManager.settings.step.x, localPoint.x);
                var localZSnappedTocenter = Snap(GridManager.settings.step.z, localPoint.z);
                var localCellCenter = new Vector3(localXSnappedToCenter, 0f, localZSnappedTocenter);
                return localCellCenter;
            }
            var segment = endPoint - startPoint;
            var localSegment = Quaternion.Inverse(GridManager.settings.rotation) * segment;
            var segmentMagnitudeX = Mathf.Abs(localSegment.x);
            var segmentMagnitudeZ = Mathf.Abs(localSegment.z);

            var localStartGridCenter = SnapToCenter(GridManager.settings.origin, startPoint);
            var localEndGridCenter = SnapToCenter(GridManager.settings.origin, endPoint);
            var centerToCenterSegment = localEndGridCenter - localStartGridCenter;
            var centerToCenterMagnitudeX = Mathf.Abs(centerToCenterSegment.x);
            var centerToCenterMagnitudeZ = Mathf.Abs(centerToCenterSegment.z);

            var endPointSnappedToGrid = SnapPosition(endPoint, onGrid: true, applySettings: true,
                snapStepFactor: Vector3.one);
            var localEndPointSnappedToGrid = Quaternion.Inverse(GridManager.settings.rotation)
               * (endPointSnappedToGrid - GridManager.settings.origin);
            var pointToGrid = endPointSnappedToGrid - endPoint;
            var pointToGridLocal = Quaternion.Inverse(GridManager.settings.rotation) * pointToGrid;

            axis = segmentMagnitudeX > segmentMagnitudeZ ? AxesUtils.Axis.X : AxesUtils.Axis.Z;
            if (centerToCenterMagnitudeX < GridManager.settings.step.x
                && centerToCenterMagnitudeZ < GridManager.settings.step.z)
                axis = Mathf.Abs(pointToGridLocal.x) < Mathf.Abs(pointToGridLocal.z) ? AxesUtils.Axis.Z : AxesUtils.Axis.X;

            CellSide cellSide = GetCellSide(pointToGridLocal, axis);
            var localBrushOffset = GetWallLocalBrushOffset(cellSide);

            var localOriginOffset = GridManager.settings.step * 0.5f;
            localOriginOffset.y = 0f;
            localOriginOffset -= localBrushOffset * 0.5f;
            var origin = GridManager.settings.origin + GridManager.settings.rotation * localOriginOffset;

            var localStartCellCenter = SnapToCenter(origin, startPoint);
            var localEndCellCenter = SnapToCenter(origin, endPoint);

            var localSnappedSegment = localEndCellCenter - localStartCellCenter;
            var snappedMagnitudeX = Mathf.Abs(localSnappedSegment.x);
            var snappedMagnitudeZ = Mathf.Abs(localSnappedSegment.z);

            var localXSnappedToBorder = localEndCellCenter.x;
            var localZSnappedToBorder = localEndCellCenter.z;
            rotateHalfTurn = false;

            if (cellSide == CellSide.L || cellSide == CellSide.R)
            {
                if (cellSide == CellSide.L)
                {
                    localXSnappedToBorder = localEndPointSnappedToGrid.x
                        + (WallManager.wallThickness - GridManager.settings.step.x) * 0.5f;
                    rotateHalfTurn = true;
                }
                else localXSnappedToBorder = localEndPointSnappedToGrid.x
                        - (WallManager.wallThickness + GridManager.settings.step.x) * 0.5f;
                cellsCount = Mathf.RoundToInt(snappedMagnitudeZ / GridManager.settings.step.z) + 1;
            }
            else
            {
                if (cellSide == CellSide.B)
                {
                    localZSnappedToBorder = localEndPointSnappedToGrid.z
                        + (WallManager.wallThickness - GridManager.settings.step.z) * 0.5f;
                    rotateHalfTurn = true;
                }
                else localZSnappedToBorder = localEndPointSnappedToGrid.z
                        - (WallManager.wallThickness + GridManager.settings.step.x) * 0.5f;
                cellsCount = Mathf.RoundToInt(snappedMagnitudeX / GridManager.settings.step.x) + 1;
            }

            var yOffset = WallManager.settings.moduleSize.y / 2;

            var localSnappedPos = new Vector3(localXSnappedToBorder, yOffset, localZSnappedToBorder);
            localPosition = localSnappedPos;
            var result = GridManager.settings.rotation * localSnappedPos + origin;
            return result;
        }

        private static void UpdateGridOrigin(Vector3 hitPoint)
        {
            var snapOrigin = GridManager.settings.origin;
            if (!GridManager.settings.lockedGrid)
            {
                if (GridManager.settings.gridOnX) snapOrigin.x = hitPoint.x;
                else if (GridManager.settings.gridOnY) snapOrigin.y = hitPoint.y;
                else if (GridManager.settings.gridOnZ) snapOrigin.z = hitPoint.z;
            }
            GridManager.settings.origin = snapOrigin;
        }
    }
}
#pragma warning restore UDR0001
