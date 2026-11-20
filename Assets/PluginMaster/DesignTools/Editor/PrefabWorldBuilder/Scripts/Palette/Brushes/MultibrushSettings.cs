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
    public class MultibrushSettings : BrushSettings
    {
        public enum FrequencyMode { RANDOM, PATTERN }
        [SerializeField] private string _name = null;
        [SerializeField]
        private System.Collections.Generic.List<MultibrushItemSettings> _items
            = new System.Collections.Generic.List<MultibrushItemSettings>();
        [SerializeField] private FrequencyMode _frequencyMode = FrequencyMode.RANDOM;
        [SerializeField] private string _pattern = "1...";
        [SerializeField] private bool _restartPatternForEachStroke = true;

        [field: System.NonSerialized] private float _totalFrequency = -1;
        [field: System.NonSerialized] private PatternMachine _patternMachine = null;
        [field: System.NonSerialized] private PaletteData _palette = null;


        public string name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnDataChanged();
            }
        }

        public FrequencyMode frequencyMode
        {
            get => _frequencyMode;
            set
            {
                if (_frequencyMode == value) return;
                _frequencyMode = value;
                OnDataChanged();
            }
        }

        public string pattern
        {
            get => _pattern;
            set
            {
                if (_pattern == value) return;
                _pattern = value;
                OnDataChanged();
            }
        }

        public PatternMachine patternMachine
        {
            get
            {
                if (_patternMachine == null)
                {
                    if (PatternMachine.Validate(_pattern, _items.Count,
                        out PatternMachine.Token[] tokens, out PatternMachine.Token[] endTokens)
                        == PatternMachine.ValidationResult.VALID) _patternMachine = new PatternMachine(tokens, endTokens);
                }
                return _patternMachine;
            }
            set
            {
                _patternMachine = value;
                OnDataChanged();
            }
        }

        public bool restartPatternForEachStroke
        {
            get => _restartPatternForEachStroke;
            set
            {
                if (_restartPatternForEachStroke == value) return;
                _restartPatternForEachStroke = value;
                OnDataChanged();
            }
        }

        public PaletteData palette
        {
            get
            {
                if (_palette == null) _palette = PaletteManager.GetPalette(this);
                return _palette;
            }
            set => _palette = value;
        }

        public override bool isAsset2D
        {
            get => _items.Exists(i => i.isAsset2D);
            set
            {
                foreach (var item in _items) item.isAsset2D = value;
            }
        }

        protected override void OnDataChanged()
        {
            base.OnDataChanged();
            SavePalette();
        }
        public void SavePalette()
        {
            if (palette != null) palette.Save();
        }
        protected MultibrushSettings(GameObject prefab, PaletteData palette) : base()
        {
            SetId();
            this.palette = palette;
            _items.Add(new MultibrushItemSettings(prefab, this));
            _name = prefab.name;
            Copy(palette.brushCreationSettings.defaultBrushSettings);
            thumbnailSettings.Copy(palette.brushCreationSettings.defaultThumbnailSettings);
            if (isAsset2D)
            {
                thumbnailSettings.Copy(ThumbnailSettings.GetDefaultTAsset2DThumbnailSettings());
                _rotateToTheSurface = false;
            }
            UpdateThumbnail(updateItemThumbnails: false, savePng: true);

        }

        public override string thumbnailPath
            => palette == null ? null : palette.thumbnailsFolderPath + "/" + id.ToString("X") + ".png";

        public void AddItem(MultibrushItemSettings item)
        {
            _items.Add(item);
            OnItemCountChange();
        }

        private void RemoveFromPalette()
        {
            if (palette != null) palette.RemoveBrush(this);
        }

        public void RemoveItemAt(int index)
        {
            _items.RemoveAt(index);
            OnItemCountChange();
            if (_items.Count == 0) RemoveFromPalette();
        }

        public void RemoveItem(MultibrushItemSettings item)
        {
            if (!_items.Contains(item)) return;
            _items.Remove(item);
            OnItemCountChange();
            if (_items.Count == 0) RemoveFromPalette();
        }

        public MultibrushItemSettings GetItemAt(int index)
        {
            if (index >= _items.Count) return null;
            var item = _items[index];
            if(item.parentSettings == null) item.InitializeParentSettings(this);
            return _items[index];
        }

        public bool ItemExist(long itemId) => _items.Exists(i => i.id == itemId);
        public MultibrushItemSettings GetItemById(long itemId)
        {
            var items = _items.Where(i => i.id == itemId).ToArray();
            if (items.Length == 0) return null;
            var item = _items[0];
            if (item.parentSettings == null) item.InitializeParentSettings(this);
            return item;
        }

        public void InsertItemAt(MultibrushItemSettings item, int index)
        {
            _items.Insert(index, item);
            OnItemCountChange();
        }

        private void OnItemCountChange()
        {
            UpdateTotalFrequency();
            UpdatePatternMachine();
            PWBCore.staticData.SaveAndUpdateVersion();
            BrushstrokeManager.UpdateBrushstroke();
            SavePalette();
            UpdateThumbnail(updateItemThumbnails: false, savePng: true);
            if (_palette != null) _palette.ClearObjectQuery();
        }

        public void Swap(int fromIdx, int toIdx, ref int[] selection)
            => SelectionUtils.Swap<MultibrushItemSettings>(fromIdx, toIdx, ref selection, _items);

        public MultibrushItemSettings[] items => _items.ToArray();

        public int itemCount => _items.Count;

        public int notNullItemCount => _items.Where(i => i.prefab != null).Count();
        public bool containMissingPrefabs
        {
            get
            {
                foreach (var item in _items)
                    if (item.prefab == null) return true;
                return false;
            }
        }

        public bool allPrefabMissing
        {
            get
            {
                foreach (var item in _items)
                    if (item.prefab != null) return false;
                return true;
            }
        }
        public void UpdateTotalFrequency()
        {
            _totalFrequency = 0;
            foreach (var item in _items) _totalFrequency += item.frequency;
        }

        public float totalFrequency
        {
            get
            {
                if (_totalFrequency == -1) UpdateTotalFrequency();
                return _totalFrequency;
            }
        }
        private int GetNextItemIndex()
        {
            if (frequencyMode == FrequencyMode.RANDOM)
            {
                if (_items.Count == 1) return 0;
                var rand = UnityEngine.Random.Range(0f, totalFrequency);
                float sum = 0;
                for (int i = 0; i < _items.Count; ++i)
                {
                    sum += _items[i].frequency;
                    if (rand <= sum) return i;
                }
                return -1;
            }
            if (_patternMachine == null)
            {
                if (PatternMachine.Validate(_pattern, _items.Count,
                    out PatternMachine.Token[] tokens, out PatternMachine.Token[] endTokens)
                    == PatternMachine.ValidationResult.VALID) _patternMachine = new PatternMachine(tokens, endTokens);
            }
            var result = _patternMachine == null ? -2 : _patternMachine.nextIndex - 1;
            return result;
        }
        private int _currentItemIndex = 0;
        public int nextItemIndex
        {
            get
            {
                _currentItemIndex = GetNextItemIndex();
                return _currentItemIndex;
            }
        }
        public int currentItemIndex
        {
            get
            {
                return _currentItemIndex;
            }
        }
        public void ResetCurrentItemIndex()
        {
            if (frequencyMode == FrequencyMode.RANDOM) return;
            if (_patternMachine == null)
            {
                if (PatternMachine.Validate(_pattern, _items.Count,
                    out PatternMachine.Token[] tokens, out PatternMachine.Token[] endTokens)
                    == PatternMachine.ValidationResult.VALID) _patternMachine = new PatternMachine(tokens, endTokens);
                return;
            }
            _patternMachine.Reset();
            _currentItemIndex = _patternMachine.nextIndex - 1;
        }
        public void SetNextItemIndex()
        {
            _currentItemIndex = GetNextItemIndex();
        }
        public int GetPatternTokenIndex()
        {
            if (frequencyMode == FrequencyMode.RANDOM | _patternMachine == null) return 0;
            return _patternMachine.tokenIndex;
        }
        public void SetPatternTokenIndex(int value)
        {
            if (frequencyMode == FrequencyMode.RANDOM | _patternMachine == null) return;
            _patternMachine.SetTokenIndex(value);
        }
        private void UpdatePatternMachine()
        {
            if (PatternMachine.Validate(_pattern, _items.Count,
                out PatternMachine.Token[] tokens, out PatternMachine.Token[] endTokens)
                != PatternMachine.ValidationResult.VALID)
                _patternMachine = null;
        }

        public override void Copy(BrushSettings other)
        {

            if (other is MultibrushSettings)
            {
                var otherMulti = other as MultibrushSettings;
                _items.Clear();
                foreach (var item in otherMulti._items)
                {
                    var clone = item.Clone() as MultibrushItemSettings;
                    clone.parentSettings = this;
                    _items.Add(clone);
                }
                _name = otherMulti._name;
                _frequencyMode = otherMulti._frequencyMode;
                _pattern = otherMulti._pattern;
                _restartPatternForEachStroke = otherMulti._restartPatternForEachStroke;
                _totalFrequency = otherMulti._totalFrequency;
            }
            base.Copy(other);
        }

        private MultibrushSettings() : base() { }
        public override BrushSettings Clone()
        {
            var clone = new MultibrushSettings();
            clone.SetId();
            clone.Copy(this);
            clone.palette = _palette;
            if (thumbnail != null)
            {
                var texture = new Texture2D(thumbnail.width, thumbnail.height, thumbnail.format, false);
                texture.SetPixels(thumbnail.GetPixels());
                texture.Apply();
                clone.SetCustomThumbnailTexture(texture, savePng: false);
            }
            return clone;
        }

        public MultibrushSettings CloneAndChangePalette(PaletteData palette)
        {
            var clone = new MultibrushSettings();
            clone.SetId();
            clone.Copy(this);
            clone.palette = palette;
            return clone;
        }
        public BrushSettings CloneMainSettings()
        {
            var clone = new BrushSettings();
            clone.Copy(this);
            return clone;
        }

        public void Duplicate(int index)
        {
            var original = _items[index];
            var clone = original.Clone();
            if (clone.thumbnailSettings.useCustomImage)
            {
                clone.thumbnailSettings.useCustomImage = false;
                clone.UpdateThumbnail(updateItemThumbnails: true, savePng: true);
                clone.thumbnailSettings.useCustomImage = true;
                var thumbnailClone = new Texture2D(original.thumbnail.width, original.thumbnail.height);
                thumbnailClone.SetPixels(original.thumbnail.GetPixels());
                thumbnailClone.Apply();
                clone.SetCustomThumbnailTexture(thumbnailClone, savePng: true);
            }
            else clone.UpdateThumbnail(updateItemThumbnails: true, savePng: true);
            _items.Insert(index, clone as MultibrushItemSettings);
            OnItemCountChange();
        }

        public MultibrushSettings[] CreateSingleBrushes()
        {
            if (itemCount <= 1 || palette == null) return null;
            var newBrushes = new System.Collections.Generic.List<MultibrushSettings>();
            foreach (var item in _items)
            {
                if (item.prefab == null) continue;
                var newBrush = new MultibrushSettings();
                newBrush.SetId();
                newBrush.Copy(this);
                newBrush._items.Clear();
                var itemClone = item.Clone() as MultibrushItemSettings;
                itemClone.parentSettings = newBrush;
                newBrush._items.Add(itemClone);
                newBrush.name = item.prefab.name;
                newBrush.palette = palette;
                if (itemClone.thumbnailSettings.useCustomImage && item.thumbnail != null)
                {
                    itemClone.thumbnailSettings.useCustomImage = false;
                    newBrush.UpdateThumbnail(updateItemThumbnails: true, savePng: true);
                    itemClone.thumbnailSettings.useCustomImage = true;
                    var thumbnailClone = new Texture2D(item.thumbnail.width, item.thumbnail.height);
                    thumbnailClone.SetPixels(item.thumbnail.GetPixels());
                    thumbnailClone.Apply();
                    itemClone.SetCustomThumbnailTexture(thumbnailClone, savePng: true);
                }
                else newBrush.UpdateThumbnail(updateItemThumbnails: true, savePng: true);
                newBrushes.Add(newBrush);
            }
            return newBrushes.ToArray();
        }
        public override void UpdateBottomVertices()
        {
            foreach (var item in _items) item.UpdateBottomVertices();
        }

        public override bool embedInSurface
        {
            get => _embedInSurface;
            set
            {
                if (_embedInSurface == value) return;
                _embedInSurface = value;
                if (_embedInSurface) UpdateBottomVertices();
            }
        }
