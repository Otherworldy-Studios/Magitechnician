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
    public static class ApplicationEventHandler
    {
        private static bool _importingPackage = false;
        public static bool importingPackage => _importingPackage;
        private static bool _refreshOnImportingCancelled = false;
        public static bool RefreshOnImportingCancelled() => _refreshOnImportingCancelled = true;

        private static bool _sceneOpening = false;
        public static bool sceneOpening => _sceneOpening;

        private static bool _hierarchyChangedWhileUsingTools = false;
        public static bool hierarchyChangedWhileUsingTools
        { get => _hierarchyChangedWhileUsingTools; set => _hierarchyChangedWhileUsingTools = value; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        static ApplicationEventHandler()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnStateChanged;
            UnityEditor.EditorApplication.hierarchyChanged += OnHierarchyChanged;
            UnityEditor.EditorApplication.update += OnEditorUpdate;
            UnityEditor.AssetDatabase.importPackageStarted += OnImportPackageStarted;
            UnityEditor.AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            UnityEditor.AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
            UnityEditor.AssetDatabase.importPackageFailed += OnImportPackageFailed;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += OnSceneOpening;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneView.duringSceneGui += OnFirstSceneGuiForOverlayInit;
            UnityEditor.EditorApplication.quitting += OnEditorQuitting;
#endif
            UnityEditor.EditorApplication.delayCall += () =>
            {
#if !PWB_DO_NOT_INITIALIZE_ON_LOAD
                PWBCore.Initialize();
#endif
#if !PWB_KEEP_OBSOLETE
                PWBCore.staticData.DeleteObsoleteFiles();
#endif
            };
        }

#if UNITY_2021_2_OR_NEWER
        private static void OnFirstSceneGuiForOverlayInit(UnityEditor.SceneView sceneView)
        {
            UnityEditor.SceneView.duringSceneGui -= OnFirstSceneGuiForOverlayInit;
            PWBIO.EnsureOverlayStateInitialized();
        }

        private static void OnEditorQuitting()
        {
            PWBIO.SaveOverlayStates();
        }
#endif

        private static void OnEditorUpdate()
        {
            if (PWBCore.refreshDatabase) PWBCore.AssetDatabaseRefresh();
        }

        private static void OnSceneOpening(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
            => _sceneOpening = true;

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene,
            UnityEditor.SceneManagement.OpenSceneMode mode)
            => _sceneOpening = false;

        private static void OnHierarchyChanged()
        {
            if (PWBCore.updatingTempColliders || PWBIO.painting)
            {
                if (PWBCore.updatingTempColliders) PWBCore.updatingTempColliders = false;
                if (PWBIO.painting) PWBIO.painting = false;
                return;
            }
            if (ToolController.current != ToolController.Tool.NONE)
                hierarchyChangedWhileUsingTools = true;
            else
            {
                PWBIO.ClearPreviewDictionaries();
            }
        }

        private static void OnStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode
                || state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                PWBCore.staticData.SaveIfPending();
        }

        private static void OnImportPackageStarted(string packageName) => _importingPackage = true;
        private static void OnImportPackageCompleted(string packageName) => _importingPackage = false;
        private static void OnImportPackageCancelled(string packageName)
        {
            if (_refreshOnImportingCancelled)
            {
                UnityEditor.AssetDatabase.Refresh();
                _refreshOnImportingCancelled = false;
            }
            _importingPackage = false;
        }
        private static void OnImportPackageFailed(string packageName, string errorMessage) => _importingPackage = false;
    }

    public class DataReimportHandler : UnityEditor.AssetPostprocessor
    {
        private static bool _importingAssets = false;
        public static bool importingAssets => _importingAssets;
        void OnPreprocessAsset() => _importingAssets = true;
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            _importingAssets = false;
            if (PWBSettings.movingDir) return;
            if (PWBCore.staticData.saving) return;
            if (!PWBData.palettesDirectory.Contains(Application.dataPath)) return;
            if (PaletteManager.addingPalettes)
            {
                PaletteManager.addingPalettes = false;
                return;
            }
            var paths = new System.Collections.Generic.List<string>(importedAssets);
            paths.AddRange(deletedAssets);
            paths.AddRange(movedAssets);
            paths.AddRange(movedFromAssetPaths);

            var relativeDataPath = PWBSettings.relativeDataDir.Replace(Application.dataPath, string.Empty);
            if (paths.Exists(p => p.Contains(relativeDataPath) && System.IO.Path.GetExtension(p) == ".txt"))
            {
                if (PaletteManager.selectedPalette != null && PaletteManager.selectedPalette.saving)
                {
                    PaletteManager.selectedPalette.StopSaving();
                    return;
                }
            }
        }
    }
}
#pragma warning restore UDR0001
