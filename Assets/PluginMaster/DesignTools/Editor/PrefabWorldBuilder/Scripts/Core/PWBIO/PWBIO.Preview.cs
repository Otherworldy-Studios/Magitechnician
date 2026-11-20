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
using UnityEngine.Rendering;

namespace PluginMaster
{
    public static partial class PWBIO
    {

        private static Mesh ReversedMesh(Mesh mesh, int subMeshCount)
        {
            var reversed = (Mesh)GameObject.Instantiate(mesh);
            reversed.name = mesh.name + "_reversed";

            if (mesh.normals != null && mesh.normals.Length == mesh.vertexCount
                && mesh.tangents != null && mesh.tangents.Length == mesh.vertexCount)
            {
                for (int i = 0; i < reversed.normals.Length; ++i)
                {
                    reversed.normals[i] = -reversed.normals[i];
                    reversed.tangents[i].x = -reversed.tangents[i].x;
                }
            }

            reversed.subMeshCount = subMeshCount;
            for (int i = 0; i < subMeshCount; ++i)
            {
                var triangles = mesh.GetTriangles(i);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    int tmp = triangles[t + 1];
                    triangles[t + 1] = triangles[t + 2];
                    triangles[t + 2] = tmp;
                }
                reversed.SetTriangles(triangles, i);
            }
            return reversed;
        }

        private struct MeshAndRenderer
        {
            public Mesh mesh;
            public Mesh reversedMesh;
            public Matrix4x4 localToWorldMatrix;
            public Material[] materials;
            public int subMeshCount;
            public Renderer renderer;
            public MeshAndRenderer(Mesh mesh, Mesh reversedMesh, Matrix4x4 localToWorldMatrix, Material[] materials,
                int subMeshCount, Renderer renderer)
            {
                this.mesh = mesh; this.reversedMesh = reversedMesh;
                this.localToWorldMatrix = localToWorldMatrix; this.materials = materials;
                this.subMeshCount = subMeshCount; this.renderer = renderer;
            }
        }

#if UNITY_6000_3_OR_NEWER
        private static System.Collections.Generic.Dictionary<EntityId, MeshAndRenderer[]> _meshesAndRenderers
            = new System.Collections.Generic.Dictionary<EntityId, MeshAndRenderer[]>();
#else
        private static System.Collections.Generic.Dictionary<int, MeshAndRenderer[]> _meshesAndRenderers
            = new System.Collections.Generic.Dictionary<int, MeshAndRenderer[]>();
#endif

        private struct SpriteAndBounds
        {
            public SpriteRenderer spriteRenderer;
            public Bounds bounds;
            public MaterialPropertyBlock mpb;
            public SpriteAndBounds(SpriteRenderer spriteRenderer, Bounds bounds, MaterialPropertyBlock mpb)
            { this.spriteRenderer = spriteRenderer; this.bounds = bounds; this.mpb = mpb; }
        }
#if UNITY_6000_3_OR_NEWER
        private static System.Collections.Generic.Dictionary<EntityId, System.Collections.Generic.HashSet<SpriteAndBounds>>
            _spriteRenderers = new System.Collections.Generic.Dictionary<EntityId,
                System.Collections.Generic.HashSet<SpriteAndBounds>>();
#else
        private static System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<SpriteAndBounds>>
            _spriteRenderers = new System.Collections.Generic.Dictionary<int,
                System.Collections.Generic.HashSet<SpriteAndBounds>>();
#endif

        public static void ClearPreviewDictionaries()
        {
            _meshesAndRenderers.Clear();
            _spriteRenderers.Clear();
            _pendingDraws.Clear();
            _pendingRawDraws.Clear();
            _negScaleReversedCache.Clear();
        }

        private struct PendingDraw
        {
            public GameObject prefab;
            public Matrix4x4 rootToWorld;
            public int layer;
            public bool redMaterial;
            public bool reverseTriangles;
            public bool flipX;
            public bool flipY;
        }
        private static readonly System.Collections.Generic.List<PendingDraw> _pendingDraws
            = new System.Collections.Generic.List<PendingDraw>();

        private struct PendingRawDraw
        {
            public Mesh mesh;
            public Matrix4x4 matrix;
            public Material material;
            public int subMeshIdx;
            public int layer;
            public MaterialPropertyBlock mpb;
        }
        private static readonly System.Collections.Generic.List<PendingRawDraw> _pendingRawDraws
            = new System.Collections.Generic.List<PendingRawDraw>();

        private static readonly System.Collections.Generic.Dictionary<Mesh, Mesh> _negScaleReversedCache
            = new System.Collections.Generic.Dictionary<Mesh, Mesh>();

