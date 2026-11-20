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
    public class PinToggle : ToolToggleBase<PinToggle>
    {
        public const string ID = "PWB/PinToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.PIN;
        public PinToggle() : base()
        {
            _iconName = "Pin";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Pin", PWBSettings.shortcuts.toolbarPinToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class BrushToggle : ToolToggleBase<BrushToggle>
    {
        public const string ID = "PWB/BrushToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.BRUSH;
        public BrushToggle() : base()
        {
            _iconName = "Brush";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Brush", PWBSettings.shortcuts.toolbarBrushToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class GravityToggle : ToolToggleBase<GravityToggle>
    {
        public const string ID = "PWB/GravityToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.GRAVITY;
        public GravityToggle() : base()
        {
            _iconName = "GravityTool";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Gravity Brush",
                PWBSettings.shortcuts.toolbarGravityToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class LineToggle : ToolToggleBase<LineToggle>
    {
        public const string ID = "PWB/LineToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.LINE;
        public LineToggle() : base()
        {
            _iconName = "Line";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Line", PWBSettings.shortcuts.toolbarLineToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class ShapeToggle : ToolToggleBase<ShapeToggle>
    {
        public const string ID = "PWB/ShapeToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.SHAPE;
        public ShapeToggle() : base()
        {
            _iconName = "Shape";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Shape", PWBSettings.shortcuts.toolbarShapeToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class TilingToggle : ToolToggleBase<TilingToggle>
    {
        public const string ID = "PWB/TilingToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.TILING;
        public TilingToggle() : base()
        {
            _iconName = "Tiling";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Tiling", PWBSettings.shortcuts.toolbarTilingToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class ReplacerToggle : ToolToggleBase<ReplacerToggle>
    {
        public const string ID = "PWB/ReplacerToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.REPLACER;
        public ReplacerToggle() : base()
        {
            _iconName = "Replace";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Replacer", PWBSettings.shortcuts.toolbarReplacerToggle.combination.ToString());
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class EraserToggle : ToolToggleBase<EraserToggle>
    {
        public const string ID = "PWB/EraserToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.ERASER;
        public EraserToggle() : base()
        {
            _iconName = "Eraser";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Eraser", PWBSettings.shortcuts.toolbarEraserToggle.combination.ToString());
        }
    }


    [UnityEditor.Overlays.Overlay(typeof(UnityEditor.SceneView), "PWB/Prop Placement", true)]
    public class PWBPropPlacementToolbarOverlay : UnityEditor.Overlays.ToolbarOverlay
    {
        private static PWBPropPlacementToolbarOverlay _instance = null;
        PWBPropPlacementToolbarOverlay() : base(PinToggle.ID, BrushToggle.ID, GravityToggle.ID, LineToggle.ID,
            ShapeToggle.ID, TilingToggle.ID, ReplacerToggle.ID, EraserToggle.ID)
        {
            _instance = this;
            this.displayedChanged += OndisplayedChanged;
#if UNITY_2022_2_OR_NEWER
            collapsedIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + "Brush");
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
