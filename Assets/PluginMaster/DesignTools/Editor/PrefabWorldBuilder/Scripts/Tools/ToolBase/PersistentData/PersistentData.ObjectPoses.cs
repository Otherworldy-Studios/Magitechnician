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
    public struct ObjectPose
    {
        [SerializeField] private Vector3 _position;
        [SerializeField] private Quaternion _localRotation;
        [SerializeField] private Vector3 _localScale;

        public ObjectPose(Vector3 position, Quaternion localRotation, Vector3 localScale)
        {
            _position = position;
            _localRotation = localRotation;
            _localScale = localScale;
        }

        public ObjectPose(GameObject obj)
        {
            _position = obj.transform.position;
            _localRotation = obj.transform.localRotation;
            _localScale = obj.transform.localScale;
        }
        public Vector3 position { get => _position; set => _position = value; }
        public Quaternion localRotation { get => _localRotation; set => _localRotation = value; }
        public Vector3 localScale { get => _localScale; set => _localScale = value; }
        public void Copy(ObjectPose other)
        {
            _position = other._position;
            _localRotation = other._localRotation;
            _localScale = other._localScale;
        }
    }

    [System.Serializable]
    public struct ObjectId : System.IEquatable<ObjectId>
    {
        [SerializeField] private string _globalObjId;
        public string globalObjId { get => _globalObjId; set => _globalObjId = value; }

        public ObjectId(GameObject gameObject)
        {
            _globalObjId = null;
            if (gameObject == null) return;
            if (PWBIO.isInPrefabMode)
            {
                _globalObjId = string.Empty;
                Transform current = gameObject.transform;

                var outermostPrefab = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                if (outermostPrefab == null) return;
                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(outermostPrefab);
                if (prefab == null) return;

                while (current != null && current != PWBIO.prefabStage.prefabContentsRoot.transform)
                {
                    var index = current.GetSiblingIndex().ToString();
                    _globalObjId = string.IsNullOrEmpty(_globalObjId) ? index : index + "," + _globalObjId;
                    current = current.parent;
                }
                UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string prefabGuid, out long localId);
                _globalObjId = prefabGuid + "," + _globalObjId;
                _globalObjId = UnityEditor.AssetDatabase.AssetPathToGUID(PWBIO.prefabStage.assetPath) + "," + _globalObjId;
                return;
            }
            _globalObjId = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString();
        }

        public ObjectId(string globalObjId)
        {
            _globalObjId = globalObjId;
        }
        public override int GetHashCode()
        {
            int hashCode = 917907199;
            hashCode = hashCode * -1521134295
                + System.Collections.Generic.EqualityComparer<string>.Default.GetHashCode(_globalObjId);
            return hashCode;
        }
        public bool Equals(ObjectId other) => _globalObjId == other._globalObjId;
        public override bool Equals(object obj) => obj is ObjectId other && this.Equals(other);
        public static bool operator ==(ObjectId lhs, ObjectId rhs) => lhs.Equals(rhs);
        public static bool operator !=(ObjectId lhs, ObjectId rhs) => !lhs.Equals(rhs);

        public void Copy(ObjectId other)
        {
            _globalObjId = other._globalObjId;
        }

        public static T FindObject<T>(string globObjId, bool ignorePrefabMode) where T : Object
        {
            if (PWBIO.isInPrefabMode && !ignorePrefabMode)
            {
                var indices = globObjId.Split(',');
                var rootId = indices[0];
                var rootPath = UnityEditor.AssetDatabase.GUIDToAssetPath(rootId);
                if (rootPath != PWBIO.prefabStage.assetPath) return null;
                var prefabId = indices[1];
                var current = PWBIO.prefabStage.prefabContentsRoot.transform;
                for (int i = 2; i < indices.Length; ++i)
                {
                    var indexStr = indices[i];
                    if (!int.TryParse(indexStr, out int childIndex)) return null;
                    if (childIndex < 0 || childIndex >= current.childCount) return null;
                    current = current.GetChild(childIndex);
                }
                var outermostPrefab = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(current);
                if (outermostPrefab == null) return null;
                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(outermostPrefab);
                if (prefab == null) return null;
                UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab,
                    out string currentPrefabGuid, out long localId);
                if (currentPrefabGuid != prefabId) return null;
                return current.gameObject as T;
            }

            T obj = null;
            if (UnityEditor.GlobalObjectId.TryParse(globObjId, out UnityEditor.GlobalObjectId id))
                obj = UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as T;
            return obj;
        }

        public static GameObject FindObject(ObjectId objId)
            => FindObject<GameObject>(objId.globalObjId, ignorePrefabMode: false);
    }

    public partial class PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>
        : IPersistentData, ISerializationCallbackReceiver
        where TOOL_NAME : IToolName, new()
        where TOOL_SETTINGS : IToolSettings, new()
        where CONTROL_POINT : ControlPoint, new()
    {
        [SerializeField]
        private System.Collections.Generic.List<ObjectPose> _poses = new System.Collections.Generic.List<ObjectPose>();
        [SerializeField]
        private System.Collections.Generic.List<ObjectId> _objectIds = new System.Collections.Generic.List<ObjectId>();
        private System.Collections.Generic.List<GameObject> _objects = new System.Collections.Generic.List<GameObject>();


        private void FindObjects()
        {
            if (_objectIds.Count == _objects.Count) return;
            var ids = _objectIds.ToArray();
            _objectIds.Clear();
            _poses.Clear();
            _objects.Clear();
            void AddPose(GameObject obj)
            {
                if (obj == null) return;
                _objectIds.Add(new ObjectId(obj));
                _objects.Add(obj);
                _poses.Add(new ObjectPose(obj));
            }

            var first = ObjectId.FindObject(ids[0]);
            if (first != null)
            {
                var parent = first.transform.parent;
                if (parent != null && parent.childCount == ids.Length)
                {
                    foreach (Transform child in parent) AddPose(child.gameObject);
                    return;
                }
            }
            for (int i = 0; i < ids.Length; ++i)
            {
                var obj = ObjectId.FindObject(ids[i]);
                if (obj == null) continue;
                _objectIds.Add(ids[i]);
                _objects.Add(obj);
                _poses.Add(new ObjectPose(obj));
            }
        }

        public void AddPose(ObjectId objId, ObjectPose pose, bool updateObjectArray = true)
        {
            var obj = ObjectId.FindObject(objId);
            if (obj == null) return;
            _poses.Add(pose);
            _objectIds.Add(objId);
            if (!updateObjectArray) return;
            if (_objectIds.Count + 1 != _objects.Count) FindObjects();
            _objects.Add(obj);
        }
        public void InitializePoses((ObjectId, ObjectPose)[] items)
        {
            _poses.Clear();
            _objectIds.Clear();
            _objects.Clear();
            foreach (var item in items) AddPose(item.Item1, item.Item2);
        }
        public void InsertPose(int index, ObjectPose pose, ObjectId objId, bool updateObjectArray = true)
        {
            var obj = ObjectId.FindObject(objId);
            if (obj == null) return;
            _poses.Insert(index, pose);
            _objectIds.Insert(index, objId);
            if (!updateObjectArray) return;
            if (_objectIds.Count + 1 != _objects.Count) FindObjects();
            _objects.Insert(index, obj);
        }

        public void RemovePose(int index)
        {
            _poses.RemoveAt(index);
            _objectIds.RemoveAt(index);
            if (_objectIds.Count - 1 == _objects.Count) _objects.RemoveAt(index);
        }

        public void RemoveAllPoses()
        {
            _poses.Clear();
            _objectIds.Clear();
            _objects.Clear();
        }

        public void UpdatePoses()
        {
            if (_objectIds.Count != _objects.Count)
            {
                FindObjects();
                return;
            }
            var objCount = _objectIds.Count;
            var ids = _objectIds.ToArray();
            var poses = _poses.ToArray();
            var objs = _objects.ToArray();
            _objectIds.Clear();
            _poses.Clear();
            _objects.Clear();
            for (int i = 0; i < objCount; ++i)
            {
                var obj = objs[i];
                if (obj == null) continue;
                AddPose(new ObjectId(obj), new ObjectPose(obj));
            }
        }

        public void AddObjects((GameObject, int)[] objects)
        {
            for (int i = 0; i < objects.Length; ++i)
            {
                var idx = objects[i].Item2;
                var obj = objects[i].Item1;
                if (idx > -1 && idx < objectCount)
                    InsertPose(idx, new ObjectPose(obj.transform.position, obj.transform.localRotation,
                        obj.transform.localScale), new ObjectId(obj));
                else AddPose(new ObjectId(obj),
                    new ObjectPose(obj.transform.position, obj.transform.localRotation, obj.transform.localScale));
            }
        }

        public bool ReplaceObject(GameObject target, GameObject obj)
        {
            int targetIdx = -1;
            var targetId = new ObjectId(target);
            for (int i = 0; i < _objectIds.Count; ++i)
            {
                var objId = _objectIds[i];
                if (targetId == objId)
                {
                    targetIdx = i;
                    break;
                }
            }
            if (targetIdx == -1) return false;
            InsertPose(targetIdx, new ObjectPose(obj.transform.position, obj.transform.localRotation,
                obj.transform.localScale), new ObjectId(obj), updateObjectArray: true);
            RemovePose(targetIdx + 1);
            return true;
        }

        public int objectCount => _objectIds.Count;

        public bool isEmpty()
        {
            if (_objectIds.Count == 0) return true;
            var allObjectsAreNull = _objects.Count > 0 && !_objects.Exists(i => i != null);
            return allObjectsAreNull;
        }

        public GameObject[] objects
        {
            get
            {
                if (_objectIds.Count != _objects.Count) FindObjects();
                return _objects.ToArray();
            }
        }

        public System.Collections.Generic.HashSet<GameObject> objectSet
        {
            get
            {
                if (_objectIds.Count != _objects.Count) FindObjects();
                return new System.Collections.Generic.HashSet<GameObject>(_objects);
            }
        }

        public System.Collections.Generic.List<GameObject> objectList
        {
            get
            {
                if (_objectIds.Count != _objects.Count) FindObjects();
                return _objects.ToList();
            }
        }

        public void DestroyGameObjects()
        {
            foreach (var obj in objectList)
                if (obj != null)
                    UnityEditor.Undo.DestroyObjectImmediate(obj);
        }

        public virtual void ResetPoses(PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT> initialData)
        {
            var initialPoses = initialData._poses;
            if (_objectIds.Count != _objects.Count) FindObjects();
            for (int i = 0; i < objectCount; ++i)
            {
                var obj = _objects[i];
                if (obj == null) obj = ObjectId.FindObject(_objectIds[i]);
                if (obj == null) continue;
                var pose = _poses[i];
                UnityEditor.Undo.RecordObject(obj.transform, RESET_COMMAND_NAME);
                obj.transform.position = pose.position;
                obj.transform.localRotation = pose.localRotation;
                obj.transform.localScale = pose.localScale;
                obj.SetActive(true);
            }
            Copy(initialData);
        }

        public GameObject GetParent()
        {
            var parents = new System.Collections.Generic.List<GameObject>();
            if (_objectIds.Count != _objects.Count) FindObjects();
            var objList = objectList;
            void GetParentList()
            {
                parents.Clear();
                foreach (var obj in objList)
                {
                    if (obj == null || obj.transform.parent == null)
                    {
                        parents.Clear();
                        return;
                    }
                    else
                    {
                        if (parents.Contains(obj.transform.parent.gameObject)) continue;
                        parents.Add(obj.transform.parent.gameObject);
                    }
                }
            }
            do
            {
                GetParentList();
                objList = parents.ToList();
            }
            while (parents.Count > 1);
            if (parents.Count == 0) return null;
            return parents[0];
        }
    }
}
#pragma warning restore UDR0001