#if UNITY_2021_1_OR_NEWER
        public System.Collections.Generic.HashSet<GameObject> prefabs
            => _items.Select(i => i.prefab).Where(p => p != null).ToHashSet();
#else
        public System.Collections.Generic.HashSet<GameObject> prefabs
            => new System.Collections.Generic.HashSet<GameObject>(_items.Select(i => i.prefab).Where(p => p != null));
#endif
#if UNITY_6000_3_OR_NEWER
        public bool ContainsPrefab(EntityId prefabId)
            => _items.Exists(item => item.prefab != null && item.prefab.GetEntityId() == prefabId);
#else
        public bool ContainsPrefab(int prefabId)
            => _items.Exists(item => item.prefab != null && item.prefab.GetInstanceID() == prefabId);
#endif

        public bool ContainsPrefabPath(string path) => _items.Exists(item => item.prefabPath == path);
        public bool ContainsSceneObject(GameObject obj)
        {
            if (obj == null) return false;
            var outermostPrefab = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (outermostPrefab == null) return false;
            var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(outermostPrefab);
            if (prefab == null) return false;
#if UNITY_6000_3_OR_NEWER
            return ContainsPrefab(prefab.GetEntityId());
#else
            return ContainsPrefab(prefab.GetInstanceID());
#endif
        }

        public Vector3 minBrushSize
        {
            get
            {
                var min = Vector3.one * float.MaxValue;
                foreach (var item in _items)
                    min = Vector3.Min(min, item.size);
                return min;
            }
        }

        public float minBrushMagnitude
        {
            get
            {
                var min = minBrushSize;
                return Mathf.Min(min.x, min.y, min.z);
            }
        }

        public Vector3 maxBrushSize
        {
            get
            {
                var max = Vector3.one * float.MinValue;
                foreach (var item in _items)
                    max = Vector3.Max(max, item.size);
                max = Vector3.Scale(max, maxScaleMultiplier);
                return max;
            }
        }

        public float maxBrushMagnitude
        {
            get
            {
                var max = maxBrushSize;
                return Mathf.Min(max.x, max.y, max.z);
            }
        }

        public float maxTrianglesCount
        {
            get
            {
                float max = float.MinValue;
                foreach (var item in _items)
                    max = Mathf.Max(max, item.trianglesCount);
                return max;
            }
        }
        public void UpdateAssetTypes()
        {
            foreach (var item in _items) item.UpdateAssetType();
        }

        public void Cleanup()
        {
            foreach (var item in items) if (item.prefab == null) RemoveItem(item);
        }

        public bool RepairItemParentIds()
        {
            bool repaired = false;
            foreach (var item in _items)
                if (item.RepairParentId(this)) repaired = true;
            return repaired;
        }
        public override int GetHashCode()
        {
            int hashCode = 917907199;
            hashCode = hashCode * -1521134295 + _name.GetHashCode();
            hashCode = hashCode * -1521134295 + _frequencyMode.GetHashCode();
            hashCode = hashCode * -1521134295 + _pattern.GetHashCode();
            hashCode = hashCode * -1521134295 + _restartPatternForEachStroke.GetHashCode();
            foreach (var item in _items) hashCode = hashCode * -1521134295 + item.GetHashCode();
            return hashCode;

        }

        public static MultibrushSettings Create(GameObject prefab, PaletteData palette)
        {
            if (PrefabUtils.HasMissingScripts(prefab))
            {
                Debug.LogWarning($"[PWB] Cannot create brush from '{prefab.name}' because it has missing script references." +
                    $" Please fix the prefab and try again.");
                return null;
            }
            return new MultibrushSettings(prefab, palette);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            foreach (var item in _items)
            {
                item.InitializeParentSettings(this);
            }
        }
    }
}
#pragma warning restore UDR0001
