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
    [System.Serializable]
    public class TerrainFlatteningSettings
    {
        [SerializeField] private float _hardness = 0f;
        [SerializeField] private float _padding = 0f;
        [SerializeField] private bool _clearTrees = true;
        [SerializeField] private bool _clearDetails = true;
        private Vector2 _coreSize = Vector2.one;
        private Vector2 _density = Vector2.zero;
        private float _angle = 0;
        private bool _updateHeightmap = true;
        private float[,] _heightmap = null;
        private Vector2 _maskWorldSize = Vector2.one;

        public TerrainFlatteningSettings() { }

        public float hardness
        {
            get => _hardness;
            set
            {
                if (_hardness == value) return;
                _hardness = value;
                _updateHeightmap = true;
                PWBCore.SetSavePending();
            }
        }
        public float padding
        {
            get => _padding;
            set
            {
                value = Mathf.Max(value, 0);
                if (_padding == value) return;
                _padding = value;
                _updateHeightmap = true;
                PWBCore.SetSavePending();
            }
        }
        public bool clearTrees
        {
            get => _clearTrees;
            set
            {
                if (_clearTrees == value) return;
                _clearTrees = value;
                PWBCore.SetSavePending();
            }
        }
        public bool clearDetails
        {
            get => _clearDetails;
            set
            {
                if (_clearDetails == value) return;
                _clearDetails = value;
                PWBCore.SetSavePending();
            }
        }
        public Vector2 size
        {
            get => _coreSize;
            set
            {
                if (_coreSize == value) return;
                _coreSize = value;
                _updateHeightmap = true;
            }
        }
        public Vector2 density
        {
            set
            {
                if (_density == value) return;
                _density = value;
                _updateHeightmap = true;
            }
        }
        public float angle
        {
            get => _angle;
            set
            {
                if (_angle == value) return;
                _angle = value;
                _updateHeightmap = true;
            }
        }
        public float[,] heightmap
        {
            get
            {
                if (_updateHeightmap || _heightmap == null) UpdateHeightmap();
                return _heightmap;
            }
        }
        public Vector2 maskWorldSize => _maskWorldSize;

        private void UpdateHeightmap()
        {
            _updateHeightmap = false;
            var coreWithPaddingSize = _coreSize + Vector2.one * _padding * 2;
            var coreMapSize = new Vector2Int(
                Mathf.RoundToInt(coreWithPaddingSize.x * _density.x),
                Mathf.RoundToInt(coreWithPaddingSize.y * _density.y)
            );

            float blendWidth = (_coreSize.x + _coreSize.y) / 2f * (1f - _hardness);
            blendWidth = Mathf.Max(blendWidth, 1f / Mathf.Min(_density.x, _density.y));
            var blendMapSize = new Vector2Int(
                Mathf.RoundToInt(blendWidth * _density.x),
                Mathf.RoundToInt(blendWidth * _density.y)
            );

            var mapSize = coreMapSize + blendMapSize * 2;
            _maskWorldSize = new Vector2(mapSize.x / _density.x, mapSize.y / _density.y);

            var mask = new float[mapSize.x, mapSize.y];
            FillCoreRect(mask, mapSize, coreMapSize, blendMapSize);
            FillBlendRect(mask, mapSize, coreMapSize, blendMapSize);
            if (_angle == 0)
            {
                _heightmap = mask;
                return;
            }
            _heightmap = RotateAndSmooth(mask, mapSize);
        }

        private void FillCoreRect(float[,] mask, Vector2Int mapSize, Vector2Int coreMapSize, Vector2Int blendMapSize)
        {
            int coreStartX = blendMapSize.x;
            int coreStartY = blendMapSize.y;
            int coreEndX = coreStartX + coreMapSize.x;
            int coreEndY = coreStartY + coreMapSize.y;
            for (int y = coreStartY; y < coreEndY; ++y)
                for (int x = coreStartX; x < coreEndX; ++x)
                    mask[x, y] = 1f;
        }

        private void FillBlendRect(float[,] mask, Vector2Int mapSize, Vector2Int coreMapSize, Vector2Int blendMapSize)
        {
            int coreStartX = blendMapSize.x;
            int coreStartY = blendMapSize.y;
            int coreEndX = coreStartX + coreMapSize.x;
            int coreEndY = coreStartY + coreMapSize.y;
            float blendMax = Mathf.Max(1, blendMapSize.x);
            for (int y = 0; y < mapSize.y; ++y)
            {
                for (int x = 0; x < mapSize.x; ++x)
                {
                    if (mask[x, y] == 1f && y > coreStartY && y < coreEndY
                        && x > coreStartX && x < coreEndX)
                        x = Mathf.Max(x, coreEndX - 1);
                    if (mask[x, y] == 1f) continue;
                    int dx = 0;
                    if (x < coreStartX) dx = coreStartX - x;
                    else if (x >= coreEndX) dx = x - (coreEndX - 1);
                    int dy = 0;
                    if (y < coreStartY) dy = coreStartY - y;
                    else if (y >= coreEndY) dy = y - (coreEndY - 1);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float norm = 1f - Mathf.Clamp01(dist / blendMax);
                    mask[x, y] = ParametricBlend(norm);
                }
            }
        }

        private float ParametricBlend(float t)
        {
            if (t > 1) return 1;
            if (t < 0) return 0;
            float tSquared = t * t;
            return tSquared / (2.0f * (tSquared - t) + 1.0f);
        }

        private float[,] RotateAndSmooth(float[,] mask, Vector2Int mapSize)
        {
            var angleRad = _angle * Mathf.Deg2Rad;
            var cos = Mathf.Cos(angleRad);
            var sin = Mathf.Sin(angleRad);
            var aspect = _density.x / _density.y;
            Vector2Int RotatePoint(Vector2 centerToPoint)
            {
                if (_angle == 0) return new Vector2Int(Mathf.RoundToInt(centerToPoint.x), Mathf.RoundToInt(centerToPoint.y));
                var result = Vector2Int.zero;
                centerToPoint.y = centerToPoint.y * aspect;
                result.x = Mathf.RoundToInt((centerToPoint.x * cos - centerToPoint.y * sin));
                result.y = Mathf.RoundToInt((centerToPoint.x * sin + centerToPoint.y * cos) / aspect);
                return result;
            }
            var centerToCorner1 = new Vector2Int(Mathf.CeilToInt(mapSize.x / 2f), Mathf.CeilToInt(mapSize.y / 2f));
            var rotatedCorner1 = RotatePoint(centerToCorner1);
            rotatedCorner1 = new Vector2Int(Mathf.Abs(rotatedCorner1.x), Mathf.Abs(rotatedCorner1.y));
            var centerToCorner2 = new Vector2Int(-Mathf.CeilToInt(mapSize.x / 2f), Mathf.CeilToInt(mapSize.y / 2f));
            var rotatedCorner2 = RotatePoint(centerToCorner2);
            rotatedCorner2 = new Vector2Int(Mathf.Abs(rotatedCorner2.x), Mathf.Abs(rotatedCorner2.y));
            var rotatedCorner = Vector2Int.Max(rotatedCorner1, rotatedCorner2);
            var rotationPadding = Vector2Int.Max(rotatedCorner - centerToCorner1, Vector2Int.zero);
            var rotatedHeightmapSize = mapSize + rotationPadding * 2;
            var rotated = new float[rotatedHeightmapSize.x, rotatedHeightmapSize.y];
            Vector2Int ClampPoint(Vector2Int point) => new Vector2Int(Mathf.Clamp(point.x, 0, rotatedHeightmapSize.x - 1),
                    Mathf.Clamp(point.y, 0, rotatedHeightmapSize.y - 1));
            void SetHeight(Vector2Int point, float value)
            {
                var clampPoint = ClampPoint(point);
                rotated[clampPoint.x, clampPoint.y] = value;
                var points = new Vector2Int[] { ClampPoint(point + Vector2Int.up), ClampPoint(point + Vector2Int.down),
            ClampPoint(point + Vector2Int.left), ClampPoint(point + Vector2Int.right)};
                foreach (var p in points)
                    rotated[p.x, p.y] = rotated[p.x, p.y] < 0.0001 ? value : (rotated[p.x, p.y] * 6 + value) / 7;
            }
            var unrotatedCenter = new Vector2Int(Mathf.FloorToInt(mapSize.x / 2f), Mathf.FloorToInt(mapSize.y / 2f));
            var center = new Vector2Int(Mathf.FloorToInt(rotatedHeightmapSize.x / 2f),
                Mathf.FloorToInt(rotatedHeightmapSize.y / 2f));
            for (int i = 0; i < mapSize.y; ++i)
            {
                for (int j = 0; j < mapSize.x; ++j)
                {
                    var h = mask[j, i];
                    var point = new Vector2(j, i);
                    var centerToPoint = point - unrotatedCenter;
                    var rotatedPoint = RotatePoint(centerToPoint) + center;
                    SetHeight(rotatedPoint, h);
                }
            }
            var smoothMap = new float[rotatedHeightmapSize.x, rotatedHeightmapSize.y];
            for (int i = 0; i < rotatedHeightmapSize.x; ++i)
            {
                for (int j = 0; j < rotatedHeightmapSize.y; ++j)
                {
                    var count = 0;
                    var sum = 0f;
                    var corners = new float[] { i == 0 || j == 0 ? 0 : rotated[i-1, j-1],
                i == rotatedHeightmapSize.x-1 || j == 0? 0 :rotated[i+1, j -1],
                i == 0 || j == rotatedHeightmapSize.y-1 ? 0 :rotated[i-1, j+1],
                i == rotatedHeightmapSize.x-1 || j == rotatedHeightmapSize.y-1 ? 0 : rotated[i+1, j+1] };
                    for (int n = 0; n < 4; ++n)
                    {
                        if (corners[n] < 0.0001) continue;
                        ++count;
                        sum += corners[n];
                    }
                    var neighbors = new float[] { i == 0 ? 0 : rotated[i - 1, j],
                i == rotatedHeightmapSize.x -1 ? 0 :rotated[i + 1, j],
                j == 0 ? 0 : rotated[i, j - 1], j == rotatedHeightmapSize.y -1 ? 0 : rotated[i, j + 1] };
                    for (int n = 0; n < 4; ++n)
                    {
                        if (neighbors[n] < 0.0001) continue;
                        count += 2;
                        sum += neighbors[n] * 2;
                    }
                    if (count == 0)
                    {
                        smoothMap[i, j] = rotated[i, j];
                        continue;
                    }
                    if (!(rotated[i, j] < 0.0001 && ((neighbors[0] > 0.0001 && neighbors[1] > 0.0001)
                        || (neighbors[2] > 0.0001 && neighbors[3] > 0.0001))))
                    {
                        sum += rotated[i, j] * 3;
                        count += 3;
                    }
                    var avg = sum / count;
                    smoothMap[i, j] = avg;
                }
            }
            return smoothMap;
        }
    }

    [System.Serializable]
    public class PinSettings : PaintOnSurfaceToolSettings, IPaintToolSettings, IToolParentingSettings
    {
        [SerializeField] private bool _repeat = false;
        [SerializeField] private TerrainFlatteningSettings _flatteningSettings = new TerrainFlatteningSettings();
        [SerializeField] private bool _flattenTerrain = false;
        [SerializeField] private bool _avoidOverlapping = false;
        [SerializeField] private bool _snapRotationToGrid = false;
        public bool repeat
        {
            get => _repeat;
            set
            {
                if (_repeat == value) return;
                _repeat = value;
                OnDataChanged();
            }
        }
        public TerrainFlatteningSettings flatteningSettings => _flatteningSettings;
        public bool flattenTerrain
        {
            get => _flattenTerrain;
            set
            {
                if (_flattenTerrain == value) return;
                _flattenTerrain = value;
                PWBCore.SetSavePending();
            }
        }

        public bool avoidOverlapping
        {
            get => _avoidOverlapping;
            set
            {
                if (_avoidOverlapping == value) return;
                _avoidOverlapping = value;
                OnDataChanged();
            }
        }

        public bool snapRotationToGrid
        {
            get => _snapRotationToGrid;
            set
            {
                if (_snapRotationToGrid == value) return;
                _snapRotationToGrid = value;
                OnDataChanged();
            }
        }
        #region PAINT TOOL
        [SerializeField] private PaintToolSettings _paintTool = new PaintToolSettings();
        public Transform parent { get => _paintTool.parent; set => _paintTool.parent = value; }
        public bool overwritePrefabLayer
        {
            get => _paintTool.overwritePrefabLayer;
            set => _paintTool.overwritePrefabLayer = value;
        }
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
        {
            get => _paintTool.overwriteBrushProperties;
            set => _paintTool.overwriteBrushProperties = value;
        }
        public BrushSettings brushSettings => _paintTool.brushSettings;
        public bool overwriteParentingSettings
        {
            get => _paintTool.overwriteParentingSettings;
            set => _paintTool.overwriteParentingSettings = value;
        }
        public PinSettings() : base() => _paintTool.OnDataChanged += DataChanged;
        public IToolParentingSettings GetParentingSettings() => _paintTool as ToolParentingSettings;
        #endregion

        public override void Copy(IToolSettings other)
        {
            var otherPinSettings = other as PinSettings;
            if (otherPinSettings == null) return;
            base.Copy(other);
            _paintTool.Copy(otherPinSettings._paintTool);
            _repeat = otherPinSettings._repeat;
            _flattenTerrain = otherPinSettings._flattenTerrain;
            _snapRotationToGrid = otherPinSettings._snapRotationToGrid;
        }
        public override void DataChanged()
        {
            base.DataChanged();
            BrushstrokeManager.UpdateBrushstroke();
        }
    }

    [System.Serializable]
    public class PinManager : ToolControllerBase<PinSettings, PinManager>
    {

        private static float _rotationSnapValueStatic = 5f;
        [SerializeField] private float _rotationSnapValue = _rotationSnapValueStatic;

        public static float rotationSnapValue
        {
            get => _rotationSnapValueStatic;
            set
            {
                if (_rotationSnapValueStatic == value) return;
                _rotationSnapValueStatic = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }


        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            _rotationSnapValue = _rotationSnapValueStatic;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            _rotationSnapValueStatic = _rotationSnapValue;
        }
    }
}
#pragma warning restore UDR0001
