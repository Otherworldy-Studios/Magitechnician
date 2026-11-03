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
    public partial class PWBPreferences : UnityEditor.EditorWindow
    {
        private int _tab = 0;
        private Vector2 _mainScrollPosition = Vector2.zero;

        
        private static PWBPreferences _instance = null;
        [UnityEditor.MenuItem("Tools/Plugin Master/Prefab World Builder/Preferences...", false, 1250)]
        public static void ShowWindow() => _instance = GetWindow<PWBPreferences>("PWB Preferences");
        private void OnEnable()
        {
            _instance = this;
        }
        private void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                _tab = GUILayout.Toolbar(_tab, new string[] { "General", "Shortcuts" });
                GUILayout.FlexibleSpace();
            }
            using (var scrollView = new UnityEditor.EditorGUILayout.ScrollViewScope(_mainScrollPosition,
                false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, UnityEditor.EditorStyles.helpBox))
            {
                _mainScrollPosition = scrollView.scrollPosition;
                if (_tab == 0) GeneralSettings();
                else Shortcuts();
            }
            UpdateCombination();
        }

    }
}
#pragma warning restore UDR0001
