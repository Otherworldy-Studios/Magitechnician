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
    public partial class ToolProperties : UnityEditor.EditorWindow
    {
        public class ProfileData
        {
            public readonly IToolController ToolController = null;
            public readonly string profileName = string.Empty;
            public ProfileData(IToolController ToolController, string profileName)
                => (this.ToolController, this.profileName) = (ToolController, profileName);
        }
        private void ToolProfileGUI(IToolController ToolController)
        {
            using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.helpBox))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Tool Profile");
                if (GUILayout.Button(ToolController.selectedProfileName,
                    UnityEditor.EditorStyles.popup, GUILayout.MinWidth(100)))
                {
                    GUI.FocusControl(null);
                    var menu = new UnityEditor.GenericMenu();
                    foreach (var profileName in ToolController.profileNames)
                        menu.AddItem(new GUIContent(profileName), profileName == ToolController.selectedProfileName,
                            SelectProfileItem, new ProfileData(ToolController, profileName));
                    menu.AddSeparator(string.Empty);
                    if (ToolController.selectedProfileName != ToolProfile.DEFAULT) menu.AddItem(new GUIContent("Save"),
                        false, SaveProfile, ToolController);
                    menu.AddItem(new GUIContent("Save As..."), false, SaveProfileAs,
                        new ProfileData(ToolController, ToolController.selectedProfileName));
                    if (ToolController.selectedProfileName != ToolProfile.DEFAULT)
                        menu.AddItem(new GUIContent("Delete Selected Profile"), false, DeleteProfile,
                            new ProfileData(ToolController, ToolController.selectedProfileName));
                    menu.AddItem(new GUIContent("Revert Selected Profile"), false, RevertProfile, ToolController);
                    menu.AddItem(new GUIContent("Factory Reset Selected Profile"), false,
                        FactoryResetProfile, ToolController);
                    menu.ShowAsContext();
                }
            }
        }

        private void SelectProfile(ProfileData profileData)
        {

            GUI.FocusControl(null);
            profileData.ToolController.selectedProfileName = profileData.profileName;
            Repaint();
            if (ToolController.current == ToolController.Tool.MIRROR)
                UnityEditor.SceneView.lastActiveSceneView.LookAt(MirrorManager.settings.mirrorPosition);
            else if (ToolController.current == ToolController.Tool.LINE)
                LineManager.settings.OnDataChanged();
            UnityEditor.SceneView.RepaintAll();
        }

        private void SelectProfileItem(object value) => SelectProfile(value as ProfileData);

        private void SaveProfile(object value)
        {

            var manager = value as IToolController;
            manager.SaveProfile();
        }

        private void SaveProfileAs(object value)
        {
            var profiledata = value as ProfileData;
            SaveProfileWindow.ShowWindow(profiledata, OnSaveProfileDone);
        }

        private void OnSaveProfileDone(IToolController ToolController, string profileName)
        {

            ToolController.SaveProfileAs(profileName);
            Repaint();
        }
        private class SaveProfileWindow : UnityEditor.EditorWindow
        {
            private IToolController _ToolController = null;
            private string _profileName = string.Empty;
            private System.Action<IToolController, string> OnDone;

            public static void ShowWindow(ProfileData data, System.Action<IToolController, string> OnDone)
            {
                var window = GetWindow<SaveProfileWindow>(true, "Save Profile");
                window._ToolController = data.ToolController;
                window._profileName = data.profileName;
                window.OnDone = OnDone;
                window.minSize = window.maxSize = new Vector2(160, 50);
                UnityEditor.EditorGUIUtility.labelWidth = 70;
                UnityEditor.EditorGUIUtility.fieldWidth = 70;
            }

            private void OnGUI()
            {
                const string textFieldName = "NewProfileName";
                GUI.SetNextControlName(textFieldName);
                _profileName = UnityEditor.EditorGUILayout.TextField(_profileName).Trim();
                GUI.FocusControl(textFieldName);
                using (new UnityEditor.EditorGUI.DisabledGroupScope(_profileName == string.Empty))
                {
                    if (GUILayout.Button("Save"))
                    {
                        OnDone(_ToolController, _profileName);
                        Close();
                    }
                }
            }
        }

        private void DeleteProfile(object value)
        {

            var profiledata = value as ProfileData;
            profiledata.ToolController.DeleteProfile();
            if (ToolController.current == ToolController.Tool.MIRROR)
                UnityEditor.SceneView.lastActiveSceneView.LookAt(MirrorManager.settings.mirrorPosition);
        }
        private void RevertProfile(object value)
        {

            var manager = value as IToolController;
            manager.Revert();
            if (ToolController.current == ToolController.Tool.MIRROR)
                UnityEditor.SceneView.lastActiveSceneView.LookAt(MirrorManager.settings.mirrorPosition);
        }
        private void FactoryResetProfile(object value)
        {

            var manager = value as IToolController;
            manager.FactoryReset();
            if (ToolController.current == ToolController.Tool.MIRROR)
                UnityEditor.SceneView.lastActiveSceneView.LookAt(MirrorManager.settings.mirrorPosition);
        }
    }
}
#pragma warning restore UDR0001
