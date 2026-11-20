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
    public class SelectionToggle : ToolToggleBase<SelectionToggle>
    {
        public const string ID = "PWB/SelectionToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.SELECTION;
        public SelectionToggle() : base()
        {
            _iconName = "Selection";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Selection",
                PWBSettings.shortcuts.toolbarSelectionToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class CircleSelectToggle : ToolToggleBase<CircleSelectToggle>
    {
        public const string ID = "PWB/CircleSelectToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.CIRCLE_SELECT;
        public CircleSelectToggle() : base()
        {
            _iconName = "CircleSelect";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Circle Select",
                PWBSettings.shortcuts.toolbarCircleSelectToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class ExtrudeToggle : ToolToggleBase<ExtrudeToggle>
    {
        public const string ID = "PWB/ExtrudeToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.EXTRUDE;
        public ExtrudeToggle() : base()
        {
            _iconName = "Extrude";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Extrude", PWBSettings.shortcuts.toolbarExtrudeToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class MirrorToggle : ToolToggleBase<MirrorToggle>
    {
        public const string ID = "PWB/MirrorToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.MIRROR;
        public MirrorToggle() : base()
        {
            _iconName = "Mirror";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Mirror", PWBSettings.shortcuts.toolbarMirrorToggle.combination.ToString());
        }
    }

    [UnityEditor.Overlays.Overlay(typeof(UnityEditor.SceneView), "PWB/Selection", true)]
    public class PWBSelectionToolbarOverlay : UnityEditor.Overlays.ToolbarOverlay
    {
        private static PWBSelectionToolbarOverlay _instance = null;
        PWBSelectionToolbarOverlay() : base(SelectionToggle.ID, CircleSelectToggle.ID, ExtrudeToggle.ID, MirrorToggle.ID)
        {
            _instance = this;
            this.displayedChanged += OndisplayedChanged;
#if UNITY_2022_2_OR_NEWER
            collapsedIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + "Selection");
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
