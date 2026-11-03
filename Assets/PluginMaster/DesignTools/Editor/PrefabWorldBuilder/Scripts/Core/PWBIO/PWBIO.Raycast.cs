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
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask,
            QueryTriggerInteraction queryTriggerInteraction)
        {
            if (isInPrefabMode)
            {
                var physScene = prefabStage.scene.GetPhysicsScene();
                return physScene.Raycast(
                    ray.origin,
                    ray.direction,
                    out hitInfo,
                    maxDistance,
                    layerMask,
                    queryTriggerInteraction);
            }
            else return Physics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }
        private static readonly System.Collections.Generic.List<RaycastHit> _raycastAllHits
            = new System.Collections.Generic.List<RaycastHit>();
        public static RaycastHit[] RaycastAll(
        Ray ray,
        float maxDistance,
        int layerMask,
        QueryTriggerInteraction queryTriggerInteraction)
        {
            if (isInPrefabMode)
            {
                bool useGlobal = queryTriggerInteraction == QueryTriggerInteraction.UseGlobal;
                bool hitTriggers = useGlobal
                    ? Physics.queriesHitTriggers
                    : queryTriggerInteraction == QueryTriggerInteraction.Collide;

                _raycastAllHits.Clear();
                var root = prefabStage.prefabContentsRoot;
                var colliders = root.GetComponentsInChildren<Collider>(false);
                for (int i = 0; i < colliders.Length; i++)
                {
                    var col = colliders[i];
                    if (((1 << col.gameObject.layer) & layerMask) == 0) continue;
                    if (!hitTriggers && col.isTrigger) continue;
                    if (col.Raycast(ray, out RaycastHit hit, maxDistance))
                        _raycastAllHits.Add(hit);
                }
                _raycastAllHits.Sort((a, b) => a.distance.CompareTo(b.distance));
                return _raycastAllHits.ToArray();
            }
            return Physics.RaycastAll(ray, maxDistance, layerMask, queryTriggerInteraction);
        }
        private struct TerrainDataSimple
        {
            public float[,,] alphamaps;
            public Vector3 size;
            public TerrainLayer[] layers;
            public TerrainDataSimple(float[,,] alphamaps, Vector3 size, TerrainLayer[] layers)
                => (this.alphamaps, this.size, this.layers) = (alphamaps, size, layers);
        }
#if UNITY_6000_3_OR_NEWER
        private static readonly System.Collections.Generic.Dictionary<EntityId, TerrainDataSimple> _terrainCache
           = new System.Collections.Generic.Dictionary<EntityId, TerrainDataSimple>();
#else
        private static readonly System.Collections.Generic.Dictionary<int, TerrainDataSimple> _terrainCache
            = new System.Collections.Generic.Dictionary<int, TerrainDataSimple>();
