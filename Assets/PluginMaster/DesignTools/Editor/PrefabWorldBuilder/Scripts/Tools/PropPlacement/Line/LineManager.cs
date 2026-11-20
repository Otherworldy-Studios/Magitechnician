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
    #region DATA & SETTINGS 
    [System.Serializable]
    public class LineSettings : PaintOnSurfaceToolSettings, IPaintToolSettings
    {
        public enum SpacingType { BOUNDS, CONSTANT }

        [SerializeField] private Vector3 _projectionDirection = Vector3.down;
        [SerializeField] protected bool _objectsOrientedAlongTheLine = true;
        [SerializeField] private AxesUtils.Axis _axisOrientedAlongTheLine = AxesUtils.Axis.X;
        [SerializeField] private SpacingType _spacingType = SpacingType.BOUNDS;
        [SerializeField] private float _gapSize = 0f;
        [SerializeField] private float _spacing = 10f;


        public Vector3 projectionDirection
        {
            get => _projectionDirection;
            set
            {
                if (_projectionDirection == value) return;
                _projectionDirection = value;
                OnDataChanged();
            }
        }
        public void UpdateProjectDirection(Vector3 value) => _projectionDirection = value;

        public virtual bool objectsOrientedAlongTheLine
        {
            get => _objectsOrientedAlongTheLine;
            set
            {
                if (_objectsOrientedAlongTheLine == value) return;
                _objectsOrientedAlongTheLine = value;
                OnDataChanged();
            }
        }

        public AxesUtils.Axis axisOrientedAlongTheLine
        {
            get => _axisOrientedAlongTheLine;
            set
            {
                if (_axisOrientedAlongTheLine == value) return;
                _axisOrientedAlongTheLine = value;
                OnDataChanged();
            }
        }

        public SpacingType spacingType
        {
            get => _spacingType;
            set
            {
                if (_spacingType == value) return;
                _spacingType = value;
                OnDataChanged();
            }
        }

        public float spacing
        {
            get => _spacing;
            set
            {
                value = Mathf.Max(value, 0.01f);
                if (_spacing == value) return;
                _spacing = value;
                OnDataChanged();
            }
        }

        public float gapSize
        {
            get => _gapSize;
            set
            {
                if (_gapSize == value) return;
                _gapSize = value;
                OnDataChanged();
            }
        }

        [SerializeField] private PaintToolSettings _paintTool = new PaintToolSettings();
        public Transform parent { get => _paintTool.parent; set => _paintTool.parent = value; }
        public bool overwritePrefabLayer
        { get => _paintTool.overwritePrefabLayer; set => _paintTool.overwritePrefabLayer = value; }
        public int layer { get => _paintTool.layer; set => _paintTool.layer = value; }
        public bool autoCreateParent { get => _paintTool.autoCreateParent; set => _paintTool.autoCreateParent = value; }
        public bool setSurfaceAsParent { get => _paintTool.setSurfaceAsParent; set => _paintTool.setSurfaceAsParent = value; }
        public bool setLastSelectedAsParent
        {
            get => _paintTool.setLastSelectedAsParent;
            set => _paintTool.setLastSelectedAsParent = value;
        }
        public bool createSubparentPerPalette
        {
            get => _paintTool.createSubparentPerPalette;
            set => _paintTool.createSubparentPerPalette = value;
        }
        public bool createSubparentPerTool
        {
            get => _paintTool.createSubparentPerTool;
            set => _paintTool.createSubparentPerTool = value;
        }
        public bool createSubparentPerBrush
        {
            get => _paintTool.createSubparentPerBrush;
            set => _paintTool.createSubparentPerBrush = value;
        }
        public bool createSubparentPerPrefab
        {
            get => _paintTool.createSubparentPerPrefab;
            set => _paintTool.createSubparentPerPrefab = value;
        }
        public bool overwriteBrushProperties
        { get => _paintTool.overwriteBrushProperties; set => _paintTool.overwriteBrushProperties = value; }
        public BrushSettings brushSettings => _paintTool.brushSettings;
        public bool overwriteParentingSettings
        {
            get => _paintTool.overwriteParentingSettings;
            set => _paintTool.overwriteParentingSettings = value;
        }
        public IToolParentingSettings GetParentingSettings() => _paintTool as ToolParentingSettings;
        public LineSettings() : base() => _paintTool.OnDataChanged += DataChanged;

        public override void DataChanged()
        {
            base.DataChanged();
            UpdateStroke();
            UnityEditor.SceneView.RepaintAll();
        }

        protected virtual void UpdateStroke() => PWBIO.UpdateStroke();

        public override void Copy(IToolSettings other)
        {
            var otherLineSettings = other as LineSettings;
            if (otherLineSettings == null) return;
            base.Copy(other);
            _projectionDirection = otherLineSettings._projectionDirection;
            _objectsOrientedAlongTheLine = otherLineSettings._objectsOrientedAlongTheLine;
            _axisOrientedAlongTheLine = otherLineSettings._axisOrientedAlongTheLine;
            _spacingType = otherLineSettings._spacingType;
            _spacing = otherLineSettings._spacing;
            _paintTool.Copy(otherLineSettings._paintTool);
            _gapSize = otherLineSettings._gapSize;
        }
    }

    [System.Serializable]
    public class LineSegment
    {
        public enum SegmentType { STRAIGHT, CURVE }
        public SegmentType type = SegmentType.CURVE;
        [SerializeField]
        private System.Collections.Generic.List<LinePoint> _linePoints = new System.Collections.Generic.List<LinePoint>();

        public Vector3[] points => _linePoints.Select(p => p.position).ToArray();
        public float[] scales => _linePoints.Select(p => p.scale).ToArray();

        public void AddPoint(Vector3 position, float scale = 0.25f) => _linePoints.Add(new LinePoint(position, scale));
    }

    [System.Serializable]
    public class LinePoint : ControlPoint
    {
        public LineSegment.SegmentType type = LineSegment.SegmentType.CURVE;
        public float scale = 0.25f;
        public LinePoint() { }
        public LinePoint(Vector3 position = new Vector3(), float scale = 0.25f,
             LineSegment.SegmentType type = LineSegment.SegmentType.CURVE)
            : base(position) => (this.type, this.scale) = (type, scale);
        public LinePoint(LinePoint other) : base((ControlPoint)other) => Copy(other);
        public override void Copy(ControlPoint other)
        {
            base.Copy(other);
            var otherLinePoint = other as LinePoint;
            if (otherLinePoint == null) return;
            type = otherLinePoint.type;
            scale = otherLinePoint.scale;
        }
    }

    [System.Serializable]
    public class LineData : PersistentData<LineToolName, LineSettings, LinePoint>
    {
        [SerializeField] private bool _closed = false;

        private float _lenght = 0f;
        private System.Collections.Generic.List<Vector3> _midpoints = new System.Collections.Generic.List<Vector3>();
        private System.Collections.Generic.List<Vector3> _pathPoints = new System.Collections.Generic.List<Vector3>();
        private System.Collections.Generic.List<Vector3> _onSurfacePathPoints = new System.Collections.Generic.List<Vector3>();
        public override ToolController.ToolState state
        {
            get => base.state;
            set
            {
                if (state == value) return;
                base.state = value;
                UpdatePath(forceUpdate: false, updateOnSurfacePoints: false);
            }
        }
        public override bool SetPoint(int idx, Vector3 value, bool registerUndo, bool selectAll, bool moveSelection = true)
        {
            if (base.SetPoint(idx, value, registerUndo, selectAll, moveSelection))
            {
                UpdatePath(forceUpdate: false, updateOnSurfacePoints: true);
                return true;
            }
            return false;
        }

        public void SetRotatedPoint(int idx, Vector3 value, bool registerUndo)
            => base.SetPoint(idx, value, registerUndo, selectAll: false, moveSelection: false);
        public void AddPoint(Vector3 point, bool registerUndo = true)
        {
            var linePoint = new LinePoint(point);
            base.AddPoint(linePoint, registerUndo);
            UpdatePath(forceUpdate: false, updateOnSurfacePoints: true);
        }

        protected override void UpdatePoints(bool deserializing = false)
        {
            base.UpdatePoints();
            UpdatePath(forceUpdate: false, updateOnSurfacePoints: !deserializing);
            if (!deserializing && ToolController.editMode) PWBIO.ApplyPersistentLine(this);
        }
        public void ToggleSegmentType()
        {
            ToolProperties.RegisterUndo(COMMAND_NAME);
            for (int i = 0; i < _selection.Count; ++i)
            {
                var idx = _selection[i];
                _controlPoints[idx].type = _controlPoints[idx].type == LineSegment.SegmentType.CURVE
                    ? LineSegment.SegmentType.STRAIGHT : LineSegment.SegmentType.CURVE;
            }
        }
        public LineSegment[] GetSegments()
        {
            var segments = new System.Collections.Generic.List<LineSegment>();
            if (_controlPoints == null || _controlPoints.Count == 0) return segments.ToArray();
            var type = _controlPoints[0].type;
            for (int i = 0; i < pointsCount; ++i)
            {
                var segment = new LineSegment();
                segments.Add(segment);
                segment.type = type;
                segment.AddPoint(_controlPoints[i].position);

                do
                {
                    ++i;
                    if (i >= pointsCount) break;
                    type = _controlPoints[i].type;
                    if (type == segment.type) segment.AddPoint(_controlPoints[i].position);
                } while (type == segment.type);
                if (i >= pointsCount) break;
                i -= 2;
            }
            if (_closed)
            {
                if (_controlPoints[0].type == _controlPoints.Last().type)
                    segments.Last().AddPoint(_controlPoints[0].position);
                else
                {
                    var segment = new LineSegment();
                    segment.type = _controlPoints[0].type;
                    segment.AddPoint(_controlPoints.Last().position);
                    segment.AddPoint(_controlPoints[0].position);
                    segments.Add(segment);
                }
            }
            return segments.ToArray();
        }

        public void ToggleClosed()
        {
            ToolProperties.RegisterUndo(COMMAND_NAME);
            _closed = !_closed;
        }

        public bool closed
        {
            get => _closed;
            set => _closed = value;
        }

        public override void ToggleSelection()
        {
            base.ToggleSelection();
            if (isSelected) SelectAll();
            else ClearSelection();
            UnityEditor.SceneView.RepaintAll();
        }
        protected override void Initialize()
        {
            base.Initialize();
            for (int i = 0; i < 2; ++i) _controlPoints.Add(new LinePoint(Vector3.zero));
            deserializing = true;
            UpdatePoints(deserializing);
            deserializing = false;
        }
        public LineData() : base() { }
        public LineData((GameObject, int)[] objects, long initialBrushId, LineData lineData)
            : base(objects, initialBrushId, lineData) { }

        
        private static LineData _instance = null;
        public static LineData instance
        {
            get
            {
                if (_instance == null) _instance = new LineData();
                if (_instance.points == null || _instance.points.Length == 0)
                {
                    _instance.Initialize();
                    _instance._settings = LineManager.settings;
                }
                return _instance;
            }
        }

        private void CopyLineData(LineData other)
        {
            _closed = other._closed;
            _lenght = other.lenght;
            _midpoints = other._midpoints.ToList();
            _pathPoints = other._pathPoints.ToList();
        }

        public LineData Clone()
        {
            var clone = new LineData();
            base.Clone(clone);
            clone.CopyLineData(this);
            return clone;
        }
        public override void Copy(PersistentData<LineToolName, LineSettings, LinePoint> other)
        {
            base.Copy(other);
            var otherLineData = other as LineData;
            if (otherLineData == null) return;
            CopyLineData(otherLineData);
        }
        private float GetLineLength(Vector3[] points, out float[] lengthFromFirstPoint)
        {
            float lineLength = 0f;
            lengthFromFirstPoint = new float[points.Length];
            var segmentLength = new float[points.Length];
            lengthFromFirstPoint[0] = 0f;
            for (int i = 1; i < points.Length; ++i)
            {
                segmentLength[i - 1] = (points[i] - points[i - 1]).magnitude;
                lineLength += segmentLength[i - 1];
                lengthFromFirstPoint[i] = lineLength;
            }
            return lineLength;
        }

        private Vector3[] GetLineMidpoints(Vector3[] points)
        {
            if (points.Length == 0) return new Vector3[0];
            var midpoints = new System.Collections.Generic.List<Vector3>();
            var subSegments = new System.Collections.Generic.List<System.Collections.Generic.List<Vector3>>();
            var pathPoints = _pointPositions;
            bool IsAPathPoint(Vector3 point) => pathPoints.Contains(point);
            subSegments.Add(new System.Collections.Generic.List<Vector3>());
            subSegments.Last().Add(points[0]);
            for (int i = 1; i < points.Length - 1; ++i)
            {
                var point = points[i];
                subSegments.Last().Add(point);
                if (IsAPathPoint(point))
                {
                    subSegments.Add(new System.Collections.Generic.List<Vector3>());
                    subSegments.Last().Add(point);
                }
            }
            subSegments.Last().Add(points.Last());
            Vector3 GetLineMidpoint(Vector3[] subSegmentPoints)
            {
                var midpoint = subSegmentPoints[0];
                float[] lengthFromFirstPoint = null;
                var halfLineLength = GetLineLength(subSegmentPoints, out lengthFromFirstPoint) / 2f;
                for (int i = 1; i < subSegmentPoints.Length; ++i)
                {
                    if (lengthFromFirstPoint[i] < halfLineLength) continue;
                    var dir = (subSegmentPoints[i] - subSegmentPoints[i - 1]).normalized;
                    var localLength = halfLineLength - lengthFromFirstPoint[i - 1];
                    midpoint = subSegmentPoints[i - 1] + dir * localLength;
                    break;
                }
                return midpoint;
            }
            foreach (var subSegment in subSegments) midpoints.Add(GetLineMidpoint(subSegment.ToArray()));
            return midpoints.ToArray();
        }

        public void UpdatePath(bool forceUpdate, bool updateOnSurfacePoints)
        {
            if (!forceUpdate && !ToolController.editMode && state != ToolController.ToolState.EDIT) return;
            _lenght = 0;
            _pathPoints.Clear();
            _midpoints.Clear();
            _onSurfacePathPoints.Clear();
            var segments = GetSegments();
            void AddSegmentPoints(System.Collections.Generic.List<Vector3> pointList, Vector3[] newPoints)
            {
                if (pointList.Count > 0 && pointList.Last() == newPoints[0] && newPoints.Length > 1)
                    for (int i = 1; i < newPoints.Length; ++i) pointList.Add(newPoints[i]);
                else pointList.AddRange(newPoints);
            }
            foreach (var segment in segments)
            {
                var segmentPoints = new Vector3[] { };
                if (segment.type == LineSegment.SegmentType.STRAIGHT) segmentPoints = segment.points.ToArray();
                else segmentPoints = (BezierPath.GetBezierPoints(segment.points, segment.scales)).ToArray();
                AddSegmentPoints(_pathPoints, segmentPoints);
                if (segmentPoints.Length == 0) continue;
                var midpoints = GetLineMidpoints(segmentPoints);
                AddSegmentPoints(_midpoints, midpoints);
            }
            if (!updateOnSurfacePoints) return;
            var objSet = objectSet;
            for (int i = 0; i < _pathPoints.Count; ++i)
            {
                float distance = 10000f;
                if (ToolController.current == ToolController.Tool.LINE && !deserializing)
                {
                    var ray = new Ray(_pathPoints[i] - settings.projectionDirection * distance, settings.projectionDirection);
                    var onSurfacePoint = _pathPoints[i];
                    if (PWBIO.PWBToolRaycast(ray, out RaycastHit hit, out GameObject collider, distance * 2, -1,
                        paintOnPalettePrefabs: false, castOnMeshesWithoutCollider: true, tags: null, terrainLayers: null,
                        exceptions: objSet, sameOriginAsRay: false, origin: _pathPoints[i],
                        createTempColliders: settings.paintOnMeshesWithoutCollider,
                        ignoreSceneColliders: settings.ignoreSceneColliders))
                    {
                        onSurfacePoint = hit.point;
                    }
                    _onSurfacePathPoints.Add(onSurfacePoint);
                }
                if (i == 0) continue;
                _lenght += (_pathPoints[i] - _pathPoints[i - 1]).magnitude;
            }
        }

        public static bool SphereSegmentIntersection(Vector3 segmentStart, Vector3 segmentEnd,
            Vector3 sphereCenter, float sphereRadius, out Vector3 intersection)
        {
            var r = sphereRadius;
            var d = segmentEnd - segmentStart;
            var f = segmentStart - sphereCenter;
            var a = Vector3.Dot(d, d);
            var b = 2 * Vector3.Dot(f, d);
            var c = Vector3.Dot(f, f) - r * r;
            float discriminant = b * b - 4 * a * c;
            float t = -1;
            intersection = segmentStart;
            if (discriminant < 0) return false;
            else
            {
                discriminant = Mathf.Sqrt(discriminant);
                var t1 = (-b - discriminant) / (2 * a);
                var t2 = (-b + discriminant) / (2 * a);
                if (t1 >= 0 && t1 <= 1 && t1 > t2) t = t1;
                else if (t2 >= 0 && t2 <= 1 && t2 > t1) t = t2;
            }
            if (t == -1) return false;
            intersection += d * t;
            return true;
        }
        public static Vector3 NearestPathPoint(int startSegmentIdx, Vector3 startPoint, float minPathLenght,
            Vector3[] pathPoints, out int nearestPointIdx, out float distanceFromNearestPoint)
        {
            nearestPointIdx = pathPoints.Length - 1;
            var result = pathPoints.Last();
            distanceFromNearestPoint = 0f;
            startSegmentIdx = Mathf.Max(startSegmentIdx, 1);
            for (int i = startSegmentIdx; i < pathPoints.Length; ++i)
            {
                var start = pathPoints[i - 1];
                var end = pathPoints[i];
                if(i == pathPoints.Length -1)
                {
                    end = (end - start) * 1000 + start;
                }
                if (SphereSegmentIntersection(start, end, startPoint, minPathLenght, out Vector3 intersection))
                {
                    result = intersection;
                    nearestPointIdx = i - 1;
                    distanceFromNearestPoint = (intersection - pathPoints[nearestPointIdx]).magnitude;
                    return result;
                }
            }

            return result;
        }

        public float lenght => _lenght;
        public Vector3[] pathPoints => _pathPoints.ToArray();
        public Vector3[] onSurfacePathPoints => _onSurfacePathPoints.ToArray();
        public Vector3 lastPathPoint => _pathPoints.Last();
        public Vector3[] midpoints => _midpoints.ToArray();
        public Vector3 lastTangentPos { get; set; }

        public bool showHandles { get; set; }
    }

    public class LineToolName : IToolName { public string value => "Line"; }

    [System.Serializable]
    public class LineSceneData : SceneData<LineToolName, LineSettings, LinePoint, LineData>
    {
        public LineSceneData() : base() { }
        public LineSceneData(string sceneGUID) : base(sceneGUID) { }
    }

    [System.Serializable]
    public class LineManager : PersistentToolControllerBase<LineToolName, LineSettings,
        LinePoint, LineData, LineSceneData, LineManager>
    {
        public enum EditModeType
        {
            NODES,
            LINE_POSE
        }

        
        public static EditModeType editModeType { get; set; }
        public static void ToggleEditModeType()
        {
            editModeType = editModeType == EditModeType.NODES ? EditModeType.LINE_POSE : EditModeType.NODES;
            ToolProperties.RepainWindow();
        }
    }
    #endregion
}
#pragma warning restore UDR0001
