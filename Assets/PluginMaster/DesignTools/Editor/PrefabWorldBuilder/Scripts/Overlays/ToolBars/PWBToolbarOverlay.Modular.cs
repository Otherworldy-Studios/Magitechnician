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
    public class WallsToggle : ToolToggleBase<WallsToggle>
    {
        public const string ID = "PWB/WallsToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.WALL;
        public WallsToggle() : base()
        {
            _iconName = "Walls";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Walls", PWBSettings.shortcuts.toolbarWallToggle.combination.ToString());
        }
    }
    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class FloorsToggle : ToolToggleBase<FloorsToggle>
    {
        public const string ID = "PWB/FloorsToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.FLOOR;
        public FloorsToggle() : base()
        {
            _iconName = "Floors";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Floors", PWBSettings.shortcuts.toolbarFloorToggle.combination.ToString());
        }
    }
    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class BlockToggle : ToolToggleBase<BlockToggle>
    {
        public const string ID = "PWB/BlockToggle";
        public override string id => ID;
        public override ToolController.Tool tool => ToolController.Tool.BLOCK;
        public BlockToggle() : base()
        {
            _iconName = "Blocks";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = ToggleManager.GetTooltip("Blocks", PWBSettings.shortcuts.toolbarBlockToggle.combination.ToString());
        }
    }
    [UnityEditor.Overlays.Overlay(typeof(UnityEditor.SceneView), "PWB/Modular", true)]
    public class ModularEnvironmentsToolbarOverlay : UnityEditor.Overlays.ToolbarOverlay
    {
        private static ModularEnvironmentsToolbarOverlay _instance = null;
        ModularEnvironmentsToolbarOverlay() : base(FloorsToggle.ID, WallsToggle.ID, BlockToggle.ID
            )
        {
            _instance = this;
            displayedChanged += OndisplayedChanged;
#if UNITY_2022_2_OR_NEWER
            collapsedIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + "Floors");
#endif
        }

        private void OndisplayedChanged(bool value)
        {
            ToolbarOverlayManager.OnToolbarDisplayedChanged();
        }
        public static bool hasInstance => _instance != null;
        public static bool isDisplayed 
            => _instance != null && _instance.displayed;
        public static void SetDisplayed(bool value) { if (_instance != null) _instance.displayed = value; }
    }
}
#endif
#pragma warning restore UDR0001
