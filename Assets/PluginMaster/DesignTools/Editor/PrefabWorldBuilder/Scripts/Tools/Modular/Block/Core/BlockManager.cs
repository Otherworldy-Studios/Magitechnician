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
    #region BLOCK SCENE DATA
    [System.Serializable]
    public class BlockSceneData : ISerializationCallbackReceiver
    {
        [SerializeField] private string _sceneGUID = null;
        [SerializeField] private OccupiedCell[] _occupiedCells = null;

        private System.Collections.Generic.HashSet<OccupiedCell> _occupiedCellsSet
            = new System.Collections.Generic.HashSet<OccupiedCell>();

        public string sceneGUID { get => _sceneGUID; set => _sceneGUID = value; }
        public int occupiedCellCount => _occupiedCellsSet.Count;

        public BlockSceneData() { }

        public BlockSceneData(string sceneGUID)
        {
            _sceneGUID = sceneGUID;
        }
        public BlockSceneData(BlockSceneData other)
        {
            _sceneGUID = other._sceneGUID;
            foreach (var cell in other._occupiedCellsSet)
            {
                _occupiedCellsSet.Add(new OccupiedCell(cell));
            }
        }
        public bool AddOccupiedCell(OccupiedCell cell)
        {
            return _occupiedCellsSet.Add(cell);
        }

        public bool RemoveOccupiedCell(OccupiedCell cell, out long brushId)
        {
            brushId = -1;
#if UNITY_2021_2_OR_NEWER
            if (_occupiedCellsSet.TryGetValue(cell, out var existingCell))
#else
            var existingCell = _occupiedCellsSet.FirstOrDefault(c => c.Equals(cell));
            if (existingCell != null)
#endif
            {
                brushId = existingCell.brushId;
                _occupiedCellsSet.Remove(existingCell);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _occupiedCellsSet.Clear();
        }

        public OccupiedCell[] GetOccupiedCells() => _occupiedCellsSet.ToArray();

        public void OnBeforeSerialize()
        {
            if (_occupiedCellsSet == null) return;
            _occupiedCells = new OccupiedCell[_occupiedCellsSet.Count];
            int i = 0;
            foreach (var cell in _occupiedCellsSet)
            {
                _occupiedCells[i] = new OccupiedCell(cell);
                i++;
            }
        }

        public void OnAfterDeserialize()
        {
            if (_occupiedCells == null) return;
            if (_occupiedCellsSet == null) _occupiedCellsSet = new System.Collections.Generic.HashSet<OccupiedCell>();
            else _occupiedCellsSet.Clear();
            foreach (var cell in _occupiedCells)
            {
                _occupiedCellsSet.Add(new OccupiedCell(cell));
            }
        }

        public void CheckCellsStillOccupiedByObjects()
        {
            if (_occupiedCellsSet == null) return;
            var occupiedCellsArray = _occupiedCellsSet.ToArray();
            _occupiedCellsSet.Clear();

            foreach (var cell in occupiedCellsArray)
            {
                if (IsCellStillOccupiedByObject(cell))
                    _occupiedCellsSet.Add(cell);
            }
        }

        private static bool IsCellStillOccupiedByObject(OccupiedCell cell)
        {
            var halfSize = cell.size * 0.5f;
            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            PWBIO.boundsOctree.GetColliding(cell.center, halfSize,
                GridManager.settings.rotation, cell.rotation, nearbyObjects);

            var invRotation = Quaternion.Inverse(GridManager.settings.rotation);
            foreach (var obj in nearbyObjects)
            {
                if (obj == null) continue;
                var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                var localOffset = invRotation * (objCenter - cell.center);
                if (Mathf.Abs(localOffset.x) < halfSize.x
                    && Mathf.Abs(localOffset.y) < halfSize.y
                    && Mathf.Abs(localOffset.z) < halfSize.z)
                    return true;
            }
            return false;
        }
    }
    #endregion

    #region BLOCK MANAGER
    [System.Serializable]
    public class BlockManager : ToolControllerBase<BlockSettings, BlockManager>
    {
        #region SIZES
        [SerializeField] private BlockCellSize[] _sizes = null;
        private const string DEFAULT_SIZE_NAME = "Default";
        [SerializeField] private string _selectedSizeName = DEFAULT_SIZE_NAME;
        private System.Collections.Generic.Dictionary<string, Vector3> _sizesDictionary
            = new System.Collections.Generic.Dictionary<string, Vector3>() { { DEFAULT_SIZE_NAME, Vector3.one } };

        public string selectedSizeName
        {
            get => _selectedSizeName;
            set
            {
                if (_selectedSizeName == value) return;
                _selectedSizeName = value;
                settings.moduleSize = _sizesDictionary[selectedSizeName];
                PWBCore.SetSavePending();
            }
        }

        public void SaveSize(string name)
        {
            if (_sizesDictionary.ContainsKey(name)) _sizesDictionary[name] = settings.moduleSize;
            else _sizesDictionary.Add(name, settings.moduleSize);
            _selectedSizeName = name;
            PWBCore.SetSavePending();
        }

        public string[] GetSizesNames() => _sizesDictionary.Keys.ToArray();

        public void DeleteSelectedSize()
        {
            _sizesDictionary.Remove(_selectedSizeName);
            selectedSizeName = DEFAULT_SIZE_NAME;
        }

        public int GetIndexOfSize(string name) => _sizesDictionary.Keys.Select((key, index) => new { key, index })
            .FirstOrDefault(pair => pair.key == name)?.index ?? -1;

        public int GetIndexOfSelectedSize() => GetIndexOfSize(selectedSizeName);

        public string GetSizeAt(int index) => _sizesDictionary.Keys.ElementAt(index);

        public void SelectSize(int index) => selectedSizeName = GetSizeAt(index);

        public void ResetSize()
        {
            settings.moduleSize = _sizesDictionary[selectedSizeName];
            PWBCore.SetSavePending();
        }
        #endregion

        public static Vector3 currentBlockPosition { get; set; } = Vector3.zero;
        public static int quarterTurns { get; set; } = 0;

        private static System.Collections.Generic.List<BlockSceneData> _staticSceneItems = null;
        [SerializeField]
        private System.Collections.Generic.List<BlockSceneData> _sceneItems
            = new System.Collections.Generic.List<BlockSceneData>();

        private static OccupiedCellOctree _occupiedCellsOctree = new OccupiedCellOctree();

        private static bool _octreeNeedsRebuild = false;

        public static int occupiedCellCount => _occupiedCellsOctree.Count;


        private static string GetCurrentSceneGUID()
        {
            if (PWBIO.isInPrefabMode)
                return UnityEditor.AssetDatabase.AssetPathToGUID(PWBIO.prefabStage.assetPath);
            var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            return UnityEditor.AssetDatabase.AssetPathToGUID(scenePath);
        }

        private static BlockSceneData GetOrCreateSceneData(string sceneGUID)
        {
            if (_staticSceneItems == null)
                _staticSceneItems = new System.Collections.Generic.List<BlockSceneData>();

            var sceneData = _staticSceneItems.Find(s => s.sceneGUID == sceneGUID);
            if (sceneData == null)
            {
                sceneData = new BlockSceneData(sceneGUID);
                _staticSceneItems.Add(sceneData);
            }
            return sceneData;
        }

        private static BlockSceneData GetCurrentSceneData()
        {
            return GetOrCreateSceneData(GetCurrentSceneGUID());
        }

        public static void AddOccupiedCell(Vector3 center, Vector3 size, Quaternion rotation, long brushId)
        {
            RebuildOctreeIfNeeded();
            var cell = new OccupiedCell(center, size, rotation, brushId);
            if (GetCurrentSceneData().AddOccupiedCell(cell))
            {
                _occupiedCellsOctree.Add(cell);
                PWBCore.SetSavePending();
            }
        }

        public static void RemoveOccupiedCell(Vector3 center, Vector3 size, Quaternion rotation, out long brushId)
        {
            RebuildOctreeIfNeeded();
            var cell = new OccupiedCell(center, size, rotation, brushId: 0);
            if (GetCurrentSceneData().RemoveOccupiedCell(cell, out brushId))
            {
                _occupiedCellsOctree.Remove(cell);
                PWBCore.SetSavePending();
            }
        }

        public static void MoveOccupiedCells(
            System.Collections.Generic.IEnumerable<(Vector3 oldCenter, Vector3 newCenter)> moves)
        {
            var size = GridManager.settings.step;
            var rotation = GridManager.settings.rotation;
            RebuildOctreeIfNeeded();
            var sceneData = GetCurrentSceneData();
            var pendingAdds = new System.Collections.Generic.List<OccupiedCell>();

            foreach (var (oldCenter, newCenter) in moves)
            {
                var oldCell = new OccupiedCell(oldCenter, size, rotation, brushId: 0);
                if (sceneData.RemoveOccupiedCell(oldCell, out var brushId))
                {
                    _occupiedCellsOctree.Remove(oldCell);
                    pendingAdds.Add(new OccupiedCell(newCenter, size, rotation, brushId));
                }
            }
            foreach (var newCell in pendingAdds)
            {
                if (sceneData.AddOccupiedCell(newCell))
                    _occupiedCellsOctree.Add(newCell);
            }

            if (pendingAdds.Count > 0)
                PWBCore.SetSavePending();
        }

        public static void ClearOccupiedCells()
        {
            RebuildOctreeIfNeeded();
            var sceneData = GetCurrentSceneData();
            var cells = sceneData.GetOccupiedCells();
            foreach (var cell in cells)
                _occupiedCellsOctree.Remove(cell);
            sceneData.Clear();
            PWBCore.SetSavePending();
        }

        public static void ClearAllOccupiedCells()
        {
            if (_staticSceneItems != null)
            {
                foreach (var sceneData in _staticSceneItems)
                    sceneData.Clear();
            }
            _occupiedCellsOctree.Clear();
            _octreeNeedsRebuild = false;
            PWBCore.SetSavePending();
        }

        public static bool IsCellOccupied(Vector3 center, Vector3 size, float tolerance = 0.01f)
        {
            RebuildOctreeIfNeeded();
            return _occupiedCellsOctree.IsOccupied(center, size, tolerance);
        }

        public static bool IsCellOccupied(Vector3 center, Vector3 size,
            out GameObject[] objects, out long brushId, float tolerance = 0.01f)
        {
            RebuildOctreeIfNeeded();
            var result = new System.Collections.Generic.List<GameObject>();
            var halfSize = size * 0.5f;
            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            PWBIO.boundsOctree.GetColliding(center, halfSize,
                GridManager.settings.rotation, GridManager.settings.rotation, nearbyObjects);
            var invRotation = Quaternion.Inverse(GridManager.settings.rotation);
            foreach (var obj in nearbyObjects)
            {
                if (obj == null) continue;
                var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                var localOffset = invRotation * (objCenter - center);
                if (Mathf.Abs(localOffset.x) < halfSize.x
                    && Mathf.Abs(localOffset.y) < halfSize.y
                    && Mathf.Abs(localOffset.z) < halfSize.z)
                {
                    result.Add(obj);
                }
            }
            objects = result.ToArray();
            var isOccupied = _occupiedCellsOctree.IsOccupied(center, size, out brushId, tolerance);
            return isOccupied;
        }

        private static void RebuildOctreeIfNeeded()
        {
            if (!_octreeNeedsRebuild) return;
            _octreeNeedsRebuild = false;
            RebuildOctree();
        }

        private static void RebuildOctree()
        {

            _occupiedCellsOctree.Clear();
            if (_staticSceneItems == null) return;
            var loadedSceneGUIDs = GetLoadedSceneGUIDs();
            foreach (var sceneData in _staticSceneItems)
            {
                if (!loadedSceneGUIDs.Contains(sceneData.sceneGUID)) continue;
                sceneData.CheckCellsStillOccupiedByObjects();
                var cells = sceneData.GetOccupiedCells();
                foreach (var cell in cells)
                    _occupiedCellsOctree.Add(cell);
            }
        }

        private static System.Collections.Generic.HashSet<string> GetLoadedSceneGUIDs()
        {
            var loadedSceneGUIDs = new System.Collections.Generic.HashSet<string>();

            if (PWBIO.isInPrefabMode)
            {
                loadedSceneGUIDs.Add(UnityEditor.AssetDatabase.AssetPathToGUID(PWBIO.prefabStage.assetPath));
            }
            else
            {
                var openedSceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
                for (int i = 0; i < openedSceneCount; ++i)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    var guid = UnityEditor.AssetDatabase.AssetPathToGUID(scene.path);
                    loadedSceneGUIDs.Add(guid);
                }
            }
            return loadedSceneGUIDs;
        }
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            _sizes = _sizesDictionary.Select(pair => new BlockCellSize(pair.Key, pair.Value)).ToArray();

            if (_staticSceneItems == null) return;
            if (_sceneItems == null) _sceneItems = new System.Collections.Generic.List<BlockSceneData>();
            else _sceneItems.Clear();
            foreach (var sceneData in _staticSceneItems)
            {
                var copy = new BlockSceneData(sceneData);
                _sceneItems.Add(copy);
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (_sizes != null && _sizes.Length > 0)
                _sizesDictionary = _sizes.ToDictionary(origin => origin.name, origin => origin.size);
            if (_sceneItems == null) return;

            if (_staticSceneItems == null) _staticSceneItems = new System.Collections.Generic.List<BlockSceneData>();
            else _staticSceneItems.Clear();
            foreach (var sceneData in _sceneItems)
            {
                var copy = new BlockSceneData(sceneData);
                _staticSceneItems.Add(copy);
            }
            _octreeNeedsRebuild = true;
        }
        #endregion
    }
}
#pragma warning restore UDR0001
