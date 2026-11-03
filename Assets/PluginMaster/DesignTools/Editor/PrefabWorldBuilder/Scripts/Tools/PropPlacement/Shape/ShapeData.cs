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
    [System.Serializable]
    public class ShapeData : PersistentData<ShapeToolName, ShapeSettings, ControlPoint>
    {
        [SerializeField] private float _radius = 0f;
        [SerializeField] private float _arcAngle = 360f;
        [SerializeField] private Vector3 _normal = Vector3.up;
        [SerializeField] private int _firstVertexIdxAfterIntersection = 2;
        [SerializeField] private int _lastVertexIdxBeforeIntersection = 1;
        [SerializeField] private Vector3[] _arcIntersections = new Vector3[2];
        private Plane _plane;
        private System.Collections.Generic.List<Vector3> _onSurfacePoints = new System.Collections.Generic.List<Vector3>();
        private int _circleSideCount = 8;

        public Vector3 normal
        {
            get => _normal;
            set
            {
                if (_normal == value) return;
                _normal = value;
            }
        }
        public int circleSideCount => _circleSideCount;
        public void SetCenter(Vector3 value, Vector3 normal)
        {
            if (pointsCount == 0)
            {
                AddPoint(value, false);
                AddPoint(value, false);
            }
            else if (points[0] != value)
            {
                SetPoint(0, value, registerUndo: false, selectAll: false);
                SetPoint(1, value, registerUndo: false, selectAll: false);
            }
            _normal = normal;
            _plane = new Plane(normal, value);
            if (_settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.PLANE_NORMAL)
                _settings.UpdateProjectDirection(-_normal);
        }

        public void SetRadius(Vector3 point)
        {
            SetPoint(1, point, registerUndo: false, selectAll: false);
            radius = Mathf.Max((points[1] - points[0]).magnitude, 0.001f);
            if (_settings.shapeType == ShapeSettings.ShapeType.CIRCLE) UpdateCircleSideCount();
        }
        public float radius
        {
            get => _radius;
            set
            {
                var r = Mathf.Max(value, 0.0001f);
                if (_radius == r) return;
                _radius = r;
                ToolProperties.RepainWindow();
            }
        }
        public Vector3 radiusPoint => points[1];
        public Plane plane => _plane;
        public Vector3 center => points[0];
        public float arcAngle => _arcAngle;

        public Vector3 GetArcIntersection(int idx) => _arcIntersections[idx];
        public int firstVertexIdxAfterIntersection => _firstVertexIdxAfterIntersection;
        public int lastVertexIdxBeforeIntersection => _lastVertexIdxBeforeIntersection;


        public void SetHandlePoints(Vector3[] vertices)
        {
            if (pointsCount > 2) PointsRemoveRange(2, pointsCount - 2);
            var midPoints = new System.Collections.Generic.List<Vector3>();
            for (int i = 1; i < vertices.Length; ++i)
            {
                AddPoint(vertices[i]);
                if (_settings.shapeType == ShapeSettings.ShapeType.POLYGON)
                    midPoints.Add((vertices[i] - vertices[i - 1]) / 2 + vertices[i - 1]);
            }
            if (_settings.shapeType == ShapeSettings.ShapeType.POLYGON)
            {
                midPoints.Add((vertices[vertices.Length - 1] - vertices[0]) / 2 + vertices[0]);
                AddPointRange(ControlPoint.VectorArrayToPointArray(midPoints.ToArray()));
            }
            var arcPoint = points[1] + (points[1] - points[0]);
            AddPoint(arcPoint);
            AddPoint(arcPoint);
            _arcIntersections[0] = points[1];
            _arcIntersections[1] = points[1];
            UpdateOnSurfacePoints();
        }
        public Vector3[] vertices => ControlPoint.PointArrayToVectorArray(PointsGetRange(1,
            _settings.shapeType == ShapeSettings.ShapeType.POLYGON ? _settings.sidesCount : _circleSideCount));
        public Quaternion planeRotation
        {
            get
            {
                var forward = Vector3.Cross(_normal, Vector3.right);
                if (forward.sqrMagnitude < 0.000001) forward = Vector3.Cross(_normal, Vector3.down);
                return Quaternion.LookRotation(forward, _normal);
            }
        }

        private static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
            Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
        {
            Vector3 lineVec3 = linePoint2 - linePoint1;
            Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
            Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);
            float planarFactor = Mathf.Abs(90 - Vector3.Angle(lineVec3, crossVec1and2));
            if (planarFactor < 0.01f && crossVec1and2.sqrMagnitude > 0.001f)
            {
                float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
                intersection = linePoint1 + (lineVec1 * s);
                var min = Vector3.Max(Vector3.Min(linePoint1, linePoint1 + lineVec1),
                    Vector3.Min(linePoint2, linePoint2 + lineVec2));
                var max = Vector3.Min(Vector3.Max(linePoint1, linePoint1 + lineVec1),
                    Vector3.Max(linePoint2, linePoint2 + lineVec2));
                var tolerance = Vector3.one * 0.001f;
                var minComp = intersection + tolerance - min;
                var maxComp = max + tolerance - intersection;
                var result = minComp.x >= 0 && minComp.y >= 0 && minComp.z >= 0
                    && maxComp.x >= 0 && maxComp.y >= 0 && maxComp.z >= 0;
                return result;
            }
            else
            {
                intersection = Vector3.zero;
                return false;
            }
        }

        public void UpdateIntersections()
        {
            if (state < ToolController.ToolState.EDIT) return;
            var centerToArc1 = GetPoint(-1) - center;
            var centerToArc2 = GetPoint(-2) - center;

            bool firstPointFound = false;
            bool lastPointFound = false;
            var sidesCount = _settings.shapeType == ShapeSettings.ShapeType.POLYGON
                ? _settings.sidesCount : _circleSideCount;
            int GetNextVertexIdx(int currentIdx) => currentIdx == sidesCount ? 1 : currentIdx + 1;
            for (int i = 1; i <= sidesCount; ++i)
            {
                var startPoint = GetPoint(i);
                var endIdx = GetNextVertexIdx(i);
                var endPoint = GetPoint(endIdx);
                var startToEnd = endPoint - startPoint;
                if (!firstPointFound)
                {
                    if (LineLineIntersection(out Vector3 intersection, center, centerToArc1,
                        startPoint, startToEnd))
                    {
                        firstPointFound = true;
                        _firstVertexIdxAfterIntersection = endIdx;
                        _arcIntersections[0] = intersection;
                    }
                }
                if (!lastPointFound)
                {
                    if (LineLineIntersection(out Vector3 intersection, center, centerToArc2,
                        startPoint, startToEnd))
                    {
                        lastPointFound = true;
                        _lastVertexIdxBeforeIntersection = i;
                        _arcIntersections[1] = intersection;
                    }
                }
                if (firstPointFound && lastPointFound) break;
            }
        }

        public Quaternion rotation
        {
            get
            {
                var radiusVector = radiusPoint - center;
                if (radiusVector == Vector3.zero)
                {
                    radiusVector = Vector3.Cross(normal, Vector3.right);
                    if (radiusVector.sqrMagnitude < 0.000001) radiusVector = Vector3.Cross(normal, Vector3.down);
                }
                return Quaternion.LookRotation(radiusVector, _normal);
            }
            set
            {
                var prevRadiusVector = radiusPoint - center;
                if (prevRadiusVector == Vector3.zero)
                {
                    prevRadiusVector = Vector3.Cross(normal, Vector3.right);
                    if (prevRadiusVector.sqrMagnitude < 0.000001) prevRadiusVector = Vector3.Cross(normal, Vector3.down);
                }
                var prev = Quaternion.LookRotation(prevRadiusVector, normal);
                _plane.normal = _normal = value * Vector3.up;
                var delta = value * Quaternion.Inverse(prev);
                for (int i = 0; i < pointsCount - 2; ++i)
                    SetPoint(i, delta * (points[i] - center) + center, registerUndo: false, selectAll: false);
                SetPoint(pointsCount - 1, delta * (points[pointsCount - 1] - center).normalized
                    * radius * 2f + center, registerUndo: false, selectAll: false);
                SetPoint(pointsCount - 2, delta * (points[pointsCount - 2] - center).normalized
                    * radius * 2f + center, registerUndo: false, selectAll: false);
                UpdateIntersections();
                if (_settings.projectionDirectionType == ShapeSettings.ShapeProjectionDirection.PLANE_NORMAL)
                    _settings.UpdateProjectDirection(-_normal);
                UpdateOnSurfacePoints();
            }
        }

        public void MovePoint(int idx, Vector3 position)
        {
            if (position == points[idx]) return;
            var delta = position - points[idx];
            if (idx == 0)
            {
                for (int i = 0; i < pointsCount; ++i)
                    SetPoint(i, points[i] + delta, registerUndo: true, selectAll: false);
                _arcIntersections[0] += delta;
                _arcIntersections[1] += delta;
            }
            else
            {
                var normalDelta = Vector3.Project(delta, _normal);
                var centerToPoint = points[idx] - center;
                var radiusDelta = Vector3.Project(delta, centerToPoint);
                var newRadius = position - center - normalDelta;
                var angle = Vector3.SignedAngle(centerToPoint, newRadius, _normal);
                var rotation = Quaternion.AngleAxis(angle, _normal);
                if ((_settings.shapeType == ShapeSettings.ShapeType.CIRCLE && idx == 1)
                  || (_settings.shapeType == ShapeSettings.ShapeType.POLYGON
                  && idx <= _settings.sidesCount * 2))
                {
                    radius = newRadius.magnitude;
                    var radiusScale = radius < 0.1f ? 1f : 1f + radiusDelta.magnitude / radius
                        * (Vector3.Dot(centerToPoint, radiusDelta) >= 0 ? 1f : -1f);
                    for (int i = 0; i < pointsCount - 2; ++i)
                        SetPoint(i, rotation * (points[i] - center) * radiusScale + normalDelta + center,
                            registerUndo: false, selectAll: false);
                    SetPoint(pointsCount - 1, rotation * (points[pointsCount - 1] - center).normalized
                        * radius * 2f + center + normalDelta, registerUndo: false, selectAll: false);
                    SetPoint(pointsCount - 2, rotation * (points[pointsCount - 2] - center).normalized
                        * radius * 2f + center + normalDelta, registerUndo: true, selectAll: false);
                }
                else
                {
                    SetPoint(idx, rotation * (points[idx] - center) + center, registerUndo: true, selectAll: false);
                    if (normalDelta != Vector3.zero)
                    {
                        for (int i = 0; i < pointsCount; ++i)
                            SetPoint(i, points[i] + normalDelta, registerUndo: true, selectAll: false);
                    }
                    _arcAngle = Vector3.SignedAngle(GetPoint(-1) - center, GetPoint(-2) - center, normal);
                    if (_arcAngle <= 0) _arcAngle += 360;
                }
                UpdateIntersections();
            }
            UpdateOnSurfacePoints();
        }

        public bool UpdateCircleSideCount()
        {
            var perimenter = 2 * Mathf.PI * radius;
            var maxItemSize = 1f;
            if (PaletteManager.selectedBrush != null)
            {
                maxItemSize = float.MinValue;
                for (int i = 0; i < PaletteManager.selectedBrush.itemCount; ++i)
                {
                    var item = PaletteManager.selectedBrush.items[i];
                    var scale = item.randomScaleMultiplier
                        ? item.randomScaleMultiplierRange.randomVector : item.scaleMultiplier;
                    if (LineManager.settings.overwriteBrushProperties)
                        scale = LineManager.settings.brushSettings.randomScaleMultiplier
                            ? LineManager.settings.brushSettings.randomScaleMultiplierRange.max
                            : LineManager.settings.brushSettings.scaleMultiplier;
                    maxItemSize = Mathf.Max(BrushstrokeManager.GetLineSpacing(i, _settings, scale), maxItemSize);
                }
            }
            var prevCount = _circleSideCount;
            _circleSideCount = Mathf.FloorToInt(perimenter / maxItemSize);
            var sideLenght = 2 * radius * Mathf.Sin(Mathf.PI / _circleSideCount);
            if (sideLenght <= maxItemSize) --_circleSideCount;
            _circleSideCount = Mathf.Max(_circleSideCount, 32);
            return prevCount != _circleSideCount;
        }

        protected override void Initialize()
        {
            base.Initialize();
            _arcIntersections[0] = _arcIntersections[1] = Vector3.zero;
            _radius = 0f;
            _arcAngle = 360f;
            _normal = Vector3.up;
            _plane = new Plane(_normal, Vector3.zero);
            _firstVertexIdxAfterIntersection = 2;
            _lastVertexIdxBeforeIntersection = 1;
            _circleSideCount = 8;
        }

        public void Update(bool clearSelection)
        {
            if (pointsCount < 2) return;
            ToolProperties.RegisterUndo(COMMAND_NAME);
            if (clearSelection) selectedPointIdx = -1;
            var arcPoints = PointsGetRange(pointsCount - 2, 2);
            var center = points[0];
            var polygonVertices = PWBIO.GetPolygonVertices(this);
            _controlPoints.Clear();
            _controlPoints.Add(center);
            _controlPoints.AddRange(ControlPoint.VectorArrayToPointArray(polygonVertices));
            if (_settings.shapeType == ShapeSettings.ShapeType.POLYGON)
            {
                for (int i = 1; i < polygonVertices.Length; ++i)
                    _controlPoints.Add((polygonVertices[i] - polygonVertices[i - 1]) / 2 + polygonVertices[i - 1]);
                _controlPoints.Add((polygonVertices[polygonVertices.Length - 1]
                    - polygonVertices[0]) / 2 + polygonVertices[0]);
            }
            _controlPoints.AddRange(arcPoints);
            UpdatePoints();
            UpdateOnSurfacePoints();
        }

        private void UpdateOnSurfacePoints()
        {
            var objSet = objectSet;
            Vector3 OnSurface(Vector3 point)
            {
                var maxDistance = radius * 20;
                var downRay = new Ray(point, -_normal);
                RaycastHit downHit;
                float downDistance = float.MaxValue;
                if (PWBIO.PWBToolRaycast(downRay, out downHit, out GameObject cd1, maxDistance, -1,
                    paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true, ignoreSceneColliders: true,
                    tags: null, terrainLayers: null, exceptions: objSet,
                    createTempColliders: ShapeManager.settings.paintOnMeshesWithoutCollider))
                    downDistance = downHit.distance;
                else
                {
                    downRay = new Ray(point + normal * maxDistance, -_normal);
                    if (PWBIO.PWBToolRaycast(downRay, out downHit, out GameObject cd2,
                        maxDistance * 2, -1, paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true,
                        ignoreSceneColliders: true, tags: null, terrainLayers: null, exceptions: objSet,
                        createTempColliders: ShapeManager.settings.paintOnMeshesWithoutCollider))
                        downDistance = downHit.distance;
                }
                if (downDistance >= float.MaxValue) return point;
                return downHit.point;
            }
            void AddPoints(Vector3 p0, Vector3 p1)
            {
                var segment = p1 - p0;
                var segmentLength = segment.magnitude;
                var pointCount = Mathf.CeilToInt(segmentLength / 0.25f);
                var delta = segment.normalized * (segmentLength / pointCount);
                _onSurfacePoints.Add(OnSurface(p0));
                for (int i = 0; i < pointCount - 1; ++i)
                {
                    var p = p0 + delta;
                    _onSurfacePoints.Add(OnSurface(p));
                    p0 = p;
                }
            }

            var polygonVertices = vertices.ToList();
            polygonVertices.Add(polygonVertices[0]);
            _onSurfacePoints.Clear();
            for (int i = 0; i < polygonVertices.Count - 1; ++i) AddPoints(polygonVertices[i], polygonVertices[i + 1]);
            if (_onSurfacePoints.Count > 0) _onSurfacePoints.Add(_onSurfacePoints[0]);
        }
        public ShapeData() : base() { }

        public ShapeData((GameObject, int)[] objects, long initialBrushId, ShapeData shapeData)
            : base(objects, initialBrushId, shapeData) { }

        
        private static ShapeData _instance = null;
        public static ShapeData instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ShapeData();
                    _instance._settings = ShapeManager.settings;
                }
                return _instance;
            }
        }

        private void CopyShapeData(ShapeData other)
        {
            _radius = other._radius;
            _arcAngle = other._arcAngle;
            _normal = other._normal;
            _plane = other._plane;
            _firstVertexIdxAfterIntersection = other._firstVertexIdxAfterIntersection;
            _lastVertexIdxBeforeIntersection = other._lastVertexIdxBeforeIntersection;
            _arcIntersections = other._arcIntersections.ToArray();
            _circleSideCount = other._circleSideCount;
        }

        public override void Copy(PersistentData<ShapeToolName, ShapeSettings, ControlPoint> other)
        {
            base.Copy(other);
            var otherShapeData = other as ShapeData;
            if (otherShapeData == null) return;
            CopyShapeData(otherShapeData);
        }

        public ShapeData Clone()
        {
            var clone = new ShapeData();
            base.Clone(clone);
            clone.CopyShapeData(this);
            return clone;
        }
    }
}
#pragma warning restore UDR0001
