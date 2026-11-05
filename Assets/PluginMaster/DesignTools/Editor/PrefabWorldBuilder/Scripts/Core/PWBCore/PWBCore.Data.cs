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
    public static partial class PWBCore
    {
        
        private static PWBData _staticData = null;
        public static bool staticDataWasInitialized => _staticData != null;
        public static PWBData staticData
        {
            get
            {
                if (_staticData != null) return _staticData;
                _staticData = new PWBData();
                return _staticData;
            }
        }

        public static PWBData GetLoadedStaticData()
        {
            if (_staticData != null) return _staticData;
            LoadFromFile();
            return staticData;
        }

        public static void Initialize()
        {
            if (!_loadedFromFile) LoadFromFile();
        }

        public static bool _loadedFromFile = false;

        public static long _loadTimeSpan = 0;

        public static System.Action OnLoadedFromFile = null;

        public static void LoadFromFile()
        {
            var now = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            if (now - _loadTimeSpan < 1000) return;
            _loadTimeSpan = now;
            _loadedFromFile = true;
            var text = PWBData.ReadDataText();
            void CreateFile()
            {
                _staticData = new PWBData();
                _staticData.SaveAndUpdateVersion();
            }
            if (text == null) CreateFile();
            else
            {
                _staticData = null;
                try
                {
                    _staticData = JsonUtility.FromJson<PWBData>(text);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
                if (_staticData == null)
                {
                    CreateFile();
                    return;
                }
                foreach (var palette in PaletteManager.allPalettes)
                    foreach (var brush in palette.brushes)
                        foreach (var item in brush.items) item.InitializeParentSettings(brush);
            }
            if (OnLoadedFromFile != null) OnLoadedFromFile();
        }

        public static void SetSavePending()
        {
            AutoSave.QuickSave();
            staticData.SetSavePending();
        }

        public static string GetRelativePath(string fullPath)
        {
            var fullUri = new System.Uri(fullPath);
            var dataUri = new System.Uri(Application.dataPath);
            return System.Uri.UnescapeDataString(dataUri.MakeRelativeUri(fullUri).ToString());
        }
        public static string GetFullPath(string retalivePath)
             => Application.dataPath.Substring(0, Application.dataPath.Length - 6) + retalivePath;

        private static readonly char[] kInvalidPathCharsArray = System.IO.Path.GetInvalidPathChars();
        public static bool IsFullPath(string path)
            => !string.IsNullOrWhiteSpace(path)
            && path.IndexOfAny(kInvalidPathCharsArray) == -1
            && System.IO.Path.IsPathRooted(path)
            && !System.IO.Path.GetPathRoot(path).Equals(System.IO.Path.DirectorySeparatorChar.ToString(),
                System.StringComparison.Ordinal);
        #region BLOCK MANAGER DATA
        [System.Serializable]
        private class BlockManagerData
        {
            [SerializeField] private BlockManager _blockManager = BlockManager.instance as BlockManager;
        }

        public static void LoadBlockManagerFromFile()
        {
            var text = PWBData.ReadDataText();
            if (text == null) return;
            try
            {
                JsonUtility.FromJson<BlockManagerData>(text);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
