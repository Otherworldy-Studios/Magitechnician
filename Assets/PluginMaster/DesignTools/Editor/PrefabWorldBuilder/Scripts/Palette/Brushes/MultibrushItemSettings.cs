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
    public class MultibrushItemSettings : BrushSettings
    {
        [SerializeField] private bool _overwriteSettings = false;
        [SerializeField] private string _guid = string.Empty;
        [SerializeField] private string _prefabPath = string.Empty;
        [SerializeField] private float _frequency = 1;
        [SerializeField] private long _parentId = -1;
        [SerializeField] private string _paletteName = string.Empty;
        [SerializeField] private bool _overwriteThumbnailSettings = false;
        [SerializeField] private bool _includeInThumbnail = true;
        [SerializeField] private bool _isAsset2D = false;

        //[SerializeField] private Vector3 _boundingBoxCenterOffset = Vector3.zero;
        //[SerializeField] private Vector3 _boundingBoxScale = Vector3.one;

        private Vector3[] _bottomVertices = null;
        private float _bottomMagnitude = 0;
        private float _height = 1f;
        private Vector3 _size = Vector3.zero;
        private int _trianglesCount = 0;
        private GameObject _prefab = null;

        private System.Collections.Generic.Dictionary<Vector3, (Vector3[] vertices, float magnitude)>
            _furthestVerticesInDirection = new System.Collections.Generic.Dictionary<Vector3,
                (Vector3[] vertices, float magnitude)>();
        private System.Collections.Generic.List<Vector3> _directionList = new System.Collections.Generic.List<Vector3>();

        [System.NonSerialized] private MultibrushSettings _parentSettings = null;
        public MultibrushSettings parentSettings
        {
            get
            {
                if (_parentSettings == null) _parentSettings = PaletteManager.GetBrushById(_parentId);
                return _parentSettings;
            }
            set
            {
                if (value == null)
                {
                    _parentId = -1;
                    _parentSettings = null;
                    return;
                }
                _parentSettings = value;
                _parentId = value.id;
            }
        }
        protected override void OnDataChanged()
        {
            base.OnDataChanged();
            SavePalette();
        }
        private void SavePalette()
        {
            if (parentSettings == null) return;
            parentSettings.SavePalette();
        }

        public static int GetTrianglesCount(Transform transform)
        {
            var meshFilters = transform.GetComponentsInChildren<MeshFilter>(includeInactive: false);
            var result = 0;
            foreach (var filter in meshFilters)
            {
                if (filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; i++)
                    result += (int)(mesh.GetIndexCount(i) / 3);
            }
            var skinnedMeshRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: false);
            foreach (var renderer in skinnedMeshRenderers)
            {
                if (renderer.sharedMesh == null) continue;
                var mesh = renderer.sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; i++)
                    result += (int)(mesh.GetIndexCount(i) / 3);
            }
            result = Mathf.Max(result, 2);
            return result;
        }
        public MultibrushItemSettings(GameObject prefab, MultibrushSettings parentSettings) : base()
        {
            SetId();
            _prefab = prefab;
            _parentId = parentSettings.id;
            _parentSettings = parentSettings;
            _paletteName = parentSettings.palette?.name ?? string.Empty;
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_prefab, out _guid, out long localId);
            if (_prefab == null) return;
            _prefabPath = UnityEditor.AssetDatabase.GetAssetPath(_prefab);
            _bottomVertices = BoundsUtils.GetBottomVertices(prefab.transform);
            _trianglesCount = GetTrianglesCount(prefab.transform);
            _height = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation).size.y;
            _size = BoundsUtils.GetBoundsRecursive(prefab.transform).size;
            _bottomMagnitude = BoundsUtils.GetBottomMagnitude(prefab.transform);
            UpdateAssetType();
            if (isAsset2D)
            {
                thumbnailSettings.Copy(ThumbnailSettings.GetDefaultTAsset2DThumbnailSettings());
                _rotateToTheSurface = false;
            }
            UpdateThumbnail(updateItemThumbnails: false, savePng: true);
        }

        public void InitializeParentSettings(MultibrushSettings parentSettings)
        {
            _parentId = parentSettings.id;
            _parentSettings = parentSettings;
            _paletteName = parentSettings.palette?.name ?? string.Empty;
            this.parentSettings.UpdateTotalFrequency();
        }

        public bool RepairParentId(MultibrushSettings correctParent)
        {
            if (correctParent == null) return false;
            if (_parentId == correctParent.id)
            {
                _parentSettings = correctParent;
                return false;
            }
            _parentId = correctParent.id;
            _parentSettings = correctParent;
            _paletteName = correctParent.palette?.name ?? string.Empty;
            return true;
        }

        public override string thumbnailPath
        {
            get
            {
                if (parentSettings == null) return null;
                var parentPath = parentSettings.thumbnailPath;
                if (parentPath == null) return null;
                var path = parentPath.Insert(parentPath.Length - 4, "_" + id.ToString("X"));
                return path;
            }
        }

        public bool overwriteSettings
        {
            get => _overwriteSettings;
            set
            {
                if (_overwriteSettings == value) return;
                _overwriteSettings = value;
                SavePalette();
            }
        }

        public float frequency
        {
            get => _frequency;
            set
            {
                value = Mathf.Max(value, 0);
                if (_frequency == value) return;
                _frequency = value;
                if (parentSettings != null) parentSettings.UpdateTotalFrequency();
                OnDataChanged();
            }
        }
        public GameObject prefab
        {
            get
            {
                if (_prefab == null)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(_guid);
                    void CheckMissingScripts(GameObject go)
                    {
                        if (PrefabUtils.HasMissingScripts(prefab))
                        {
                            var pathInPalette = string.Empty;
                            if (!string.IsNullOrEmpty(_paletteName))
                                pathInPalette += $" in palette: '{_paletteName}'";
                            Debug.LogWarning($"[PWB Palette] prefab '{go.name}'{pathInPalette} has missing scripts." +
                                $" Prefab Path: '{_prefabPath}'");
                        }
                    }
                    if (!string.IsNullOrEmpty(path))
                    {
                        _prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (_prefab != null)
                        {
                            _prefabPath = path;
                            CheckMissingScripts(_prefab);
                            return _prefab;
                        }
                    }
                    if (_prefab == null && !string.IsNullOrEmpty(_prefabPath))
                    {
                        _prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
                        if (_prefab != null)
                        {
                            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_prefab, out _guid, out long localId);
                            CheckMissingScripts(_prefab);
                            return _prefab;
                        }
                    }
                    Debug.LogWarning($"Missing prefab at: {_prefabPath}. " +
                        "It is recommended to run 'Cleanup Palette' from the palette settings menu.");
                    return null;
                }
                else
                {
                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(_prefab);
                    if (string.IsNullOrEmpty(assetPath)) _prefab = null;
                    else
                    {
                        _prefabPath = assetPath;
                        return _prefab;
                    }
                }
                return _prefab;
            }
        }
        public string guid => _guid;
        public string prefabPath => _prefabPath;
        public override float surfaceDistance
            => _overwriteSettings || parentSettings == null ? base.surfaceDistance : parentSettings.surfaceDistance;

        public override bool randomSurfaceDistance
            => _overwriteSettings || parentSettings == null
            ? base.randomSurfaceDistance : parentSettings.randomSurfaceDistance;

        public override RandomUtils.Range randomSurfaceDistanceRange
            => _overwriteSettings || parentSettings == null
            ? base.randomSurfaceDistanceRange : parentSettings.randomSurfaceDistanceRange;

        public override bool embedInSurface
        {
            get => _overwriteSettings || parentSettings == null ? base.embedInSurface
                : parentSettings.embedInSurface;
            set
            {
                if (_embedInSurface == value) return;
                _embedInSurface = value;
                if (_embedInSurface) UpdateBottomVertices();
            }
        }

        public override bool embedAtPivotHeight
        {
            get => _overwriteSettings || parentSettings == null ? base.embedAtPivotHeight : parentSettings.embedAtPivotHeight;
            set
            {
                if (_embedAtPivotHeight == value) return;
                _embedAtPivotHeight = value;
            }
        }

        public override Vector3 localPositionOffset
            => _overwriteSettings || parentSettings == null ? base.localPositionOffset : parentSettings.localPositionOffset;
        public override bool rotateToTheSurface
            => _overwriteSettings || parentSettings == null ? base.rotateToTheSurface : parentSettings.rotateToTheSurface;
        public override Vector3 eulerOffset
            => _overwriteSettings || parentSettings == null ? base.eulerOffset : parentSettings.eulerOffset;
        public override bool addRandomRotation
            => _overwriteSettings || parentSettings == null ? base.addRandomRotation : parentSettings.addRandomRotation;
        public override RandomUtils.Range3 randomEulerOffset
            => _overwriteSettings || parentSettings == null ? base.randomEulerOffset : parentSettings.randomEulerOffset;
        public override float rotationFactor
            => _overwriteSettings || parentSettings == null ? base.rotationFactor : parentSettings.rotationFactor;
        public override bool rotateInMultiples
            => _overwriteSettings || parentSettings == null ? base.rotateInMultiples : parentSettings.rotateInMultiples;
        public override bool alwaysOrientUp
            => _overwriteSettings || parentSettings == null ? base.alwaysOrientUp : parentSettings.alwaysOrientUp;
        public override bool separateScaleAxes
            => _overwriteSettings || parentSettings == null ? base.separateScaleAxes : parentSettings.separateScaleAxes;
        public override Vector3 scaleMultiplier
            => _overwriteSettings || parentSettings == null ? base.scaleMultiplier : parentSettings.scaleMultiplier;
        public override RandomUtils.Range3 randomScaleMultiplierRange
            => _overwriteSettings || parentSettings == null ? base.randomScaleMultiplierRange
            : parentSettings.randomScaleMultiplierRange;
        public override bool randomScaleMultiplier
            => _overwriteSettings || parentSettings == null ? base.randomScaleMultiplier
            : parentSettings.randomScaleMultiplier;
        public override FlipAction flipX
            => _overwriteSettings || parentSettings == null ? base.flipX : parentSettings.flipX;
        public override FlipAction flipY
            => _overwriteSettings || parentSettings == null ? base.flipY : parentSettings.flipY;
        public override Vector3 maxScaleMultiplier
            => _overwriteSettings || parentSettings == null ? base.maxScaleMultiplier
            : randomScaleMultiplier ? randomScaleMultiplierRange.max : scaleMultiplier;
        public override Vector3 minScaleMultiplier
            => _overwriteSettings || parentSettings == null ? base.minScaleMultiplier
            : randomScaleMultiplier ? randomScaleMultiplierRange.min : scaleMultiplier;
        public virtual bool overwriteThumbnailSettings
        {
            get => _overwriteThumbnailSettings;
            set
            {
                if (_overwriteThumbnailSettings == value) return;
                _overwriteThumbnailSettings = value;
            }
        }
        public override ThumbnailSettings thumbnailSettings
        {
            get => _overwriteThumbnailSettings || parentSettings == null
                ? base.thumbnailSettings : parentSettings.thumbnailSettings;
            set => base.thumbnailSettings = value;
        }
        public bool includeInThumbnail
        {
            get => _includeInThumbnail;
            set
            {
                if (_includeInThumbnail == value) return;
                _includeInThumbnail = value;
            }
        }

        public override bool isAsset2D { get => _isAsset2D; set => _isAsset2D = value; }

        public void UpdateAssetType() => _isAsset2D = Utils2D.Is2DAsset(prefab);
        public override void Copy(BrushSettings other)
        {
            if (other is MultibrushItemSettings)
            {
                var otherItemSettings = other as MultibrushItemSettings;
                _overwriteSettings = otherItemSettings._overwriteSettings;
                _frequency = otherItemSettings._frequency;
                _overwriteThumbnailSettings = otherItemSettings._overwriteThumbnailSettings;
                _includeInThumbnail = otherItemSettings._includeInThumbnail;
                _isAsset2D = otherItemSettings._isAsset2D;
            }
            base.Copy(other);
        }

        public MultibrushItemSettings() : base() { }

        public MultibrushItemSettings(MultibrushItemSettings other) : base() => Copy(other);

        public override BrushSettings Clone()
        {
            var clone = new MultibrushItemSettings();
            clone._prefab = _prefab;
            clone._guid = _guid;
            if (parentSettings != null)
            {
                clone._parentId = parentSettings.id;
                clone._parentSettings = parentSettings;
            }
            clone._bottomVertices = bottomVertices == null ? null : bottomVertices.ToArray();
            clone._bottomMagnitude = bottomMagnitude;
            clone._height = height;
            clone.Copy(this);
            clone.SetId();
            if (thumbnail != null)
            {
                var texture = new Texture2D(thumbnail.width, thumbnail.height, thumbnail.format, false);
                texture.SetPixels(thumbnail.GetPixels());
                texture.Apply();
                clone.SetCustomThumbnailTexture(texture, savePng: false);
            }
            clone._furthestVerticesInDirection = new System.Collections.Generic.Dictionary<Vector3,
                (Vector3[] vertices, float magnitude)>(_furthestVerticesInDirection);
            clone._directionList = new System.Collections.Generic.List<Vector3>(_directionList);
            return clone;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            _prefabPath = UnityEditor.AssetDatabase.GetAssetPath(_prefab);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            _prefab = null;
        }

        public Vector3[] bottomVertices
        {
            get
            {
                if (_bottomVertices == null) UpdateBottomVertices();
                return _bottomVertices;
            }
        }

        public float bottomMagnitude
        {
            get
            {
                if (prefab == null) return 0f;
                if (_bottomMagnitude == 0) _bottomMagnitude = BoundsUtils.GetBottomMagnitude(prefab.transform);
                return _bottomMagnitude;
            }
        }

        public float height => _height;
        public Vector3 size
        {
            get
            {
                if (prefab == null) return Vector3.zero;
                if (_size == Vector3.zero) _size = BoundsUtils.GetBoundsRecursive(prefab.transform).size;
                return _size;
            }
        }
        public override void UpdateBottomVertices()
        {
            base.UpdateBottomVertices();
            if (prefab == null) return;
            _bottomVertices = BoundsUtils.GetBottomVertices(prefab.transform);
            _height = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation).size.y;
            _size = BoundsUtils.GetBoundsRecursive(prefab.transform).size;
            _bottomMagnitude = BoundsUtils.GetBottomMagnitude(prefab.transform);
        }

        public Vector3[] GetFurthestVerticesInDirection(Vector3 direction, out float magnitude)
        {
            var vertices = BoundsUtils.GetFurthestVertices(prefab.transform, direction, out magnitude);
            return vertices;
        }

        public int trianglesCount
        {
            get
            {
                if(_trianglesCount > 2) return _trianglesCount; 
                if (prefab == null) return 0;
                _trianglesCount = GetTrianglesCount(prefab.transform);
                return _trianglesCount;
            }
        }
        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode();
            hashCode = hashCode * -1521134295 + _overwriteSettings.GetHashCode();
            hashCode = hashCode * -1521134295 + _guid.GetHashCode();
            hashCode = hashCode * -1521134295 + _prefabPath.GetHashCode();
            hashCode = hashCode * -1521134295 + _frequency.GetHashCode();
            hashCode = hashCode * -1521134295 + _parentId.GetHashCode();
            hashCode = hashCode * -1521134295 + _embedAtPivotHeight.GetHashCode();
            hashCode = hashCode * -1521134295 + _overwriteThumbnailSettings.GetHashCode();
            hashCode = hashCode * -1521134295 + _includeInThumbnail.GetHashCode();
            hashCode = hashCode * -1521134295 + _isAsset2D.GetHashCode();
            return hashCode;
        }
    }
}
#pragma warning restore UDR0001
