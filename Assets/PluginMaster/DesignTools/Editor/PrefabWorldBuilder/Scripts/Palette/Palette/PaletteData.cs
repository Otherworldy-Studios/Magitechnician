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
    public class PaletteData : ISerializationCallbackReceiver
    {
        [SerializeField] private string _version = PWBData.VERSION;
        [SerializeField] private string _name = string.Empty;
        [SerializeField] private long _id = -1;
        [SerializeField] private string _hexId = string.Empty;
        [SerializeField]
        private System.Collections.Generic.List<MultibrushSettings> _brushes
            = new System.Collections.Generic.List<MultibrushSettings>();
        [SerializeField] BrushCreationSettings _brushCreationSettings = new BrushCreationSettings();
        [SerializeField] private int _hashCode = 0;
        [SerializeField] private bool _isPinned = false;
        private string _filePath = null;
        private bool _saving = false;
        public PaletteData(string name, long id) => (_name, _id) = (name, id);

        public string name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                Save();
            }
        }
        public long id => _id;
        public string hexId => _id.ToString("X");
        public MultibrushSettings[] brushes => _brushes.Where(b => !b.allPrefabMissing).ToArray();

        public int brushCount => _brushes.Count;

        public BrushCreationSettings brushCreationSettings => _brushCreationSettings;

        public string filePath
        {
            get
            {
                void SetFilePath() => _filePath = PWBData.palettesDirectory + "/"
                    + GetFileNameFromData(this, includeExtension: true);
                if (_filePath == null) SetFilePath();
                else if (!System.IO.File.Exists(_filePath)) SetFilePath();
                return _filePath;
            }
            set => _filePath = value;
        }
        public string thumbnailsFolderPath
        {
            get
            {
                var path = PWBData.palettesDirectory + (PWBCore.staticData.createThumbnailsFolder ? "/Thumbnails/" : "/")
                    + GetFileNameFromData(this, includeExtension: false);
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                return path;
            }
        }

        public string version
        {
            get
            {
                if (_version == null || _version == string.Empty) _version = PWBData.VERSION;
                return _version;
            }
            set => _version = value;
        }
        public bool saving => _saving;
        public bool isPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned == value) return;
                _isPinned = value;
                Save();
            }
        }
        public void StopSaving() => _saving = false;

        public static string GetFileNameFromData(PaletteData data, bool includeExtension)
            => "PWB_" + data._id.ToString("X") + (includeExtension ? ".txt" : "");

        public MultibrushSettings GetBrush(int idx)
        {
            if (idx < 0 || idx >= _brushes.Count) return null;
            if (_brushes[idx].allPrefabMissing) return null;
            return _brushes[idx];
        }

        public void UpdateAllThumbnails()
        {
            foreach (var brush in _brushes) brush.UpdateThumbnail(updateItemThumbnails: true, savePng: true);
        }

        public void AddBrush(MultibrushSettings brush)
        {
            if (brush == null) return;
            _brushes.Add(brush);
            brush.palette = this;
            Save();
            ClearObjectQuery();
        }

        public void RemoveBrushAt(int idx)
        {
            _brushes.RemoveAt(idx);
            BrushstrokeManager.UpdateBrushstroke();
            Save();
            ClearObjectQuery();
        }

        public void RemoveBrush(MultibrushSettings brush)
        {
            _brushes.Remove(brush);
            BrushstrokeManager.UpdateBrushstroke();
            PrefabPalette.OnChangeRepaint();
            Save();
            ClearObjectQuery();
        }

        public void InsertBrushAt(MultibrushSettings brush, int idx)
        {
            _brushes.Insert(idx, brush);
            brush.palette = this;
            Save();
            ClearObjectQuery();
        }

        public void Swap(int fromIdx, int toIdx, ref int[] selection)
            => SelectionUtils.Swap(fromIdx, toIdx, ref selection, _brushes);

        public void AscendingSort()
        {
            _brushes.Sort(delegate (MultibrushSettings x, MultibrushSettings y) { return x.name.CompareTo(y.name); });
            PaletteManager.ClearSelection();
            PrefabPalette.OnChangeRepaint();
        }

        public void DescendingSort()
        {
            _brushes.Sort(delegate (MultibrushSettings x, MultibrushSettings y) { return y.name.CompareTo(x.name); });
            PaletteManager.ClearSelection();
            PrefabPalette.OnChangeRepaint();
        }

        public void DuplicateBrush(int index, out MultibrushSettings duplicate)
            => DuplicateBrushAt(index, index, out duplicate);

        public void DuplicateBrushAt(int indexToDuplicate, int at, out MultibrushSettings duplicate)
        {
            var original = _brushes[indexToDuplicate];
            duplicate = original.Clone() as MultibrushSettings;

            if (duplicate.thumbnailSettings.useCustomImage)
            {
                duplicate.thumbnailSettings.useCustomImage = false;
                duplicate.UpdateThumbnail(updateItemThumbnails: true, savePng: true);
                duplicate.thumbnailSettings.useCustomImage = true;
                var thumbnailClone = new Texture2D(original.thumbnail.width, original.thumbnail.height);
                thumbnailClone.SetPixels(original.thumbnail.GetPixels());
                thumbnailClone.Apply();
                duplicate.SetCustomThumbnailTexture(thumbnailClone, savePng: true);
            }
            else duplicate.UpdateThumbnail(updateItemThumbnails: true, savePng: true);
            _brushes.Insert(at, duplicate);
            Save();
        }

        public void CreateSingleBrushes(int index)
        {
            _brushes[index].palette = this;
            var singleBrushes = _brushes[index].CreateSingleBrushes();
            _brushes.AddRange(singleBrushes);
            Save();
            ClearObjectQuery();
        }

