/*
Copyright (c) Omar Duarte
Unauthorized copying of this path, via any medium is strictly prohibited.
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

namespace PluginMaster
{
    public partial class PWBData
    {
        public void DeleteObsoleteFiles()
        {
            var rootDirFullPath = PWBCore.GetFullPath(rootDirectory);

            var obsoleteDirNew = rootDirFullPath + "/Scripts/Obsolete";
            if (System.IO.Directory.Exists(obsoleteDirNew))
            {
                System.IO.Directory.Delete(obsoleteDirNew, true);
                var metaFilePath = obsoleteDirNew + ".meta";
                if (System.IO.File.Exists(metaFilePath))
                    System.IO.File.Delete(metaFilePath);
                PWBCore.refreshDatabase = true;
            }

            const string obsoleteFilesDeletedSessionStateKey = "PWBObsoleteFilesDeleted" + PWBData.VERSION;
            if (UnityEditor.SessionState.GetBool(obsoleteFilesDeletedSessionStateKey, false)) return;
            else UnityEditor.SessionState.SetBool(obsoleteFilesDeletedSessionStateKey, true);

            var obsoleteRootDir = rootDirFullPath + "/Scripts";
            if (!System.IO.Directory.Exists(obsoleteRootDir)) return;

            var filePaths = new string[]
            {
               "Shortcuts.cs",
               "SnapManager.cs",
               "SnapSettingsWindow.cs",
               "ToolBase.cs",
               "ToolManager.cs",
               "Tools/Modular/Block/Core/PWBIO.BlockSymmetryOriginHandling.cs",
               "Tools/Modular/Block/ToolModesOverlay/ToolModesOverlay.BlockMirrorModes.cs"
            };
            var filesWereDeleted = false;
            foreach (var path in filePaths)
            {
                var filePath = obsoleteRootDir + "/" + path;
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    var metaFilePath = filePath + ".meta";
                    if (System.IO.File.Exists(metaFilePath))
                        System.IO.File.Delete(metaFilePath);
                    filesWereDeleted = true;
                }
            }
            if (filesWereDeleted)
                PWBCore.refreshDatabase = true;
        }
    }
}
#pragma warning restore UDR0001
