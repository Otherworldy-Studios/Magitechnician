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
    [UnityEditor.InitializeOnLoad]
    public class RenderPipelineDefine
    {
        private const string _sesionStateKey = "PWB_LastPipeline";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        static RenderPipelineDefine()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
                var pipeline = GetCurrentRenderPipeline();
                var lastPipeLine = UnityEditor.SessionState.GetString(_sesionStateKey, string.Empty);
                if (pipeline == lastPipeLine) return;
                SetRenderPipelineDefineSymbol(pipeline);
            };
        }

        #region PIPELINE DETECTION
        private static string GetCurrentRenderPipeline()
        {
            var currentRenderPipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (currentRenderPipeline != null)
            {
                if (currentRenderPipeline.GetType().ToString().Contains("HighDefinition")) return "HDRP";
                if (currentRenderPipeline.GetType().ToString().Contains("Universal")) return "URP";
            }
            return "BIRP";
        }
        #endregion

        #region DEFINE SYMBOLS
        private static void SetRenderPipelineDefineSymbol(string pipeline)
        {
            string define = $"PWB_{pipeline}";
            var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(target);
#if UNITY_2022_2_OR_NEWER
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var definesSCSV = UnityEditor.PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            var definesSCSV = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
#endif
            var defines = definesSCSV.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var d in defines) if (d.Trim() == define) return;
            definesSCSV = string.IsNullOrEmpty(definesSCSV) ? define : definesSCSV + ";" + define;
#if UNITY_2022_2_OR_NEWER
            UnityEditor.PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, definesSCSV);
#else
            UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, definesSCSV);
