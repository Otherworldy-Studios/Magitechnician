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
using System.Linq;

namespace PluginMaster
{
    public static partial class PWBCore
    {
        #region PARENT MANAGEMENT

        public const string PARENT_COLLIDER_NAME = "PluginMasterPrefabPaintTempMeshColliders";

        private static GameObject _parentCollider = null;
#if UNITY_6000_3_OR_NEWER
        private static EntityId _parentColliderId = EntityId.None;
#else
        private static int _parentColliderId = -1;
#endif

        private static GameObject parentCollider
        {
            get
            {
                if (PWBIO.isInPrefabMode)
                {
                    var parentCollider = PWBIO.prefabStage.prefabContentsRoot.transform.Find(PARENT_COLLIDER_NAME);
                    if (parentCollider == null)
                    {
                        _parentCollider = new GameObject(PWBCore.PARENT_COLLIDER_NAME);
                        _parentCollider.transform.SetParent(PWBIO.prefabStage.prefabContentsRoot.transform);
                    }
                    else _parentCollider = parentCollider.gameObject;
                    _parentCollider.hideFlags = HideFlags.HideAndDontSave;
                }
                else if (_parentCollider == null)
                {
                    _parentCollider = new GameObject(PARENT_COLLIDER_NAME);
#if UNITY_6000_3_OR_NEWER
                    _parentColliderId = _parentCollider.GetEntityId();
#else
                    _parentColliderId = _parentCollider.GetInstanceID();
#endif
                    _parentCollider.hideFlags = HideFlags.HideAndDontSave;
                }
                return _parentCollider;
            }
        }
#if UNITY_6000_3_OR_NEWER
        public static EntityId parentColliderId => _parentColliderId;
#else
        public static int parentColliderId => _parentColliderId;
#endif

        #endregion

        #region DATA STRUCTURES

#if UNITY_6000_3_OR_NEWER
        private static System.Collections.Generic.Dictionary<EntityId, GameObject> _tempCollidersIds
            = new System.Collections.Generic.Dictionary<EntityId, GameObject>();
        private static System.Collections.Generic.Dictionary<EntityId, GameObject> _tempCollidersTargets
            = new System.Collections.Generic.Dictionary<EntityId, GameObject>();
        private static System.Collections.Generic.HashSet<EntityId> _tempCollidersRootTargets
            = new System.Collections.Generic.HashSet<EntityId>();
        private static System.Collections.Generic.Dictionary<EntityId, System.Collections.Generic.HashSet<EntityId>>
            _tempCollidersTargetParentsIds
            = new System.Collections.Generic.Dictionary<EntityId, System.Collections.Generic.HashSet<EntityId>>();
        private static System.Collections.Generic.Dictionary<EntityId, System.Collections.Generic.HashSet<EntityId>>
            _tempCollidersTargetChildrenIds
            = new System.Collections.Generic.Dictionary<EntityId, System.Collections.Generic.HashSet<EntityId>>();
#else
        private static System.Collections.Generic.Dictionary<int, GameObject> _tempCollidersIds
            = new System.Collections.Generic.Dictionary<int, GameObject>();
        private static System.Collections.Generic.Dictionary<int, GameObject> _tempCollidersTargets
            = new System.Collections.Generic.Dictionary<int, GameObject>();
        private static System.Collections.Generic.HashSet<int> _tempCollidersRootTargets
            = new System.Collections.Generic.HashSet<int>();
        private static System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>
            _tempCollidersTargetParentsIds
            = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>();
        private static System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>
            _tempCollidersTargetChildrenIds
            = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>();
#endif

        #endregion

        #region QUERIES
#if UNITY_6000_3_OR_NEWER
        public static bool IsTempCollider(EntityId instanceId) => _tempCollidersIds.ContainsKey(instanceId);
#else
        public static bool IsTempCollider(int instanceId) => _tempCollidersIds.ContainsKey(instanceId);
#endif
#if UNITY_6000_3_OR_NEWER
        public static GameObject GetGameObjectFromTempColliderId(EntityId instanceId)
#else
        public static GameObject GetGameObjectFromTempColliderId(int instanceId)
#endif
        {
            if (!_tempCollidersIds.ContainsKey(instanceId)) return null;
            else if (_tempCollidersIds[instanceId] == null)
            {
                _tempCollidersIds.Remove(instanceId);
#if UNITY_6000_3_OR_NEWER
                var tempCol = UnityEditor.EditorUtility.EntityIdToObject(instanceId);
#else
                var tempCol = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
#endif
                if (tempCol != null) Object.DestroyImmediate(tempCol);
                return null;
            }
            return _tempCollidersIds[instanceId];
        }

