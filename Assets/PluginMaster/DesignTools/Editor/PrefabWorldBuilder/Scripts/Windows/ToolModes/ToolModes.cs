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
    public partial class ToolModes : UnityEditor.EditorWindow
    {
        
        private static ToolModes _instance = null;
        private Vector2 _mainScrollPosition = Vector2.zero;

        [UnityEditor.MenuItem("Tools/Plugin Master/Prefab World Builder/Tool Modes...", false, 1138)]
        public static void ShowWindow() => _instance = GetWindow<ToolModes>("Tool Modes");

        public static void RepainWindow()
        {
            if (_instance != null) _instance.Repaint();
        }

        public static void CloseWindow()
        {
            if (_instance != null) _instance.Close();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        private void OnEnable()
        {
            UnityEditor.Undo.undoRedoPerformed -= Repaint;
            UnityEditor.Undo.undoRedoPerformed += Repaint;

            ModularToolModes.OnBlockToolModeChanged -= Repaint;
            ModularToolModes.OnBlockToolModeChanged += Repaint;
            PWBCore.Initialize();
        }

        private void OnDisable()
        {
            UnityEditor.Undo.undoRedoPerformed -= Repaint;
            ModularToolModes.OnBlockToolModeChanged -= Repaint;
        }

        private void OnGUI()
        {
            if (_instance == null) _instance = this;
            using (var scrollView = new UnityEditor.EditorGUILayout.ScrollViewScope(
                _mainScrollPosition, false, false,
                GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUIStyle.none))
            {
                _mainScrollPosition = scrollView.scrollPosition;
                if (ToolController.current == ToolController.Tool.BLOCK)
                    BlockModesGUI();
#if PWB_FLOOR_TOOL_MODES
                else if (ToolController.current == ToolController.Tool.FLOOR)
                    FloorModesGUI();
#endif
            }
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                GUI.FocusControl(null);
                Repaint();
            }
        }
    }
}
#pragma warning restore UDR0001
