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
    [System.Serializable]
    public class GridManager
    {
        
        private static GridSettings _staticSettings = new GridSettings();
        [SerializeField] GridSettings _settings = _staticSettings;
        public static GridSettings settings => _staticSettings;

        public static void FrameGridOrigin()
        {
            var sceneView = (UnityEditor.SceneView)(UnityEditor.SceneView.sceneViews[0]);
            if (sceneView == null) return;
            var viewportPoint = sceneView.camera.WorldToViewportPoint(settings.origin);
            bool originOnScreen = viewportPoint.x > 0 && viewportPoint.y > 0
                && viewportPoint.x < 1 && viewportPoint.y < 1;
            if (originOnScreen) return;
            var activeGO = UnityEditor.Selection.activeGameObject;
            var tempGO = new GameObject();
            tempGO.transform.position = settings.origin;
            UnityEditor.Selection.activeObject = tempGO;
            UnityEditor.SceneView.FrameLastActiveSceneView();
            UnityEditor.Selection.activeGameObject = activeGO;
            GameObject.DestroyImmediate(tempGO);
        }

        public static void ToggleGridPositionHandle()
        {
            if (!settings.lockedGrid) settings.lockedGrid = true;
            settings.showPositionHandle = !settings.showPositionHandle;
            SnapSettingsWindow.RepaintWindow();
        }

        public static void ToggleGridRotationHandle()
        {
            if (!settings.lockedGrid) settings.lockedGrid = true;
            settings.showRotationHandle = !settings.showRotationHandle;
            SnapSettingsWindow.RepaintWindow();
        }

        public static void ToggleGridScaleHandle()
        {
            if (!settings.lockedGrid) settings.lockedGrid = true;
            settings.showScaleHandle = !settings.showScaleHandle;
            SnapSettingsWindow.RepaintWindow();
        }
    }
}
#pragma warning restore UDR0001