        public static GameObject GetGameObjectFromTempCollider(GameObject source)
        {
            if (source == null) return null;
#if UNITY_6000_3_OR_NEWER
            if (IsTempCollider(source.GetEntityId())) return GetGameObjectFromTempColliderId(source.GetEntityId());
#else
            if (IsTempCollider(source.GetInstanceID())) return GetGameObjectFromTempColliderId(source.GetInstanceID());
#endif
            return source;
        }

        public static GameObject[] GetTempColliders(GameObject obj)
        {
#if UNITY_6000_3_OR_NEWER
            var parentId = obj.GetEntityId();
#else
            var parentId = obj.GetInstanceID();
#endif
            bool isParent = false;
            foreach (var childId in _tempCollidersTargetParentsIds.Keys)
            {
                var parentsIds = _tempCollidersTargetParentsIds[childId];
                if (parentsIds.Contains(parentId))
                {
                    isParent = true;
                    break;
                }
            }
            if (!isParent) return null;
            var tempColliders = new System.Collections.Generic.List<GameObject>();
            foreach (var id in _tempCollidersTargetChildrenIds[parentId].ToArray())
            {
                if (!_tempCollidersTargets.ContainsKey(id))
                {
                    _tempCollidersTargetChildrenIds[parentId].Remove(id);
                    continue;
                }
                var tempCollider = _tempCollidersTargets[id];
                if (tempCollider == null)
                {
                    _tempCollidersTargetChildrenIds[parentId].Remove(id);
                    _tempCollidersTargets.Remove(id);
                    continue;
                }
                tempColliders.Add(tempCollider);
            }
            return tempColliders.ToArray();
        }

        #endregion

        #region CREATION

        public static void AddTempCollider(GameObject obj, Pose pose)
        {
            var currentPose = new Pose(obj.transform.position, obj.transform.rotation);
            obj.transform.SetPositionAndRotation(pose.position, pose.rotation);
            AddTempCollider(obj);
            obj.transform.SetPositionAndRotation(currentPose.position, currentPose.rotation);
        }

        public static void AddTempCollider(GameObject obj)
        {
            if (obj == null) return;
#if UNITY_6000_3_OR_NEWER
            if (!_tempCollidersRootTargets.Add(obj.GetEntityId())) return;
#else
            if (!_tempCollidersRootTargets.Add(obj.GetInstanceID())) return;
#endif
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>().ToArray();
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.gameObject.activeInHierarchy)
                    CreateTempCollider(meshFilter.gameObject, meshFilter.sharedMesh);
            }

            var skinnedMeshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>().ToArray();
            foreach (var renderer in skinnedMeshRenderers)
            {
                if (renderer.gameObject.activeInHierarchy)
                    CreateTempCollider(renderer.gameObject, renderer.sharedMesh);
            }

