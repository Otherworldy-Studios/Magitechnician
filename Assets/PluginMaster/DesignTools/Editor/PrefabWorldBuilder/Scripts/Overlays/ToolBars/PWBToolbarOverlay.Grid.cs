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
    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class GridTypeToggle : UnityEditor.Toolbars.EditorToolbarButton
    {
        public const string ID = "PWB/GridTypeToggle";
        private Texture2D _radialGridIcon = null;
        private Texture2D _rectGridIcon = null;
        private string _radialIconName = string.Empty;
        private string _rectIconName = string.Empty;
        public GridTypeToggle() : base()
        {
            UpdateIcon();
            clicked += OnClick;
            GridManager.settings.OnDataChanged += UpdateIcon;
        }

        public void UpdateIcon()
        {

            if (_radialGridIcon == null)
            {
                _radialIconName = "RadialGrid";
                _radialGridIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + _radialIconName);
            }
            if (_rectGridIcon == null)
            {
                _rectIconName = "Grid";
                _rectGridIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + _rectIconName);
            }
            if (_radialGridIcon == null || _rectIconName == null) DoLoadIcons();
            icon = GridManager.settings.radialGridEnabled ? _rectGridIcon : _radialGridIcon;
            tooltip = GridManager.settings.radialGridEnabled ? "Grid" : "Radial Grid";

        }

        private void OnClick()
        {
            GridManager.settings.radialGridEnabled = !GridManager.settings.radialGridEnabled;
            UpdateIcon();
            SnapSettingsWindow.RepaintWindow();
        }

        protected async void DoLoadIcons()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            _radialGridIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + _radialIconName);
            _rectGridIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + _rectIconName);
            if (_radialGridIcon == null || _rectIconName == null) DoLoadIcons();
        }
    }

    public abstract class GridBarToggle : EditorToolbarDropdownToggle
    {
        protected string _iconName = string.Empty;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public GridBarToggle()
        {
            GridManager.settings.OnDataChanged -= UpdateValue;
            GridManager.settings.OnDataChanged += UpdateValue;
            UnityEditor.SceneView.duringSceneGui -= UpdateValue;
            UnityEditor.SceneView.duringSceneGui += UpdateValue;
        }
        protected abstract void UpdateValue();
        private void UpdateValue(UnityEditor.SceneView sceneView) => UpdateValue();

        protected async void DoLoadIcon()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
        }
    }



    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class SnapToggle : GridBarToggle, UnityEditor.Toolbars.IAccessContainerWindow
    {
        public const string ID = "PWB/SnapToggle";
        public UnityEditor.EditorWindow containerWindow { get; set; }

        public SnapToggle() : base()
        {
            _iconName = "SnapOn";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Enable snapping";
            dropdownClicked += ShowSnapWindow;
            this.RegisterValueChangedCallback(OnValueChange);
        }
        protected override void UpdateValue() => value = GridManager.settings.snappingEnabled;
        private void OnValueChange(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            GridManager.settings.snappingEnabled = evt.newValue;
            SnapSettingsWindow.RepaintWindow();
        }

        private void ShowSnapWindow()
        {
            var settings = GridManager.settings;
            var menu = new UnityEditor.GenericMenu();
            if (settings.radialGridEnabled)
            {
                menu.AddItem(new GUIContent("Snap To Radius"), settings.snapToRadius,
                    () => settings.snapToRadius = !settings.snapToRadius);
                menu.AddItem(new GUIContent("Snap To Circunference"), settings.snapToCircunference,
                    () => settings.snapToCircunference = !settings.snapToCircunference);
            }
            else
            {
                menu.AddItem(new GUIContent("X"), settings.snappingOnX, () => settings.snappingOnX = !settings.snappingOnX);
                menu.AddItem(new GUIContent("Y"), settings.snappingOnY, () => settings.snappingOnY = !settings.snappingOnY);
                menu.AddItem(new GUIContent("Z"), settings.snappingOnZ, () => settings.snappingOnZ = !settings.snappingOnZ);
            }
            menu.ShowAsContext();
            SnapSettingsWindow.RepaintWindow();
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class GridToggle : GridBarToggle, UnityEditor.Toolbars.IAccessContainerWindow
    {
        public const string ID = "PWB/GridToggle";
        public UnityEditor.EditorWindow containerWindow { get; set; }

        private void UpdateIcon()
        {
            var settings = GridManager.settings;
            _iconName = "ShowGrid" + (settings.gridOnY ? "Y" : (settings.gridOnX ? "X" : "Z"));
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
        }
        public GridToggle() : base()
        {
            UpdateIcon();
            tooltip = "Show grid";
            dropdownClicked += ShowGridWindow;
            this.RegisterValueChangedCallback(OnValueChange);
            var settings = GridManager.settings;
            settings.OnGridOrientationChange += UpdateIcon;
        }

        protected override void UpdateValue() => value = GridManager.settings.visibleGrid;

        private void OnValueChange(UnityEngine.UIElements.ChangeEvent<bool> evt)
            => GridManager.settings.visibleGrid = evt.newValue;

        private void ShowGridWindow()
        {
            var settings = GridManager.settings;
            var menu = new UnityEditor.GenericMenu();
            menu.AddItem(new GUIContent("X"), settings.gridOnX,
                () =>
                {
                    if (settings.gridOnX) return;
                    settings.gridOnX = true;
                    PWBIO.SetAxis(AxesUtils.Axis.X);
                    UpdateIcon();
                });
            menu.AddItem(new GUIContent("Y"), settings.gridOnY,
                () =>
                {
                    if (settings.gridOnY) return;
                    settings.gridOnY = true;
                    PWBIO.SetAxis(AxesUtils.Axis.Y);
                    UpdateIcon();
                });
            menu.AddItem(new GUIContent("Z"), settings.gridOnZ,
                () =>
                {
                    if (settings.gridOnZ) return;
                    settings.gridOnZ = true;
                    PWBIO.SetAxis(AxesUtils.Axis.Z);
                    UpdateIcon();
                });
            menu.ShowAsContext();
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class LockGridToggle : PWBToolbarToggle
    {
        public const string ID = "PWB/LockGridToggle";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public LockGridToggle()
        {
            UpdteIcon();
            this.RegisterValueChangedCallback(OnValueChange);
            GridManager.settings.OnDataChanged -= UpdateValue;
            GridManager.settings.OnDataChanged += UpdateValue;
            UnityEditor.SceneView.duringSceneGui -= UpdateValue;
            UnityEditor.SceneView.duringSceneGui += UpdateValue;
        }
        protected void UpdateValue() => value = GridManager.settings.lockedGrid;
        private void UpdateValue(UnityEditor.SceneView sceneView) => UpdateValue();

        private void UpdteIcon()
        {
            _iconName = (GridManager.settings.lockedGrid ? "LockGrid" : "UnlockGrid");
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = GridManager.settings.lockedGrid ? "Lock the grid origin in place" : "Unlock the grid origin";
        }

        private void OnValueChange(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            GridManager.settings.lockedGrid = evt.newValue;
            UpdteIcon();
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class BoundsSnappingToggle : PWBToolbarToggle
    {
        public const string ID = "PWB/BoundsSnappingToggle";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public BoundsSnappingToggle()
        {
            UpdteIcon();
            this.RegisterValueChangedCallback(OnValueChange);
            GridManager.settings.OnDataChanged += UpdateValue;
            UnityEditor.SceneView.duringSceneGui -= UpdateValue;
            UnityEditor.SceneView.duringSceneGui += UpdateValue;
        }
        protected void UpdateValue() => value = GridManager.settings.boundsSnapping;
        private void UpdateValue(UnityEditor.SceneView sceneView) => UpdateValue();

        private void UpdteIcon()
        {
            _iconName = "BoundsSnapping";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Bounds Snapping";
        }

        private void OnValueChange(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            GridManager.settings.boundsSnapping = evt.newValue;
        }
    }

    [UnityEditor.Overlays.Overlay(typeof(UnityEditor.SceneView), "PWB/Grid", true)]
    public class PWBGridToolbarOverlay : UnityEditor.Overlays.ToolbarOverlay
    {
        private static PWBGridToolbarOverlay _instance = null;
        PWBGridToolbarOverlay() : base(GridTypeToggle.ID, SnapToggle.ID,
            GridToggle.ID, LockGridToggle.ID, BoundsSnappingToggle.ID)
        {
            _instance = this;
            this.displayedChanged += OndisplayedChanged;
#if UNITY_2022_2_OR_NEWER
            collapsedIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + "Grid");
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