#endif

        public static bool PWBToolRaycast(
            Ray mouseRay,
            out RaycastHit mouseHit,
            out GameObject collider,
            float maxDistance,
            LayerMask layerMask,
            bool paintOnPalettePrefabs,
            bool castOnMeshesWithoutCollider,
            string[] tags = null,
            TerrainLayer[] terrainLayers = null,
            System.Collections.Generic.HashSet<GameObject> exceptions = null,
            bool sameOriginAsRay = true,
            Vector3 origin = default,
            bool createTempColliders = false,
            bool ignoreSceneColliders = false)
        {
            mouseHit = new RaycastHit();
            collider = null;

            System.Collections.Generic.HashSet<string> tagSet = (tags != null && tags.Length > 0)
                ? new System.Collections.Generic.HashSet<string>(tags)
                : null;

            Plane originPlane = default;
            if (!sameOriginAsRay) originPlane = new Plane(mouseRay.direction, origin);

            bool validHit = false;
            bool valid = false;
#if UNITY_2021_1_OR_NEWER
            using (UnityEngine.Pool.DictionaryPool<GameObject, RaycastHit>
                .Get(out System.Collections.Generic.Dictionary<GameObject, RaycastHit> hitDictionary))
#else
            var hitDictionary = new System.Collections.Generic.Dictionary<GameObject, RaycastHit>();
#endif
            {
#if UNITY_2021_1_OR_NEWER
                using (UnityEngine.Pool.ListPool<GameObject>
                    .Get(out System.Collections.Generic.List<GameObject> nearbyObjectsList))
#else
                var nearbyObjectsList = new System.Collections.Generic.List<GameObject>();
#endif

                {
                    void MeshRaycast()
                    {
                        boundsOctree.GetColliding(nearbyObjectsList, mouseRay, maxDistance);
#if UNITY_2021_1_OR_NEWER
                        using (UnityEngine.Pool.ListPool<RaycastHit>
                            .Get(out System.Collections.Generic.List<RaycastHit> hitResultsList))
#else
                        var hitResultsList = new System.Collections.Generic.List<RaycastHit>();
#endif
#if UNITY_2021_1_OR_NEWER
                        using (UnityEngine.Pool.ListPool<GameObject>
                            .Get(out System.Collections.Generic.List<GameObject> collidersResultsList))
#else
                        var collidersResultsList = new System.Collections.Generic.List<GameObject>();
#endif
                        {
                            if (createTempColliders && !isInPrefabMode)
                            {
                                foreach (var obj in nearbyObjectsList)
                                {
                                    if(obj == null) continue;
                                    PWBCore.AddTempCollider(obj);
                                }
                                PhysicsRaycast();
                            }
                            else if (MeshUtils.RaycastAll(mouseRay, hitResultsList, collidersResultsList,
                                nearbyObjectsList, maxDistance, sameOriginAsRay, origin))
                            {
                                for (int i = 0; i < hitResultsList.Count; i++)
                                {
                                    var obj = collidersResultsList[i];
                                    float dist = sameOriginAsRay ? hitResultsList[i].distance
                                        : Mathf.Abs(originPlane.GetDistanceToPoint(hitResultsList[i].point));

                                    if (hitDictionary.TryGetValue(obj, out var existing))
                                    {
                                        if (dist < (sameOriginAsRay
                                            ? existing.distance : Mathf.Abs(originPlane.GetDistanceToPoint(existing.point))))
                                            hitDictionary[obj] = hitResultsList[i];
                                    }
                                    else
                                    {
                                        hitDictionary.Add(obj, hitResultsList[i]);
                                    }
                                }
                            }
                        }
                    }

                    bool PhysicsRaycast()
                    {
                        validHit = Raycast(mouseRay, out RaycastHit hitInfo, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
                        if (validHit)
                        {
                            var allHits = RaycastAll(mouseRay, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
                            for (int i = 0; i < allHits.Length; i++)
                            {
                                var obj = allHits[i].collider.gameObject;

                                if (ignoreSceneColliders
#if UNITY_6000_3_OR_NEWER
                                     && _sceneColliders.Contains(allHits[i].collider.GetEntityId()))
#else
                                    && _sceneColliders.Contains(allHits[i].collider.GetInstanceID()))
#endif
                                    continue;
                                float dist = sameOriginAsRay ? allHits[i].distance
                                    : Mathf.Abs(originPlane.GetDistanceToPoint(allHits[i].point));

                                if (hitDictionary.TryGetValue(obj, out var existing))
                                {
                                    if (dist < (sameOriginAsRay
                                        ? existing.distance : Mathf.Abs(originPlane.GetDistanceToPoint(existing.point))))
                                        hitDictionary[obj] = allHits[i];
                                }
                                else
                                {
                                    hitDictionary.Add(obj, allHits[i]);
                                }
                            }
                        }
                        return validHit;
                    }

                    if (castOnMeshesWithoutCollider)
                    {
                        if (ignoreSceneColliders)
                            MeshRaycast();
                        else if (!PhysicsRaycast())
                            MeshRaycast();
                    }
                    else
                    {
                        if (ignoreSceneColliders)
                        {
                            foreach (var id in _sceneColliders)
                            {
#if UNITY_6000_3_OR_NEWER
                                var c = UnityEditor.EditorUtility.EntityIdToObject(id) as Collider;
#else
                                var c = UnityEditor.EditorUtility.InstanceIDToObject(id) as Collider;
#endif
                                if (c != null) PWBCore.AddTempCollider(c.gameObject);
                            }
                            PhysicsRaycast();
                        }
                        else PhysicsRaycast();
                    }
                }
                float minDistance = float.MaxValue;

                foreach (var pair in hitDictionary)
                {
                    var obj = ResolveOriginal(pair.Key);
                    var hitInfo = pair.Value;

                    float dist = sameOriginAsRay ? hitInfo.distance : originPlane.GetDistanceToPoint(hitInfo.point);

                    if (Mathf.Abs(dist) < minDistance && FiltersPassed(obj, hitInfo.point, paintOnPalettePrefabs, tagSet,
                        terrainLayers, exceptions))
                    {
                        minDistance = dist;
                        collider = obj;
                        mouseHit = hitInfo;
                        mouseHit.distance = dist;
                        valid = true;
                    }
                }
            }
            return valid;
        }

        private static bool ResolveTemp(GameObject obj)
        {
            var parent = obj.transform.parent;
#if UNITY_6000_3_OR_NEWER
            if (parent == null) return false;
            var parentId = parent.gameObject.GetEntityId();
            return parentId != EntityId.None && parentId == PWBCore.parentColliderId;
#else
            return parent != null && parent.gameObject.GetInstanceID() == PWBCore.parentColliderId;
#endif
        }

        private static GameObject ResolveOriginal(GameObject obj)
        {
#if UNITY_6000_3_OR_NEWER
            return ResolveTemp(obj) ? PWBCore.GetGameObjectFromTempColliderId(obj.GetEntityId()) : obj;
#else
            return ResolveTemp(obj) ? PWBCore.GetGameObjectFromTempColliderId(obj.GetInstanceID()) : obj;
#endif
        }

        private static bool IsVisible(ref GameObject obj)
        {
            if (obj == null) return false;
            var parentRenderer = obj.GetComponentInParent<Renderer>();
            var parentTerrain = obj.GetComponentInParent<Terrain>();
            if (parentRenderer != null) obj = parentRenderer.gameObject;
            else if (parentTerrain != null) obj = parentTerrain.gameObject;
            else
            {
                var parent = obj.transform.parent;
                if (parent != null)
                {
                    var siblingRenderer = parent.GetComponentInChildren<Renderer>();
                    var siblingTerrain = parent.GetComponentInChildren<Terrain>();
                    if (siblingRenderer != null) obj = parent.gameObject;
                    else if (siblingTerrain != null) obj = parent.gameObject;

                }
            }
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                foreach (var renderer in renderers)
                    if (renderer.enabled) return true;
            }
            var terrains = obj.GetComponentsInChildren<Terrain>();
            if (terrains.Length > 0)
            {
                foreach (var terrain in terrains)
                    if (terrain.enabled) return true;
            }
            return false;
        }

        private static bool IsVisible(GameObject obj)
        {
            obj = PWBCore.GetGameObjectFromTempCollider(obj);
            return IsVisible(ref obj);
        }

        private static bool FiltersPassed(
            GameObject obj,
            Vector3 hitPoint,
            bool paintOnPalettePrefabs,
            System.Collections.Generic.HashSet<string> tagSet,
            TerrainLayer[] terrainLayers,
            System.Collections.Generic.HashSet<GameObject> exceptions)
        {
            if (obj == null || !IsVisible(obj))
                return false;

            if (exceptions != null && exceptions.Count > 0 && exceptions.Contains(obj))
                return false;

            if (tagSet != null && obj.tag != "untagged" && !tagSet.Contains(obj.tag))
                return false;

            if (!paintOnPalettePrefabs && PaletteManager.selectedPalette.ContainsSceneObject(obj))
                return false;

            if (terrainLayers != null && terrainLayers.Length > 0)
                return TerrainCheck(obj, hitPoint, terrainLayers);
            return true;
        }

        private static bool TerrainCheck(GameObject obj, Vector3 hitPoint, TerrainLayer[] terrainLayers)
        {
            var terrain = obj.GetComponent<Terrain>();
            if (terrain == null) return true;
#if UNITY_6000_3_OR_NEWER
            var id = terrain.GetEntityId();
#else
            var id = terrain.GetInstanceID();
#endif
            if (!_terrainCache.TryGetValue(id, out var data))
            {
                var td = terrain.terrainData;
                if (td == null) return false;
                data = new TerrainDataSimple(td.GetAlphamaps(0, 0, td.alphamapWidth, td.alphamapHeight),
                    td.size, td.terrainLayers);
                _terrainCache[id] = data;
            }

            Vector3 local = terrain.transform.InverseTransformPoint(hitPoint);
            int x = Mathf.Clamp(Mathf.RoundToInt(local.x / data.size.x * data.alphamaps.GetLength(1)),
                0, data.alphamaps.GetLength(1) - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(local.z / data.size.z * data.alphamaps.GetLength(0)),
                0, data.alphamaps.GetLength(0) - 1);

            int layerIdx = 0;
            for (int k = 1; k < data.alphamaps.GetLength(2); k++)
                if (data.alphamaps[z, x, k] > 0.5f)
                {
                    layerIdx = k;
                    break;
                }

            foreach (var layer in terrainLayers)
                if (layer == data.layers[layerIdx])
                    return true;

            return false;
        }

        public static float GetDistanceToSurface(Vector3[] vertices, Matrix4x4 TRS, Vector3 direction, float magnitude,
          bool paintOnPalettePrefabs, bool castOnMeshesWithoutCollider, bool ignoreSceneColliders, out Transform surface,
          GameObject prefab, System.Collections.Generic.HashSet<GameObject> exceptions = null, bool createTemColliders = true)
        {
            surface = null;
            var distance = 0f;
            void GetDistance(float height, Vector3 direction, out GameObject collider)
            {
                collider = null;
                var positiveDistance = float.MinValue;
                var negativeDistance = float.MinValue;
                foreach (var vertex in vertices)
                {
                    var origin = TRS.MultiplyPoint(vertex);
                    var ray = new Ray(origin - (direction * height), direction);
                    if (PWBToolRaycast(ray, out RaycastHit hitInfo, out GameObject rayCollider,
                        float.MaxValue, -1, paintOnPalettePrefabs, castOnMeshesWithoutCollider,
                        tags: null, terrainLayers: null, exceptions, sameOriginAsRay: false, origin, createTemColliders,
                        ignoreSceneColliders: ignoreSceneColliders))
                    {
                        var prevPosDistance = positiveDistance;
                        var prevNegDistance = negativeDistance;
                        if (hitInfo.distance >= 0) positiveDistance = Mathf.Max(hitInfo.distance, positiveDistance);
                        else negativeDistance = Mathf.Max(hitInfo.distance, negativeDistance);
                        if (Mathf.Approximately(prevPosDistance, positiveDistance)
                            && Mathf.Approximately(prevNegDistance, negativeDistance)) continue;
                        distance = positiveDistance >= 0 ? positiveDistance : negativeDistance;
                        collider = rayCollider;
                    }
                }
                if (Mathf.Approximately(distance, float.MinValue) || Mathf.Approximately(distance, float.MaxValue))
                    distance = 0;
            }
            var scale = TRS.lossyScale;
            var scaleMult = Mathf.Max(scale.x + scale.y + scale.z, 1) * 9;
            float hMult = magnitude * scaleMult;
            GetDistance(hMult, direction, out GameObject surfaceCollider);
            if (surfaceCollider != null) surface = surfaceCollider.transform;
            return distance;
        }

        public static float GetBottomDistanceToSurface(Vector3[] bottomVertices, Matrix4x4 TRS,
            float magnitude, bool paintOnPalettePrefabs, bool castOnMeshesWithoutCollider, bool ignoreSceneColliders,
            out Transform surface, System.Collections.Generic.HashSet<GameObject> exceptions = null)
        {
            surface = null;
            var distance = 0f;
            void GetDistance(float height, Vector3 direction, out GameObject collider)
            {
                collider = null;
                var positiveDistance = float.MinValue;
                var negativeDistance = float.MinValue;
                foreach (var vertex in bottomVertices)
                {
                    var origin = TRS.MultiplyPoint(vertex);
                    var ray = new Ray(origin - (direction * height), direction);
                    if (PWBToolRaycast(ray, out RaycastHit hitInfo, out GameObject rayCollider, float.MaxValue, -1,
                        paintOnPalettePrefabs, castOnMeshesWithoutCollider, tags: null, terrainLayers: null, exceptions,
                        sameOriginAsRay: false, origin, createTempColliders: true,
                        ignoreSceneColliders: ignoreSceneColliders))
                    {
                        var prevPosDistance = positiveDistance;
                        var prevNegDistance = negativeDistance;
                        if (hitInfo.distance >= 0) positiveDistance = Mathf.Max(hitInfo.distance, positiveDistance);
                        else negativeDistance = Mathf.Max(hitInfo.distance, negativeDistance);
                        if (Mathf.Approximately(prevPosDistance, positiveDistance)
                            && Mathf.Approximately(prevNegDistance, negativeDistance)) continue;
                        distance = positiveDistance >= 0 ? positiveDistance : negativeDistance;
                        collider = rayCollider;
                    }
                }
                if (Mathf.Approximately(distance, float.MinValue) || Mathf.Approximately(distance, float.MaxValue))
                    distance = 0;
            }
            var scale = TRS.lossyScale;
            var scaleMult = Mathf.Max(scale.x + scale.y + scale.z, 1) * 9;
            float hMult = magnitude * scaleMult;
            var down = (TRS.rotation * Vector3.down).normalized;
            GetDistance(hMult, down, out GameObject surfaceCollider);
            if (surfaceCollider != null) surface = surfaceCollider.transform;
            return distance;
        }

        public static float GetBottomDistanceToSurfaceSigned(Vector3[] bottomVertices, Matrix4x4 TRS,
            float maxDistance, bool paintOnPalettePrefabs, bool castOnMeshesWithoutCollider, bool ignoreSceneColliders)
        {
            float distance = 0f;
            var down = Vector3.down;
            foreach (var vertex in bottomVertices)
            {
                var origin = TRS.MultiplyPoint(vertex);
                var ray = new Ray(origin - down * maxDistance, down);
                if (PWBToolRaycast(ray, out RaycastHit hitInfo, out GameObject collider,
                    float.MaxValue, -1, paintOnPalettePrefabs, castOnMeshesWithoutCollider,
                    ignoreSceneColliders: ignoreSceneColliders))
                {
                    var d = hitInfo.distance - maxDistance;
                    if (Mathf.Abs(d) > Mathf.Abs(distance)) distance = d;
                }
            }
            return distance;
        }

        public static float GetPivotDistanceToSurfaceSigned(Vector3 pivot,
            float maxDistance, bool paintOnPalettePrefabs, bool castOnMeshesWithoutCollider, bool ignoreSceneColliders,
            out Transform surface, System.Collections.Generic.HashSet<GameObject> exceptions = null)
        {
            surface = null;
            var ray = new Ray(pivot + Vector3.up * maxDistance, Vector3.down);
            if (PWBToolRaycast(ray, out RaycastHit hitInfo, out GameObject collider, float.MaxValue, -1, paintOnPalettePrefabs,
                castOnMeshesWithoutCollider, exceptions: exceptions, ignoreSceneColliders: ignoreSceneColliders))
            {
                surface = collider.transform;
                return hitInfo.distance - maxDistance;
            }
            return 0;
        }
    }
}
#pragma warning restore UDR0001