            var spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
            foreach (var spriteRenderer in spriteRenderers)
            {
                var target = spriteRenderer.gameObject;
                if (!target.activeInHierarchy) continue;
                if (spriteRenderer.sprite == null) continue;
#if UNITY_6000_3_OR_NEWER
                if (_tempCollidersTargets.ContainsKey(target.GetEntityId()))
                {
                    if (_tempCollidersTargets[target.GetEntityId()] != null) return;
                    else _tempCollidersTargets.Remove(target.GetEntityId());
                }
                var name = spriteRenderer.gameObject.GetEntityId().ToString();
#else
                if (_tempCollidersTargets.ContainsKey(target.GetInstanceID()))
                {
                    if (_tempCollidersTargets[target.GetInstanceID()] != null) return;
                    else _tempCollidersTargets.Remove(target.GetInstanceID());
                }
                var name = spriteRenderer.gameObject.GetInstanceID().ToString();
#endif
                var tempObj = new GameObject(name);
                tempObj.hideFlags = HideFlags.HideAndDontSave;
#if UNITY_6000_3_OR_NEWER
                _tempCollidersIds.Add(tempObj.GetEntityId(), spriteRenderer.gameObject);
#else
                _tempCollidersIds.Add(tempObj.GetInstanceID(), spriteRenderer.gameObject);
#endif
                tempObj.transform.SetParent(parentCollider.transform);
                tempObj.transform.position = spriteRenderer.transform.position;
                tempObj.transform.rotation = spriteRenderer.transform.rotation;
                tempObj.transform.localScale = spriteRenderer.transform.lossyScale;
#if UNITY_6000_3_OR_NEWER
                _tempCollidersTargets.Add(target.GetEntityId(), tempObj);
#else
                _tempCollidersTargets.Add(target.GetInstanceID(), tempObj);
#endif
                AddParentsIds(target);
                var boxCollider = tempObj.AddComponent<BoxCollider>();
                boxCollider.size = (Vector3)(spriteRenderer.sprite.rect.size / spriteRenderer.sprite.pixelsPerUnit)
                    + new Vector3(0f, 0f, 0.01f);
                var collider = spriteRenderer.GetComponent<Collider2D>();
                if (collider != null && !collider.isTrigger) continue;
                tempObj = new GameObject(name);
                tempObj.hideFlags = HideFlags.HideAndDontSave;
#if UNITY_6000_3_OR_NEWER
                _tempCollidersIds.Add(tempObj.GetEntityId(), spriteRenderer.gameObject);
#else
                _tempCollidersIds.Add(tempObj.GetInstanceID(), spriteRenderer.gameObject);
#endif
                tempObj.transform.SetParent(parentCollider.transform);
                tempObj.transform.position = spriteRenderer.transform.position;
                tempObj.transform.rotation = spriteRenderer.transform.rotation;
                tempObj.transform.localScale = spriteRenderer.transform.lossyScale;
                var boxCollider2D = tempObj.AddComponent<BoxCollider2D>();
                boxCollider2D.size = spriteRenderer.sprite.rect.size / spriteRenderer.sprite.pixelsPerUnit;
            }
        }

        private static GameObject CreateTempCollider(GameObject target, Mesh mesh)
        {
            if (target == null || mesh == null) return null;
            var differentVertices = new System.Collections.Generic.HashSet<Vector3>();
            foreach (var vertex in mesh.vertices)
            {
                if (!differentVertices.Contains(vertex)) differentVertices.Add(vertex);
                if (differentVertices.Count >= 3) break;
            }
            if (differentVertices.Count < 3) return null;

#if UNITY_6000_3_OR_NEWER
            if (_tempCollidersTargets.ContainsKey(target.GetEntityId()))
            {
                if (_tempCollidersTargets[target.GetEntityId()] != null)
                    return _tempCollidersTargets[target.GetEntityId()];
                else _tempCollidersTargets.Remove(target.GetEntityId());
            }
            var name = target.GetEntityId().ToString();
#else
            if (_tempCollidersTargets.ContainsKey(target.GetInstanceID()))
            {
                if (_tempCollidersTargets[target.GetInstanceID()] != null)
                    return _tempCollidersTargets[target.GetInstanceID()];
                else _tempCollidersTargets.Remove(target.GetInstanceID());
            }
            var name = target.GetInstanceID().ToString();
#endif
            var tempObj = new GameObject(name);
            tempObj.hideFlags = HideFlags.HideAndDontSave;
#if UNITY_6000_3_OR_NEWER
            _tempCollidersIds.Add(tempObj.GetEntityId(), target);
#else
            _tempCollidersIds.Add(tempObj.GetInstanceID(), target);
#endif
            tempObj.transform.SetParent(parentCollider.transform);
            tempObj.transform.position = target.transform.position;
            tempObj.transform.rotation = target.transform.rotation;
            tempObj.transform.localScale = target.transform.lossyScale;
#if UNITY_6000_3_OR_NEWER
            _tempCollidersTargets.Add(target.GetEntityId(), tempObj);
#else
            _tempCollidersTargets.Add(target.GetInstanceID(), tempObj);
#endif
            AddParentsIds(target);
            MeshUtils.AddCollider(mesh, tempObj);
            return tempObj;
        }

        private static void AddParentsIds(GameObject target)
        {
            var parents = target.GetComponentsInParent<Transform>();
            foreach (var parent in parents)
            {
#if UNITY_6000_3_OR_NEWER
                if (!_tempCollidersTargetParentsIds.ContainsKey(target.GetEntityId()))
                    _tempCollidersTargetParentsIds.Add(target.GetEntityId(),
                        new System.Collections.Generic.HashSet<EntityId>());
                _tempCollidersTargetParentsIds[target.GetEntityId()].Add(parent.gameObject.GetEntityId());
                if (!_tempCollidersTargetChildrenIds.ContainsKey(parent.gameObject.GetEntityId()))
                    _tempCollidersTargetChildrenIds.Add(parent.gameObject.GetEntityId(),
                        new System.Collections.Generic.HashSet<EntityId>());
                _tempCollidersTargetChildrenIds[parent.gameObject.GetEntityId()].Add(target.GetEntityId());
#else
                if (!_tempCollidersTargetParentsIds.ContainsKey(target.GetInstanceID()))
                    _tempCollidersTargetParentsIds.Add(target.GetInstanceID(), new System.Collections.Generic.HashSet<int>());
                _tempCollidersTargetParentsIds[target.GetInstanceID()].Add(parent.gameObject.GetInstanceID());
                if (!_tempCollidersTargetChildrenIds.ContainsKey(parent.gameObject.GetInstanceID()))
                    _tempCollidersTargetChildrenIds.Add(parent.gameObject.GetInstanceID(),
                        new System.Collections.Generic.HashSet<int>());
                _tempCollidersTargetChildrenIds[parent.gameObject.GetInstanceID()].Add(target.GetInstanceID());
#endif
            }
        }

        public static void CreateTempCollidersWithinFrustum(Camera cam)
        {
            var objects = PWBIO.boundsOctree.GetWithinFrustum(cam);
            updatingTempColliders = true;
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                AddTempCollider(obj);
            }
        }

        #endregion

        #region UPDATE & LIFECYCLE

        
        public static bool updatingTempColliders { get; set; }

        public static void UpdateTempColliders(bool force = false)
        {
            updatingTempColliders = true;
            DestroyTempColliders();
            if (!force && ToolController.current == ToolController.Tool.GRAVITY
                && !GravityToolController.settings.createTempColliders) return;
            bool createWithinFrustrum = ToolController.current == ToolController.Tool.GRAVITY
                && GravityToolController.settings.tempCollidersAction
                == GravityToolSettings.TempCollidersAction.CREATE_WITHIN_FRUSTRUM;
            if (createWithinFrustrum)
            {
                if (UnityEditor.SceneView.lastActiveSceneView != null)
                {
                    CreateTempCollidersWithinFrustum(UnityEditor.SceneView.lastActiveSceneView.camera);
                    return;
                }
            }
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; ++i)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene == null || !scene.IsValid() || !scene.isLoaded) continue;
                GameObject[] rootObjs;
                if (PWBIO.isInPrefabMode) rootObjs = new GameObject[] { PWBIO.prefabStage.prefabContentsRoot };
                else rootObjs = scene.GetRootGameObjects();
                foreach (var rootObj in rootObjs)
                {
                    if (rootObj == null) continue;
                    if (!rootObj.activeInHierarchy) continue;
                    AddTempCollider(rootObj);
                }
            }
        }

        public static void UpdateTempCollidersIfHierarchyChanged()
        {
            if (!ApplicationEventHandler.hierarchyChangedWhileUsingTools) return;
            UpdateTempColliders();
            ApplicationEventHandler.hierarchyChangedWhileUsingTools = false;
        }

        public static void UpdateTempCollidersTransforms(GameObject[] objects)
        {
            foreach (var obj in objects)
            {
#if UNITY_6000_3_OR_NEWER
                var parentId = obj.GetEntityId();
#else
                var parentId = obj.GetInstanceID();
#endif
                bool isParent = false;
                _tempCollidersRootTargets.Add(parentId);
                foreach (var childId in _tempCollidersTargetParentsIds.Keys)
                {
                    var parentsIds = _tempCollidersTargetParentsIds[childId];
                    if (parentsIds.Contains(parentId))
                    {
                        isParent = true;
                        break;
                    }
                }
                if (!isParent) continue;
                foreach (var id in _tempCollidersTargetChildrenIds[parentId].ToArray())
                {
                    if (!_tempCollidersTargets.ContainsKey(id))
                    {
                        _tempCollidersTargetChildrenIds[parentId].Remove(id);
                        continue;
                    }
                    var tempCollider = _tempCollidersTargets[id];
                    if (tempCollider == null)
                    {
                        _tempCollidersTargetChildrenIds[parentId].Remove(id);
                        _tempCollidersTargets.Remove(id);
                        continue;
                    }
#if UNITY_6000_3_OR_NEWER
                    var childObj = (GameObject)UnityEditor.EditorUtility.EntityIdToObject(id);
#else
                    var childObj = (GameObject)UnityEditor.EditorUtility.InstanceIDToObject(id);
#endif
                    if (childObj == null) continue;
                    tempCollider.transform.position = childObj.transform.position;
                    tempCollider.transform.rotation = childObj.transform.rotation;
                    tempCollider.transform.localScale = childObj.transform.lossyScale;
                }
            }
        }

        public static void SetActiveTempColliders(GameObject[] objects, bool value)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                if (!obj.activeInHierarchy) continue;
