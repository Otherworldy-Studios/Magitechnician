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
#if UNITY_2021_2_OR_NEWER
using UnityEngine;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace PluginMaster
{
    public class PWBWindowToggle : PWBToolbarToggle
    {
        protected void StartPolling(System.Func<bool> isWindowOpen)
        {
            schedule.Execute(() => SetValueWithoutNotify(isWindowOpen())).Every(200);
        }
    }

    public class PWBToolbarButton : UnityEditor.Toolbars.EditorToolbarButton
    {
        protected string _iconName = string.Empty;
        protected async void DoLoadIcon()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class PropertiesButton : PWBWindowToggle
    {
        public const string ID = "PWB/PropertiesButton";
        public PropertiesButton()
        {
            _iconName = "ToolProperties";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Tool Properties";
            this.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    ToolProperties.ShowWindow();
                else if (UnityEditor.EditorWindow.HasOpenInstances<ToolProperties>())
                    UnityEditor.EditorWindow.GetWindow<ToolProperties>().Close();
            });

            StartPolling(() => UnityEditor.EditorWindow.HasOpenInstances<ToolProperties>());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class BrushPropertiesButton : PWBWindowToggle
    {
        public const string ID = "PWB/BrushPropertiesButton";
        public BrushPropertiesButton()
        {
            _iconName = "BrushProperties";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Brush Properties";
            this.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    BrushProperties.ShowWindow();
                else if (UnityEditor.EditorWindow.HasOpenInstances<BrushProperties>())
                    UnityEditor.EditorWindow.GetWindow<BrushProperties>().Close();
            });

            StartPolling(() => UnityEditor.EditorWindow.HasOpenInstances<BrushProperties>());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class ToolModesButton : PWBWindowToggle
    {
        public const string ID = "PWB/ToolModesButton";
        public ToolModesButton()
        {
            _iconName = "ToolModes";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Tool Modes";
            this.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    ToolModes.ShowWindow();
                else if (UnityEditor.EditorWindow.HasOpenInstances<ToolModes>())
                    UnityEditor.EditorWindow.GetWindow<ToolModes>().Close();
            });

            StartPolling(() => UnityEditor.EditorWindow.HasOpenInstances<ToolModes>());
        }

    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class HelpButton : PWBToolbarButton
    {
        public const string ID = "PWB/HelpButton";
        public HelpButton()
        {
            _iconName = "Help";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Documentation";
            clicked += PWBCore.OpenDocFile;
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class GridSettingsButton : PWBWindowToggle
    {
        public const string ID = "PWB/GridSettingsButton";
        public GridSettingsButton()
        {
            _iconName = "SnapSettings";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Grid & Snapping Settings";
            this.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SnapSettingsWindow.ShowWindow();
                else if (UnityEditor.EditorWindow.HasOpenInstances<SnapSettingsWindow>())
                    UnityEditor.EditorWindow.GetWindow<SnapSettingsWindow>().Close();
            });

            StartPolling(() => UnityEditor.EditorWindow.HasOpenInstances<SnapSettingsWindow>());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class PreferencesButton : PWBWindowToggle
    {
        public const string ID = "PWB/PreferencesButton";
        public PreferencesButton()
        {
            _iconName = "Preferences";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            tooltip = "PWB Preferences";

            this.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    PWBPreferences.ShowWindow();
                else if (UnityEditor.EditorWindow.HasOpenInstances<PWBPreferences>())
                        UnityEditor.EditorWindow.GetWindow<PWBPreferences>().Close();
            });

            StartPolling(() => UnityEditor.EditorWindow.HasOpenInstances<PWBPreferences>());
        }
    }

    [UnityEditor.Overlays.Overlay(typeof(UnityEditor.SceneView), "PWB/Settings", true)]
    public class SettingsAndDocsToolbarOverlay : UnityEditor.Overlays.ToolbarOverlay
    {
        private static SettingsAndDocsToolbarOverlay _instance = null;
        SettingsAndDocsToolbarOverlay()
            : base(PropertiesButton.ID, BrushPropertiesButton.ID, ToolModesButton.ID,
                   GridSettingsButton.ID, PreferencesButton.ID, HelpButton.ID)
        {
            _instance = this;
            displayedChanged += OndisplayedChanged;
#if UNITY_2022_2_OR_NEWER
            collapsedIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + "Preferences");
#endif
        }

        private void OndisplayedChanged(bool value)
        {
            ToolbarOverlayManager.OnToolbarDisplayedChanged();
        }
        public static bool hasInstance => _instance != null;
        public static bool isDisplayed => _instance != null && _instance.displayed;
        public static void SetDisplayed(bool value) { if (_instance != null) _instance.displayed = value; }
    }
}
#endif
#pragma warning restore UDR0001