        public static void EnqueueRawPreviewDraw(Mesh mesh, Matrix4x4 matrix, Material material,
            int subMeshIdx, int layer = 0, MaterialPropertyBlock mpb = null)
        {
            if (mesh == null || material == null) return;
            if (matrix.determinant < 0f)
            {
                if (!_negScaleReversedCache.TryGetValue(mesh, out var reversed) || reversed == null)
                {
                    reversed = ReversedMesh(mesh, mesh.subMeshCount);
                    _negScaleReversedCache[mesh] = reversed;
                }
                mesh = reversed;
            }

            _pendingRawDraws.Add(new PendingRawDraw
            {
                mesh = mesh,
                matrix = matrix,
                material = material,
                subMeshIdx = subMeshIdx,
                layer = layer,
                mpb = mpb,
            });
        }

        public static void BeginPreviewFrame()
        {
            _pendingDraws.Clear();
            _pendingRawDraws.Clear();
        }

        private static void PreviewBrushItem(GameObject prefab, Matrix4x4 rootToWorld, int layer,
            Camera camera, bool redMaterial, bool reverseTriangles, bool flipX, bool flipY)
        {
            if (prefab == null) return;
            EnsurePrefabCached(prefab, reverseTriangles);
            _pendingDraws.Add(new PendingDraw
            {
                prefab = prefab,
                rootToWorld = rootToWorld,
                layer = layer,
                redMaterial = redMaterial,
                reverseTriangles = reverseTriangles,
                flipX = flipX,
                flipY = flipY,
            });
        }

