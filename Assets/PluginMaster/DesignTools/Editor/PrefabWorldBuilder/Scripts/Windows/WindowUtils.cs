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
    public class RenameWindow : UnityEditor.EditorWindow
    {
        private string _name = string.Empty;
        private bool _focusSet = false;
        private System.Action<string> Save = null;
        public static void ShowWindow(Vector2 position, System.Action<string> saveAction, string title, string name)
        {
            var window = GetWindow<RenameWindow>(true, title);
            window.position = new Rect(position.x, position.y + 50, 0, 0);
            window.minSize = window.maxSize = new Vector2(160, 45);
            window._focusSet = false;
            window.Save += saveAction;
            window._name = name;
        }

        private void OnGUI()
        {
            GUI.SetNextControlName("NameField");
            _name = GUILayout.TextField(_name);
            if (!_focusSet)
            {
                UnityEditor.EditorGUI.FocusTextInControl("NameField");
                _focusSet = true;
            }
            using (new UnityEditor.EditorGUI.DisabledGroupScope(string.IsNullOrWhiteSpace(_name)))
            {
                if (GUILayout.Button("Save"))
                {
                    if (Save != null) Save(_name);
                    Close();
                }
            }
        }
    }
}
#pragma warning restore UDR0001