#if UNITY_6000_3_OR_NEWER
                var parentId = obj.GetEntityId();
#else
                var parentId = obj.GetInstanceID();
#endif
                bool isParent = false;
                foreach (var childId in _tempCollidersTargetParentsIds.Keys)
                {
                    var parentsIds = _tempCollidersTargetParentsIds[childId];
                    if (parentsIds.Contains(parentId))
                    {
                        isParent = true;
                        break;
                    }
                }
                if (!isParent) continue;
                foreach (var id in _tempCollidersTargetChildrenIds[parentId].ToArray())
                {
                    if (!_tempCollidersTargets.ContainsKey(id))
                    {
                        _tempCollidersTargetChildrenIds[parentId].Remove(id);
                        continue;
                    }
                    var tempCollider = _tempCollidersTargets[id];
                    if (tempCollider == null)
                    {
                        _tempCollidersTargetChildrenIds[parentId].Remove(id);
                        _tempCollidersTargets.Remove(id);
                        continue;
                    }
#if UNITY_6000_3_OR_NEWER
                    var childObj = (GameObject)UnityEditor.EditorUtility.EntityIdToObject(id);
#else
                    var childObj = (GameObject)UnityEditor.EditorUtility.InstanceIDToObject(id);
#endif
                    if (childObj == null) continue;
                    tempCollider.SetActive(value);
                    tempCollider.transform.position = childObj.transform.position;
                    tempCollider.transform.rotation = childObj.transform.rotation;
                    tempCollider.transform.localScale = childObj.transform.lossyScale;
                }
            }
        }

        #endregion

        #region DESTRUCTION