        private static void EnsurePrefabCached(GameObject prefab, bool reverseTriangles)
        {
#if UNITY_6000_3_OR_NEWER
            var id = prefab.GetEntityId();
#else
            var id = prefab.GetInstanceID();
#endif
            if (!_meshesAndRenderers.ContainsKey(id))
            {
                var meshesAndRenderers = new System.Collections.Generic.List<MeshAndRenderer>();
                var renderers = prefab.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null) continue;
                    var mesh = filter.sharedMesh;
                    if (mesh == null || mesh.subMeshCount == 0) continue;
                    var materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0) continue;
                    var submeshCount = Mathf.Min(mesh.subMeshCount, materials.Length);
                    Mesh reversedMesh = reverseTriangles ? ReversedMesh(mesh, submeshCount) : null;
                    meshesAndRenderers.Add(new MeshAndRenderer(mesh, reversedMesh,
                        filter.transform.localToWorldMatrix, materials, submeshCount, renderer));
                }
                var skinedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var renderer in skinedMeshRenderers)
                {
                    var mesh = renderer.sharedMesh;
                    if (mesh == null) continue;
                    var materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0) continue;
                    var submeshCount = Mathf.Min(mesh.subMeshCount, materials.Length);
                    Mesh reversedMesh = reverseTriangles ? ReversedMesh(mesh, submeshCount) : null;
                    meshesAndRenderers.Add(new MeshAndRenderer(mesh, reversedMesh,
                        renderer.transform.localToWorldMatrix, materials, submeshCount, renderer));
                }
                _meshesAndRenderers.Add(id, meshesAndRenderers.ToArray());
            }

            if (!_spriteRenderers.ContainsKey(id))
            {
                System.Collections.Generic.HashSet<SpriteAndBounds> spritesAndBounds = null;
                var spriteRenderersArray = prefab.GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0; i < spriteRenderersArray.Length; ++i)
                {
                    var sr = spriteRenderersArray[i];
                    if (sr == null || !sr.enabled || sr.sprite == null || !sr.gameObject.activeSelf) continue;
                    var bounds = BoundsUtils.GetBoundsRecursive(prefab.transform);
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetTexture("_MainTex", sr.sprite.texture);
                    mpb.SetColor("_Color", sr.color);
                    if (spritesAndBounds == null)
                        spritesAndBounds = new System.Collections.Generic.HashSet<SpriteAndBounds>();
                    spritesAndBounds.Add(new SpriteAndBounds(sr, bounds, mpb));
                }
                _spriteRenderers[id] = spritesAndBounds;
            }
        }

        private static void DrawPending(in PendingDraw d, Camera camera)
        {
            var prefab = d.prefab;
            if (prefab == null) return;
#if UNITY_6000_3_OR_NEWER
            var id = prefab.GetEntityId();
#else
            var id = prefab.GetInstanceID();
#endif
            if (!_meshesAndRenderers.TryGetValue(id, out var meshAndRenderers)) return;

            for (int i = 0; i < meshAndRenderers.Length; ++i)
            {
                var item = meshAndRenderers[i];
                var mesh = item.mesh;
                var childToWorld = d.rootToWorld * item.localToWorldMatrix;

                if (item.renderer is SkinnedMeshRenderer smr && !d.redMaterial)
                {
                    var rootBone = smr.rootBone;
                    if (rootBone != null)
                    {
                        while (rootBone.parent != null && rootBone.parent != prefab.transform)
                            rootBone = rootBone.parent;
                        var rotation = rootBone.rotation;
                        var position = rootBone.position; position.y = 0f;
                        var scale = rootBone.localScale;
                        childToWorld = d.rootToWorld * Matrix4x4.TRS(position, rotation, scale);
                    }
                }

                bool needsReverse = d.reverseTriangles || childToWorld.determinant < 0f;
                Mesh meshToDraw = mesh;
                if (needsReverse)
                {
                    if (item.reversedMesh == null)
                    {
                        item.reversedMesh = ReversedMesh(mesh, item.subMeshCount);
                        meshAndRenderers[i] = item;
                    }
                    meshToDraw = item.reversedMesh;
                }

                if (!d.redMaterial)
                {
                    for (int subMeshIdx = 0; subMeshIdx < item.subMeshCount; ++subMeshIdx)
                    {
                        var material = item.materials[subMeshIdx];
                        if (material == null) continue;
                        Graphics.DrawMesh(meshToDraw, childToWorld, material, d.layer, camera, subMeshIdx);
                    }
                }
                else
                {
                    for (int subMeshIdx = 0; subMeshIdx < meshToDraw.subMeshCount; ++subMeshIdx)
                        Graphics.DrawMesh(meshToDraw, childToWorld, transparentRedMaterial, d.layer,
                            camera, subMeshIdx);
                }
            }

            if (_spriteRenderers.TryGetValue(id, out var spritesAndBounds)
                && spritesAndBounds != null && spritesAndBounds.Count > 0)
            {
                foreach (var snb in spritesAndBounds)
                    DrawSprite(snb.spriteRenderer, d.rootToWorld, camera, snb.bounds,
                        d.flipX, d.flipY, snb.mpb, d.layer);
            }
        }

        private static Mesh quadMesh = null;
        private static void DrawSprite(SpriteRenderer renderer, Matrix4x4 matrix,
            Camera camera, Bounds objectBounds, bool flipX, bool flipY, MaterialPropertyBlock mpb, int layer)
        {
            if (quadMesh == null)
            {
                quadMesh = new Mesh
                {
                    vertices = new[] { new Vector3(-.5f,.5f,0), new Vector3(.5f,.5f,0),
                        new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0) },
                    normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward },
                    triangles = new[] { 0, 2, 3, 3, 1, 0 }
                };
            }
            var minUV = new Vector2(float.MaxValue, float.MaxValue);
            var maxUV = new Vector2(float.MinValue, float.MinValue);
            foreach (var uv in renderer.sprite.uv)
            { minUV = Vector2.Min(minUV, uv); maxUV = Vector2.Max(maxUV, uv); }
            var uvs = new Vector2[]{ new Vector2(minUV.x,maxUV.y), new Vector2(maxUV.x,maxUV.y),
                new Vector2(minUV.x,minUV.y), new Vector2(maxUV.x,minUV.y) };
            void ToggleFlip(ref bool flip) => flip = !flip;
            if (renderer.flipX) ToggleFlip(ref flipX);
            if (renderer.flipY) ToggleFlip(ref flipY);
            if (flipX) { uvs[0].x = maxUV.x; uvs[1].x = minUV.x; uvs[2].x = maxUV.x; uvs[3].x = minUV.x; }
            if (flipY) { uvs[0].y = minUV.y; uvs[1].y = minUV.y; uvs[2].y = maxUV.y; uvs[3].y = maxUV.y; }
            quadMesh.uv = uvs;
            var pivotToCenter = (renderer.sprite.rect.size / 2 - renderer.sprite.pivot) / renderer.sprite.pixelsPerUnit;
            if (renderer.flipX) pivotToCenter.x = -pivotToCenter.x;
            if (renderer.flipY) pivotToCenter.y = -pivotToCenter.y;
            matrix *= Matrix4x4.Translate(pivotToCenter);
            matrix *= renderer.transform.localToWorldMatrix;
            matrix *= Matrix4x4.Scale(new Vector3(
                renderer.sprite.textureRect.width / renderer.sprite.pixelsPerUnit,
                renderer.sprite.textureRect.height / renderer.sprite.pixelsPerUnit, 1));
            Graphics.DrawMesh(quadMesh, matrix, renderer.sharedMaterial, layer, camera, 0, mpb);
        }

        private static BrushstrokeItem[] _brushstroke = null;
        private struct PreviewData
        {
            public readonly GameObject prefab;
            public readonly Matrix4x4 rootToWorld;
            public readonly int layer;
            public readonly bool flipX, flipY;
            public PreviewData(GameObject prefab, Matrix4x4 rootToWorld, int layer, bool flipX, bool flipY)
            {
                this.prefab = prefab; this.rootToWorld = rootToWorld; this.layer = layer;
                this.flipX = flipX; this.flipY = flipY;
            }
        }
        private static System.Collections.Generic.List<PreviewData> _previewData
            = new System.Collections.Generic.List<PreviewData>();

        private static bool PreviewIfBrushtrokestaysTheSame(out BrushstrokeItem[] brushstroke,
            Camera camera, bool forceUpdate)
        {
            brushstroke = BrushstrokeManager.brushstroke;
            if (!forceUpdate && _brushstroke != null && BrushstrokeManager.BrushstrokeEqual(brushstroke, _brushstroke))
            {
                foreach (var p in _previewData)
                    PreviewBrushItem(p.prefab, p.rootToWorld, p.layer, camera, false, false, p.flipX, p.flipY);
                return true;
            }
            _brushstroke = BrushstrokeManager.brushstrokeClone;
            _previewData.Clear();
            return false;
        }

        private static System.Collections.Generic.Dictionary<long, PreviewData[]> _persistentPreviewData
            = new System.Collections.Generic.Dictionary<long, PreviewData[]>();
        private static System.Collections.Generic.Dictionary<long, BrushstrokeItem[]> _persistentLineBrushstrokes
            = new System.Collections.Generic.Dictionary<long, BrushstrokeItem[]>();

        private static void PreviewPersistent(Camera camera)
        {
            foreach (var arr in _persistentPreviewData.Values)
                foreach (var p in arr)
                    PreviewBrushItem(p.prefab, p.rootToWorld, p.layer, camera, false, false, p.flipX, p.flipY);
        }

        [UnityEditor.InitializeOnLoadMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        private static void UnregisterOnReload()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
#if UNITY_2021_1_OR_NEWER
                RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
#else
                RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
#endif
                Camera.onPreCull -= OnCameraPreCull;
            };
        }

        
        public static void RegisterPreviewSRPCallbacks()
        {
#if UNITY_2021_1_OR_NEWER
            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
#else
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
#endif
            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPreCull += OnCameraPreCull;
        }