#if UNITY_6000_3_OR_NEWER
        private System.Collections.Generic.Dictionary<EntityId, bool> _objectQuery
            = new System.Collections.Generic.Dictionary<EntityId, bool>();
        private System.Collections.Generic.Dictionary<EntityId, bool> objectQuery
        {
            get
            {
                if (_objectQuery == null) _objectQuery = new System.Collections.Generic.Dictionary<EntityId, bool>();
                return _objectQuery;
            }
        }
#else
        private System.Collections.Generic.Dictionary<int, bool> _objectQuery
            = new System.Collections.Generic.Dictionary<int, bool>();
        private System.Collections.Generic.Dictionary<int, bool> objectQuery
        {
            get
            {
                if (_objectQuery == null) _objectQuery = new System.Collections.Generic.Dictionary<int, bool>();
                return _objectQuery;
            }
        }
#endif
        public void ClearObjectQuery() => objectQuery.Clear();

        public bool ContainsSceneObject(GameObject obj)
        {
            if (obj == null) return false;
#if UNITY_6000_3_OR_NEWER
            var id = obj.GetEntityId();
#else
            var id = obj.GetInstanceID();
#endif
            if (objectQuery.TryGetValue(id, out bool cached)) return cached;
            bool found = false;
            var root = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (root != null)
            {
                var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(root);
                if (source != null)
#if UNITY_6000_3_OR_NEWER
                    found = _brushes.Exists(brush => brush.ContainsPrefab(source.GetEntityId()));
#else
                    found = _brushes.Exists(brush => brush.ContainsPrefab(source.GetInstanceID()));
#endif
            }
            objectQuery[id] = found;
            return found;
        }

        public System.Collections.Generic.HashSet<GameObject> prefabs
        {
            get
            {
                var prefabSet = new System.Collections.Generic.HashSet<GameObject>();
                foreach (var brush in _brushes) prefabSet.UnionWith(brush.prefabs);
                return prefabSet;
            }
        }
        public bool ContainsPrefabPath(string path) => _brushes.Exists(brush => brush.ContainsPrefabPath(path));
        public int FindBrushIdx(GameObject obj)
        {
            if (obj == null) return -1;
            var outermostPrefab = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (outermostPrefab == null) return -1;
            var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(outermostPrefab);
            if (prefab == null) return -1;
#if UNITY_6000_3_OR_NEWER
            var idx = _brushes.FindIndex(brush => brush.ContainsPrefab(prefab.GetEntityId()) && brush.itemCount == 1);
            if (idx == -1) idx = _brushes.FindIndex(brush => brush.ContainsPrefab(prefab.GetEntityId()));
#else
            var idx = _brushes.FindIndex(brush => brush.ContainsPrefab(prefab.GetInstanceID()) && brush.itemCount == 1);
            if (idx == -1) idx = _brushes.FindIndex(brush => brush.ContainsPrefab(prefab.GetInstanceID()));
#endif
            return idx;
        }

        public bool ContainsBrush(MultibrushSettings brush)
            => _brushes.Contains(brush) || _brushes.Exists(b => b.id == brush.id);

        
        public static System.Action OnPaletteSaved = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public string Save()
        {
            if (UnityEditor.EditorApplication.isCompiling
                || UnityEditor.EditorApplication.isUpdating
                || UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                UnityEditor.EditorApplication.delayCall += () => Save();
                return filePath;
            }
            _saving = true;
            var jsonString = JsonUtility.ToJson(this, true);
            var fileExist = System.IO.File.Exists(filePath);
            UnityEditor.AssetDatabase.StartAssetEditing();
            try
            {
                System.IO.File.WriteAllText(filePath, jsonString);
            }
            finally
            {
                UnityEditor.AssetDatabase.StopAssetEditing();
                _saving = false;
            }
            if (!fileExist) PWBCore.refreshDatabase = true;
            if (OnPaletteSaved != null) OnPaletteSaved();
            return filePath;
        }

        public void Copy(PaletteData other)
        {
            _brushes.Clear();
            var otherBrushes = other.brushes.ToArray();
            var cloneBrushes = otherBrushes.Select(b => b.CloneAndChangePalette(this)).ToArray();
            _brushes.AddRange(cloneBrushes);
            _name = other.name;
            _brushCreationSettings.Copy(other._brushCreationSettings);
        }
        public void ReloadFromFile()
        {
            var fileText = System.IO.File.ReadAllText(_filePath);
            if (string.IsNullOrEmpty(fileText)) return;
            var paletteData = JsonUtility.FromJson<PaletteData>(fileText);
            if (paletteData == null) return;
            Copy(paletteData);
            ClearObjectQuery();
        }

        public void Cleanup()
        {
            foreach (var brush in _brushes.ToArray()) brush.Cleanup();
            Save();
            ClearObjectQuery();
        }

        public bool RepairItemParentIds()
        {
            bool repaired = false;
            foreach (var brush in _brushes)
                if (brush.RepairItemParentIds()) repaired = true;
            return repaired;
        }

        public int hashCode => _hashCode;
        public override int GetHashCode()
        {
            if (_name == null) return 0;
            int hashCode = 917907199;
            hashCode = hashCode * -1521134295 + version.GetHashCode();
            hashCode = hashCode * -1521134295 + _name.GetHashCode();
            hashCode = hashCode * -1521134295 + _id.GetHashCode();
            foreach (var brush in _brushes) hashCode = hashCode * -1521134295 + brush.GetHashCode();
            hashCode = hashCode * -1521134295 + _brushCreationSettings.GetHashCode();
            return hashCode;
        }

        public void ResetHashCode()
        {
            _hashCode = GetHashCode();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            try
            {
                _hashCode = GetHashCode();
                _hexId = _id.ToString("X");
            }
            catch
            {
                _hashCode = 0;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }
    }
}
#pragma warning restore UDR0001