#endif
            UnityEditor.SessionState.SetString(_sesionStateKey, pipeline);
        }

        public static void SetRenderPipelineDefineSymbol()
        {
            var pipeline = GetCurrentRenderPipeline();
            SetRenderPipelineDefineSymbol(pipeline);
        }
        #endregion
    }

    public partial class ThumbnailUtils
    {
        #region CONSTANTS & FIELDS
        private static LayerMask layerMask => 1 << PWBCore.staticData.thumbnailLayer;
        private const int MULTIBRUSH_SIZE = 256;
        public const int SIZE = 256;
        private const int MIN_SIZE = 24;

        private static Texture2D _emptyTexture = null;
        private static bool _savingImage = false;

        public static bool savingImage => _savingImage;
        #endregion

        #region THUMBNAIL EDITOR DATA
        private class ThumbnailEditor
        {
            public ThumbnailSettings settings = null;
            public GameObject root = null;
            public Camera camera = null;
            public RenderTexture renderTexture = null;
            public Light light = null;
            public Transform pivot = null;
            public GameObject target = null;
#if PWB_HDRP
            public UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData HDCamData = null;
            public UnityEngine.Rendering.Volume volume = null;
            public BoxCollider volumeCollider = null;
#endif
        }
        #endregion

        #region TEXTURE UTILITIES
        public static void RenderTextureToTexture2D(RenderTexture renderTexture, Texture2D texture)
        {
            var prevActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0);
            texture.Apply();
            RenderTexture.active = prevActive;
        }

        private static Texture2D emptyTexture
        {
            get
            {
                if (_emptyTexture == null) _emptyTexture = Resources.Load<Texture2D>("Sprites/Empty");
                return _emptyTexture;
            }
        }

        public static void SavePngResource(Texture2D texture, string thumbnailPath)
        {
            if (texture == null || string.IsNullOrEmpty(thumbnailPath)) return;
            _savingImage = true;
            var isNewFile = !System.IO.File.Exists(thumbnailPath);
            byte[] buffer = texture.EncodeToPNG();
            UnityEditor.AssetDatabase.StartAssetEditing();
            try
            {
                System.IO.File.WriteAllBytes(thumbnailPath, buffer);
            }
            finally
            {
                UnityEditor.AssetDatabase.StopAssetEditing();
            }
            if (isNewFile) PWBCore.refreshDatabase = true;
            _savingImage = false;
        }

        public static Texture2D ScaleImage(string imagePath)
        {
            if (!System.IO.File.Exists(imagePath)) return null;
            var rawData = System.IO.File.ReadAllBytes(imagePath);
            Texture2D source = new Texture2D(2, 2);
            ImageConversion.LoadImage(source, rawData);
            RenderTexture renderTexture = RenderTexture.GetTemporary(SIZE, SIZE);
            Graphics.Blit(source, renderTexture);
            Texture2D scaledTexture = new Texture2D(SIZE, SIZE);
            RenderTextureToTexture2D(renderTexture, scaledTexture);
            return scaledTexture;
        }

        public static void CopyTexture(Texture2D from, out Texture2D to)
        {
            if (from == null)
            {
                to = null;
                return;
            }
            to = new Texture2D(from.width, from.height);
            to.SetPixels(from.GetPixels());
            to.Apply();
        }
        #endregion

        #region EDITOR LIFECYCLE
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterPlayModeStateChangedCallback()
        {

            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state != UnityEditor.PlayModeStateChange.ExitingEditMode) return;
            if (_thumbnailEditor == null) return;

            if (_thumbnailEditor.camera != null)
            {
                _thumbnailEditor.camera.targetTexture = null;
                Object.DestroyImmediate(_thumbnailEditor.camera.gameObject);
            }

            if (_thumbnailEditor.renderTexture != null)
                Object.DestroyImmediate(_thumbnailEditor.renderTexture);

            if (_thumbnailEditor.light != null)
                Object.DestroyImmediate(_thumbnailEditor.light.gameObject);

            if (_thumbnailEditor.root != null)
                Object.DestroyImmediate(_thumbnailEditor.root);

            if (_bgMaterial != null)
            {
                Object.DestroyImmediate(_bgMaterial);
                _bgMaterial = null;
            }

            if (_defaultCubemap != null)
            {
                Object.DestroyImmediate(_defaultCubemap);
                _defaultCubemap = null;
            }
        }
        #endregion

        #region MULTIBRUSH THUMBNAIL
        public static void UpdateThumbnail(ThumbnailSettings settings,
            Texture2D thumbnailTexture, Texture2D[] subThumbnails, string thumbnailPath, bool savePng)
        {
            if (subThumbnails.Length == 0)
            {
                thumbnailTexture.SetPixels(new Color[SIZE * SIZE]);
                thumbnailTexture.Apply();
                return;
            }

            var sqrt = Mathf.Sqrt(subThumbnails.Length);
            var sideCellsCount = Mathf.FloorToInt(sqrt);
            if (Mathf.CeilToInt(sqrt) != sideCellsCount) ++sideCellsCount;
            var spacing = (SIZE * sideCellsCount) / MIN_SIZE;
            var bigSize = SIZE * sideCellsCount + spacing * (sideCellsCount - 1);
            var texture = new Texture2D(bigSize, bigSize);
            var pixelCount = bigSize * bigSize;
            var pixels = new Color32[pixelCount];
            texture.SetPixels32(pixels);
            int subIdx = 0;
            bool finished = false;
            for (int i = sideCellsCount - 1; i >= 0 && !finished; --i)
            {
                for (int j = 0; j < sideCellsCount && !finished; ++j)
                {
                    var x = j * (SIZE + spacing);
                    var y = i * (SIZE + spacing);
                    if (subThumbnails[subIdx] != null)
                        texture.SetPixels32(x, y, SIZE, SIZE, subThumbnails[subIdx].GetPixels32());
                    ++subIdx;
                    if (subIdx == subThumbnails.Length) finished = true;
                }
            }
            texture.filterMode = FilterMode.Trilinear;
            texture.Apply();
            var renderTexture = new RenderTexture(MULTIBRUSH_SIZE, MULTIBRUSH_SIZE, 24);
            var prevActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Graphics.Blit(texture, renderTexture);
            if (thumbnailTexture.width != MULTIBRUSH_SIZE || thumbnailTexture.height != MULTIBRUSH_SIZE)
#if UNITY_2021_2_OR_NEWER
                thumbnailTexture.Reinitialize(MULTIBRUSH_SIZE, MULTIBRUSH_SIZE);
#else
                thumbnailTexture.Resize(MULTIBRUSH_SIZE, MULTIBRUSH_SIZE);
#endif
            thumbnailTexture.ReadPixels(new Rect(0, 0, MULTIBRUSH_SIZE, MULTIBRUSH_SIZE), 0, 0);
            thumbnailTexture.Apply();
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(texture);
            if (savePng) SavePngResource(thumbnailTexture, thumbnailPath);
        }
        #endregion

        #region UPDATE THUMBNAIL
        public static void UpdateThumbnail(MultibrushItemSettings brushItem, bool savePng, bool updateParent)
        {
            if (brushItem.thumbnailSettings.useCustomImage)
            {
                brushItem.LoadThumbnailFromFile();
                return;
            }
            if (brushItem.prefab == null) return;

            if (PWBCore.staticData.useAssetPreview)
            {
                var preview = UnityEditor.AssetPreview.GetAssetPreview(brushItem.prefab);
                if (preview != null)
                {
                    CopyTexture(preview, out Texture2D thumbnailTexture);
                    var rt = RenderTexture.GetTemporary(SIZE, SIZE);
                    Graphics.Blit(thumbnailTexture, rt);
                    var resizedTexture = new Texture2D(SIZE, SIZE, thumbnailTexture.format, false);
                    RenderTexture.active = rt;
                    resizedTexture.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0);
                    resizedTexture.Apply();
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(rt);
                    brushItem.SetCustomThumbnailTexture(resizedTexture, savePng);
                    if (updateParent)
                        UpdateThumbnail(brushItem.parentSettings, updateItemThumbnails: false, savePng);
                }
                return;
            }
            UpdateThumbnail(brushItem.thumbnailSettings, brushItem.thumbnailTexture,
                brushItem.prefab, brushItem.thumbnailPath, savePng);
            if (updateParent)
                UpdateThumbnail(brushItem.parentSettings, updateItemThumbnails: false, savePng);
        }

        public static void UpdateThumbnail(MultibrushSettings brushSettings, bool updateItemThumbnails, bool savePng)
        {
            if (brushSettings.thumbnailSettings.useCustomImage) return;
            var brushItems = brushSettings.items;
            var subThumbnails = new System.Collections.Generic.List<Texture2D>();
            foreach (var item in brushItems)
            {
                if (updateItemThumbnails) UpdateThumbnail(item, savePng, updateParent: false);
                if (item.includeInThumbnail) subThumbnails.Add(item.thumbnail);
            }
            UpdateThumbnail(brushSettings.thumbnailSettings, brushSettings.thumbnailTexture,
                subThumbnails.ToArray(), brushSettings.thumbnailPath, savePng);
        }

        public static void UpdateThumbnail(BrushSettings brushItem, bool updateItemThumbnails, bool savePng)
        {
            if (brushItem is MultibrushItemSettings)
                UpdateThumbnail(brushItem as MultibrushItemSettings, savePng, updateParent: true);
            else if (brushItem is MultibrushSettings)
                UpdateThumbnail(brushItem as MultibrushSettings, updateItemThumbnails, savePng);
        }
        #endregion

        #region CLEANUP
        public static void DeleteUnusedThumbnails()
        {
            var palettes = PaletteManager.allPalettes;
            bool CheckThumbnailPath(string thumbnailPath)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(thumbnailPath);
                var ids = fileName.Split('_');
                if (ids.Length > 2) return false;
                long itemId = -1;
                long brushId = -1;
                var provider = new System.Globalization.CultureInfo("en-US");
                if (!long.TryParse(ids[0], System.Globalization.NumberStyles.HexNumber, provider, out brushId)) return false;
                var brush = PaletteManager.GetBrushById(brushId);
                if (brush == null) return false;
                if (ids.Length == 1) return true;
                if (!long.TryParse(ids[1], System.Globalization.NumberStyles.HexNumber, provider, out itemId)) return false;
                return brush.ItemExist(itemId);
            }

            var folderPaths = PaletteManager.GetPaletteThumbnailFolderPaths();
            foreach (var folderPath in folderPaths)
            {
                var thumbnailPaths = System.IO.Directory.GetFiles(folderPath, "*.png");
                foreach (var thumbnailPath in thumbnailPaths)
                {
                    if (!CheckThumbnailPath(thumbnailPath))
                    {
                        System.IO.File.Delete(thumbnailPath);
                        var metapath = thumbnailPath + ".meta";
                        if (System.IO.File.Exists(metapath)) System.IO.File.Delete(metapath);
                        PWBCore.refreshDatabase = true;
                    }
                }
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