#if UNITY_2021_1_OR_NEWER
        private static void OnBeginContextRendering(ScriptableRenderContext _,
            System.Collections.Generic.List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; ++i)
            {
                var cam = cameras[i];
                if (cam == null || cam.cameraType != CameraType.SceneView) continue;
                FlushPendingFor(cam);
            }
        }
#else
        private static void OnBeginFrameRendering(ScriptableRenderContext _, Camera[] cameras)
        {
            for (int i = 0; i < cameras.Length; ++i)
            {
                var cam = cameras[i];
                if (cam == null || cam.cameraType != CameraType.SceneView) continue;
                FlushPendingFor(cam);
            }
        }
#endif
        private static void OnCameraPreCull(Camera cam)
        {
            if (GraphicsSettings.currentRenderPipeline != null) return;
            if (cam == null || cam.cameraType != CameraType.SceneView) return;
            FlushPendingFor(cam);
        }

        private static void FlushPendingFor(Camera cam)
        {
            for (int i = 0; i < _pendingDraws.Count; ++i)
                DrawPending(_pendingDraws[i], cam);

            for (int i = 0; i < _pendingRawDraws.Count; ++i)
            {
                var r = _pendingRawDraws[i];
                if (r.mpb != null)
                    Graphics.DrawMesh(r.mesh, r.matrix, r.material, r.layer, cam, r.subMeshIdx, r.mpb);
                else
                    Graphics.DrawMesh(r.mesh, r.matrix, r.material, r.layer, cam, r.subMeshIdx);
            }
        }


        #region PREVIEW LIFECYCLE
        
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterPreviewLifecycleHooks()
        {

            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= OnSceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += OnSceneClosed;

            UnityEditor.SceneManagement.EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
            UnityEditor.SceneManagement.EditorSceneManager.newSceneCreated += OnNewSceneCreated;

            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        }

        private static void OnSceneClosed(UnityEngine.SceneManagement.Scene scene) => ClearPreviewState();

        private static void OnNewSceneCreated(UnityEngine.SceneManagement.Scene scene,
            UnityEditor.SceneManagement.NewSceneSetup setup,
            UnityEditor.SceneManagement.NewSceneMode mode) => ClearPreviewState();

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.ExitingEditMode
                || change == UnityEditor.PlayModeStateChange.EnteredEditMode)
                ClearPreviewState();
        }

        private static void ClearPreviewState()
        {
            ClearPreviewDictionaries();
            _previewData.Clear();
            _persistentPreviewData.Clear();
            _persistentLineBrushstrokes.Clear();
            _brushstroke = null;
        }
        #endregion

    }
}
#pragma warning restore UDR0001
