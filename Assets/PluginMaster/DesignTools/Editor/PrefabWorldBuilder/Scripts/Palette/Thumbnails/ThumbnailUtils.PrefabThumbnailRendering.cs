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
    public partial class ThumbnailUtils
    {

        private static Material _bgMaterial = null;
        private static Cubemap _defaultCubemap = null;
        private static ThumbnailEditor _thumbnailEditor = null;

        public static void UpdateThumbnail(ThumbnailSettings settings,
            Texture2D thumbnailTexture, GameObject prefab, string thumbnailPath, bool savePng)
        {
            var magnitude = BoundsUtils.GetMagnitude(prefab.transform);
            if (_thumbnailEditor == null) _thumbnailEditor = new ThumbnailEditor();
            if (_thumbnailEditor.root != null) _thumbnailEditor.root.SetActive(true);
            _thumbnailEditor.settings = new ThumbnailSettings(settings);

            if (magnitude == 0)
            {
                if (_emptyTexture == null) _emptyTexture = Resources.Load<Texture2D>("Sprites/Empty");
                var pixels = _emptyTexture.GetPixels32();
                for (int i = 0; i < pixels.Length; ++i)
                {
                    if (pixels[i].a == 0) pixels[i] = _thumbnailEditor.settings.backgroudColor;
                }
                thumbnailTexture.SetPixels32(pixels);
                thumbnailTexture.Apply();
                return;
            }
#if UNITY_6000_4_OR_NEWER
            var foundLights = Object.FindObjectsByType<Light>();
#elif UNITY_2022_2_OR_NEWER
            var foundLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            var foundLights = Object.FindObjectsOfType<Light>();
#endif
            var sceneLights = new System.Collections.Generic.Dictionary<Light, int>(foundLights.Length);
            for (int i = 0; i < foundLights.Length; ++i)
                sceneLights.Add(foundLights[i], foundLights[i].cullingMask);

            const string rootName = "PWBThumbnailEditor";

            do
            {
                var obj = GameObject.Find(rootName);
                if (obj == null) break;
                else GameObject.DestroyImmediate(obj);
            } while (true);
            if (_thumbnailEditor.root == null) _thumbnailEditor.root = new GameObject();
            _thumbnailEditor.root.name = rootName;
            if (_thumbnailEditor.camera == null)
            {
                var camObj = new GameObject("PWBThumbnailEditorCam");
                _thumbnailEditor.camera = camObj.AddComponent<Camera>();
            }
            _thumbnailEditor.camera.transform.SetParent(_thumbnailEditor.root.transform);
            _thumbnailEditor.camera.transform.localPosition = new Vector3(0f, 1.2f, -4f);
            _thumbnailEditor.camera.transform.localRotation = Quaternion.Euler(17.5f, 0f, 0f);
            _thumbnailEditor.camera.fieldOfView = 20f;
            _thumbnailEditor.camera.clearFlags = CameraClearFlags.SolidColor;
            _thumbnailEditor.camera.backgroundColor = _thumbnailEditor.settings.backgroudColor;
            _thumbnailEditor.camera.cullingMask = layerMask;
            _thumbnailEditor.renderTexture = new RenderTexture(SIZE, SIZE, 24);
            _thumbnailEditor.camera.targetTexture = _thumbnailEditor.renderTexture;

            var originalAmbientMode = RenderSettings.ambientMode;
            var originalAmbientLight = RenderSettings.ambientLight;
            var originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            var originalAmbientGroundColor = RenderSettings.ambientGroundColor;
            var originalAmbientSkyColor = RenderSettings.ambientSkyColor;
            var originalAmbientIntensity = RenderSettings.ambientIntensity;
            var originalAmbientProbe = RenderSettings.ambientProbe;
            var originalReflectionMode = RenderSettings.defaultReflectionMode;
            var originalSkybox = RenderSettings.skybox;
            var originalFog = RenderSettings.fog;
            var originalFogColor = RenderSettings.fogColor;
            var originalFogStartDistance = RenderSettings.fogStartDistance;
            var originalFogEndDistance = RenderSettings.fogEndDistance;
            var originalFogDensity = RenderSettings.fogDensity;
            var originalFogMode = RenderSettings.fogMode;
            var originalHaloStrength = RenderSettings.haloStrength;
            var originalFlareFadeSpeed = RenderSettings.flareFadeSpeed;
            var originalFlareStrength = RenderSettings.flareStrength;
            var originalReflectionIntensity = RenderSettings.reflectionIntensity;
            var originalReflectionBounces = RenderSettings.reflectionBounces;
            var originalDefaultReflectionResolution = RenderSettings.defaultReflectionResolution;
            var originalSubtractiveShadowColor = RenderSettings.subtractiveShadowColor;
            var originalSun = RenderSettings.sun;
#if UNITY_2022_2_OR_NEWER
            var originalReflectionTexture = RenderSettings.customReflectionTexture;
#else
            var originalReflectionTexture = RenderSettings.customReflection;
#endif
            float intensityMultiplier = 0.7f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = _thumbnailEditor.settings.lightColor;
            RenderSettings.ambientEquatorColor = _thumbnailEditor.settings.backgroudColor;
            RenderSettings.ambientGroundColor = _thumbnailEditor.settings.backgroudColor;
            RenderSettings.ambientSkyColor = _thumbnailEditor.settings.backgroudColor;
            RenderSettings.ambientIntensity = _thumbnailEditor.settings.lightIntensity * intensityMultiplier;
            RenderSettings.ambientProbe = new UnityEngine.Rendering.SphericalHarmonicsL2();
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.fogColor = Color.clear;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = 1f;
            RenderSettings.fogDensity = 0f;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.haloStrength = 0f;
            RenderSettings.flareFadeSpeed = 1f;
            RenderSettings.flareStrength = 0f;
            RenderSettings.reflectionIntensity = _thumbnailEditor.settings.lightIntensity * intensityMultiplier;
            RenderSettings.reflectionBounces = 1;
            RenderSettings.defaultReflectionResolution = 128;
            RenderSettings.subtractiveShadowColor = Color.black;
            RenderSettings.sun = null;
            if (_defaultCubemap == null)
            {
                _defaultCubemap = new Cubemap(1, TextureFormat.RGB24, false);
                Color[] colors = { Color.white };
                for (int face = 0; face < 6; face++)
                {
                    _defaultCubemap.SetPixels(colors, (CubemapFace)face);
                }
                _defaultCubemap.Apply();
            }
#if UNITY_2022_2_OR_NEWER
            RenderSettings.customReflectionTexture = _defaultCubemap;
#else
            RenderSettings.customReflection = _defaultCubemap;
#endif

            if (_thumbnailEditor.light == null)
            {
                var lightObj = new GameObject("PWBThumbnailEditorLight");
                _thumbnailEditor.light = lightObj.AddComponent<Light>();
            }
            _thumbnailEditor.light.type = LightType.Directional;
            _thumbnailEditor.light.transform.SetParent(_thumbnailEditor.root.transform);
            _thumbnailEditor.light.transform.localRotation = Quaternion.Euler(_thumbnailEditor.settings.lightEuler);
            _thumbnailEditor.light.color = _thumbnailEditor.settings.lightColor;
            _thumbnailEditor.light.intensity = _thumbnailEditor.settings.lightIntensity;
            _thumbnailEditor.light.cullingMask = layerMask;
            if (_thumbnailEditor.pivot == null)
            {
                var pivotObj = new GameObject("PWBThumbnailEditorPivot");
                _thumbnailEditor.pivot = pivotObj.transform;
            }
            _thumbnailEditor.pivot.gameObject.layer = PWBCore.staticData.thumbnailLayer;
            _thumbnailEditor.pivot.transform.SetParent(_thumbnailEditor.root.transform);
            _thumbnailEditor.pivot.localPosition = _thumbnailEditor.settings.targetOffset;
            _thumbnailEditor.pivot.transform.localRotation = Quaternion.identity;
            _thumbnailEditor.pivot.transform.localScale = Vector3.one;

            Transform InstantiateBones(Transform source, Transform parent)
            {
                var obj = new GameObject();
                obj.name = source.name;
                obj.transform.SetParent(parent);
                obj.transform.position = source.position;
                obj.transform.rotation = source.rotation;
                obj.transform.localScale = source.localScale;
                foreach (Transform child in source) InstantiateBones(child, obj.transform);
                return obj.transform;
            }

            bool Requires(System.Type obj, System.Type requirement)
            {
                if (!System.Attribute.IsDefined(obj, typeof(RequireComponent)))
                    return false;

                var attrs = System.Attribute.GetCustomAttributes(obj, typeof(RequireComponent));
                for (int i = 0; i < attrs.Length; ++i)
                {
                    var rc = attrs[i] as RequireComponent;
                    if (rc != null && rc.m_Type0 != null && rc.m_Type0.IsAssignableFrom(requirement))
                        return true;
                }
                return false;
            }

            bool CanDestroy(GameObject go, System.Type t)
            {
                var comps = go.GetComponents<Component>();
                for (int i = 0; i < comps.Length; ++i)
                    if (Requires(comps[i].GetType(), t))
                        return false;
                return true;
            }

            void CopyComponents(GameObject source, GameObject destination)
            {
                var srcComps = source.GetComponentsInChildren<Component>();
                foreach (var srcComp in srcComps)
                {
                    if (srcComp is MonoBehaviour) continue;
                    var destComp = srcComp is Transform ? destination.transform : destination.AddComponent(srcComp.GetType());
                    UnityEditor.EditorUtility.CopySerialized(srcComp, destComp);
                }
                foreach (Transform srcChild in source.transform)
                {
                    var destChild = new GameObject();
                    destChild.transform.SetParent(destination.transform);
                    CopyComponents(srcChild.gameObject, destChild);
                }
            }

            GameObject InstantiateAndRemoveMonoBehaviours()
            {
                var obj = Object.Instantiate(prefab);
                var toBeDestroyed = new System.Collections.Generic.List<Component>(obj.GetComponentsInChildren<Component>());

                while (toBeDestroyed.Count > 0)
                {
                    var components = toBeDestroyed.ToArray();
                    int compCount = components.Length;
                    toBeDestroyed.Clear();
                    foreach (var comp in components)
                    {
                        if (comp is MonoBehaviour)
                        {
                            var monoBehaviour = comp as MonoBehaviour;
                            monoBehaviour.enabled = false;
                            monoBehaviour.runInEditMode = false;
                            if (CanDestroy(comp.gameObject, comp.GetType())) Object.DestroyImmediate(comp);
                            else toBeDestroyed.Add(comp);
                        }
                    }
                    if (compCount == toBeDestroyed.Count) break;
                }
                if (toBeDestroyed.Count > 0)
                {
                    var noMonoBehaviourObj = new GameObject();
                    CopyComponents(noMonoBehaviourObj, obj);
                    Object.DestroyImmediate(obj);
                    obj = noMonoBehaviourObj;
                }
                return obj;
            }

            _thumbnailEditor.target = InstantiateAndRemoveMonoBehaviours();

            var monoBehaviours = _thumbnailEditor.target.GetComponentsInChildren<MonoBehaviour>();
            foreach (var monoBehaviour in monoBehaviours)
                if (monoBehaviour != null) monoBehaviour.enabled = false;

            magnitude = BoundsUtils.GetMagnitude(_thumbnailEditor.target.transform);
            var targetScale = magnitude > 0 ? 1f / magnitude : 1f;
            var targetBounds = BoundsUtils.GetBoundsRecursive(_thumbnailEditor.target.transform);
            var localPosition = (_thumbnailEditor.target.transform.localPosition - targetBounds.center) * targetScale;
            _thumbnailEditor.target.transform.SetParent(_thumbnailEditor.pivot);
            _thumbnailEditor.target.transform.localPosition = localPosition;
            _thumbnailEditor.target.transform.localRotation = Quaternion.identity;
            _thumbnailEditor.target.transform.localScale = prefab.transform.localScale * targetScale;
            _thumbnailEditor.pivot.localScale = Vector3.one * _thumbnailEditor.settings.zoom;
            _thumbnailEditor.pivot.localRotation = Quaternion.Euler(_thumbnailEditor.settings.targetEuler);

            var bgObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bgObject.name = "PWBThumbnailEditorBg";
            if (_bgMaterial == null)
            {
#if PWB_HDRP
                _bgMaterial = new Material(Shader.Find("HDRP/Unlit"));
#else
                _bgMaterial = new Material(Shader.Find("Unlit/Color"));
#endif
            }
            _bgMaterial.color = _thumbnailEditor.settings.backgroudColor;

            var bgRenderer = bgObject.GetComponent<MeshRenderer>();
            bgRenderer.sharedMaterial = _bgMaterial;
            bgObject.transform.SetParent(_thumbnailEditor.root.transform);
            bgObject.transform.localPosition = new Vector3(0, -3, 10);
            bgObject.transform.localScale = new Vector3(30, 30, 0.1f);


#if PWB_HDRP || PWB_URP
#if UNITY_6000_4_OR_NEWER
            var foundVolumes = Object.FindObjectsByType<UnityEngine.Rendering.Volume>();
#elif UNITY_2022_2_OR_NEWER
            var foundVolumes = Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
#else
            var foundVolumes = Object.FindObjectsOfType<UnityEngine.Rendering.Volume>();    
#endif
            var sceneVolumes
                = new System.Collections.Generic.Dictionary<UnityEngine.Rendering.Volume, bool>(foundVolumes.Length);
            for (int i = 0; i < foundVolumes.Length; ++i)
            {
                sceneVolumes.Add(foundVolumes[i], foundVolumes[i].isActiveAndEnabled);
                foundVolumes[i].gameObject.SetActive(false);
            }

            var meshRenderersArray = _thumbnailEditor.target.GetComponentsInChildren<MeshRenderer>();
            var meshRenderers = new System.Collections.Generic.Dictionary<MeshRenderer,
                UnityEngine.Rendering.LightProbeUsage>(meshRenderersArray.Length);
            for (int i = 0; i < meshRenderersArray.Length; ++i)
            {
                meshRenderers.Add(meshRenderersArray[i], meshRenderersArray[i].lightProbeUsage);
                meshRenderersArray[i].lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }

            var skinnedMeshRenderArray = _thumbnailEditor.target.GetComponentsInChildren<SkinnedMeshRenderer>();
            var skinnedMeshRenderers = new System.Collections.Generic.Dictionary<SkinnedMeshRenderer,
                UnityEngine.Rendering.LightProbeUsage>(skinnedMeshRenderArray.Length);
            for (int i = 0; i < skinnedMeshRenderArray.Length; ++i)
            {
                skinnedMeshRenderers.Add(skinnedMeshRenderArray[i], skinnedMeshRenderArray[i].lightProbeUsage);
                skinnedMeshRenderArray[i].lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }
#endif

#if PWB_HDRP
            if (_thumbnailEditor.HDCamData == null)
            {
                _thumbnailEditor.HDCamData = _thumbnailEditor.camera.gameObject
                    .AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
            }
            _thumbnailEditor.HDCamData.volumeLayerMask = layerMask | 1;
            _thumbnailEditor.HDCamData.probeLayerMask = 0;
            _thumbnailEditor.HDCamData.clearColorMode
                = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
            _thumbnailEditor.HDCamData.backgroundColorHDR = _thumbnailEditor.settings.backgroudColor;
            _thumbnailEditor.HDCamData.antialiasing
                = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
            if (_thumbnailEditor.volume == null)
            {
                var volumeObj = new GameObject("PWBThumbnailEditorVolume");
                volumeObj.transform.SetParent(_thumbnailEditor.root.transform);
                _thumbnailEditor.volume = volumeObj.AddComponent<UnityEngine.Rendering.Volume>();
                _thumbnailEditor.volumeCollider = _thumbnailEditor.volume.gameObject.AddComponent<BoxCollider>();
            }
            _thumbnailEditor.volume.isGlobal = false;
            _thumbnailEditor.volume.priority = 1;
            _thumbnailEditor.volume.profile = Resources.Load<UnityEngine.Rendering.VolumeProfile>("ThumbnailVolume");
            UnityEngine.Rendering.HighDefinition.Exposure exposure = null;
            if (!_thumbnailEditor.volume.profile.Has<UnityEngine.Rendering.HighDefinition.Exposure>())
                exposure = _thumbnailEditor.volume.profile.Add<UnityEngine.Rendering.HighDefinition.Exposure>(true);
            else _thumbnailEditor.volume.profile.TryGet<UnityEngine.Rendering.HighDefinition.Exposure>(out exposure);
            if (exposure != null)
            {
                exposure.mode.value = UnityEngine.Rendering.HighDefinition.ExposureMode.AutomaticHistogram;
                exposure.meteringMode.value = UnityEngine.Rendering.HighDefinition.MeteringMode.CenterWeighted;
                exposure.limitMin.Override(13f);
                exposure.limitMax.Override(15f);
                exposure.compensation.Override(_thumbnailEditor.light.intensity);
            }

            _thumbnailEditor.volumeCollider.size = new Vector3(50, 50, 50);
#endif

            _thumbnailEditor.root.transform.position = new Vector3(10000, 10000, 10000);

            var children = _thumbnailEditor.root.GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                child.gameObject.layer = PWBCore.staticData.thumbnailLayer;
                child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            foreach (var light in sceneLights.Keys) light.cullingMask = light.cullingMask & ~layerMask;

            for (int i = 0; i < 9; ++i) _thumbnailEditor.camera.Render();

            foreach (var light in sceneLights.Keys) light.cullingMask = sceneLights[light];
#if PWB_HDRP  || PWB_URP
            foreach (var vol in sceneVolumes) vol.Key.gameObject.SetActive(vol.Value);
            foreach (var meshRenderer in meshRenderers) meshRenderer.Key.lightProbeUsage = meshRenderer.Value;
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
                skinnedMeshRenderer.Key.lightProbeUsage = skinnedMeshRenderer.Value;
#endif

            RenderTextureToTexture2D(_thumbnailEditor.camera.targetTexture, thumbnailTexture);

            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientLight = originalAmbientLight;
            RenderSettings.ambientEquatorColor = originalAmbientEquatorColor;
            RenderSettings.ambientGroundColor = originalAmbientGroundColor;
            RenderSettings.ambientSkyColor = originalAmbientSkyColor;
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            RenderSettings.ambientProbe = originalAmbientProbe;
            RenderSettings.defaultReflectionMode = originalReflectionMode;
            RenderSettings.skybox = originalSkybox;
            RenderSettings.fog = originalFog;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogStartDistance = originalFogStartDistance;
            RenderSettings.fogEndDistance = originalFogEndDistance;
            RenderSettings.fogDensity = originalFogDensity;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.haloStrength = originalHaloStrength;
            RenderSettings.flareFadeSpeed = originalFlareFadeSpeed;
            RenderSettings.flareStrength = originalFlareStrength;
            RenderSettings.reflectionIntensity = originalReflectionIntensity;
            RenderSettings.reflectionBounces = originalReflectionBounces;
            RenderSettings.defaultReflectionResolution = originalDefaultReflectionResolution;
            RenderSettings.subtractiveShadowColor = originalSubtractiveShadowColor;
            RenderSettings.sun = originalSun;

#if UNITY_2022_2_OR_NEWER
            RenderSettings.customReflectionTexture = originalReflectionTexture;
#else
            RenderSettings.customReflection = originalReflectionTexture;
#endif
            if (_thumbnailEditor.camera != null) _thumbnailEditor.camera.targetTexture = null;
            if (_thumbnailEditor.renderTexture != null) Object.DestroyImmediate(_thumbnailEditor.renderTexture);
            Object.DestroyImmediate(_thumbnailEditor.target);
            _thumbnailEditor.root.SetActive(false);
            if (savePng) SavePngResource(thumbnailTexture, thumbnailPath);
        }
    }
}
#pragma warning restore UDR0001