#if UNITY_6000_3_OR_NEWER
        public static void DestroyTempCollider(EntityId objId)
#else
        public static void DestroyTempCollider(int objId)
#endif
        {
            if (_tempCollidersRootTargets.Contains(objId)) _tempCollidersRootTargets.Remove(objId);
            if (!_tempCollidersTargets.ContainsKey(objId)) return;
            var temCollider = _tempCollidersTargets[objId];
            if (temCollider == null)
            {
                _tempCollidersTargets.Remove(objId);
                return;
            }
#if UNITY_6000_3_OR_NEWER
            var tempId = temCollider.GetEntityId();
#else
            var tempId = temCollider.GetInstanceID();
#endif
            _tempCollidersIds.Remove(tempId);
            _tempCollidersTargets.Remove(objId);
            _tempCollidersTargetParentsIds.Remove(objId);
            Object.DestroyImmediate(temCollider);
        }

        public static void DestroyTempColliders()
        {
            _tempCollidersIds.Clear();
            _tempCollidersTargets.Clear();
            _tempCollidersRootTargets.Clear();
            _tempCollidersTargetParentsIds.Clear();
            _tempCollidersTargetChildrenIds.Clear();
            var parentObj = GameObject.Find(PWBCore.PARENT_COLLIDER_NAME);
            if (parentObj != null) Object.DestroyImmediate(parentObj);
#if UNITY_6000_3_OR_NEWER
            _parentColliderId = EntityId.None;
#else
            _parentColliderId = -1;
#endif
        }

#endregion
    }
}
#pragma warning restore UDR0001
