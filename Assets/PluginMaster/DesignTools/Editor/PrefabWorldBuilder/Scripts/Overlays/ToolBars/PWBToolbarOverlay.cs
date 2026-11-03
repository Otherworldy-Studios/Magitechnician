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
    #region TOGGLE MANAGER
    public static class ToggleManager
    {
        
        private static System.Collections.Generic.Dictionary<ToolController.Tool, IPWBToogle> _toogles = null;
        private static System.Collections.Generic.Dictionary<ToolController.Tool, IPWBToogle> toogles
        {
            get
            {
                if (_toogles == null)
                {
                    _toogles = new System.Collections.Generic.Dictionary<ToolController.Tool, IPWBToogle>()
                    {
                        {ToolController.Tool.PIN,  PinToggle.instance },
                        {ToolController.Tool.BRUSH, BrushToggle.instance},
                        {ToolController.Tool.GRAVITY, GravityToggle.instance},
                        {ToolController.Tool.LINE, LineToggle.instance},
                        {ToolController.Tool.SHAPE, ShapeToggle.instance},
                        {ToolController.Tool.TILING, TilingToggle.instance},
                        {ToolController.Tool.REPLACER, ReplacerToggle.instance},
                        {ToolController.Tool.ERASER, EraserToggle.instance},
                        {ToolController.Tool.SELECTION, SelectionToggle.instance},
                        {ToolController.Tool.CIRCLE_SELECT, CircleSelectToggle.instance},
                        {ToolController.Tool.EXTRUDE, ExtrudeToggle.instance},
                        {ToolController.Tool.MIRROR, MirrorToggle.instance}
                    };
                }
                return _toogles;
            }
        }

        public static void DeselectOthers(string id)
        {
            foreach (var toggle in toogles.Values)
            {
                if (toggle == null) continue;
                if (id != toggle.id && toggle.value) toggle.value = false;
            }
        }

        public static string GetTooltip(string tooltip, string keyCombination) => tooltip + " ... " + keyCombination;

        public static string iconPath => UnityEditor.EditorGUIUtility.isProSkin ? "Sprites/" : "Sprites/LightTheme/";
    }
    #endregion
    #region TOGGLE BASE
    interface IPWBToogle
    {
        public string id { get; }
        public ToolController.Tool tool { get; }
        public bool value { get; set; }
    }

    public class PWBToolbarToggle : UnityEditor.Toolbars.EditorToolbarToggle
    {
        protected string _iconName = string.Empty;
        protected async void DoLoadIcon()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
        }
    }


    public abstract class ToolToggleBase<T> : PWBToolbarToggle,
        IPWBToogle where T : UnityEditor.Toolbars.EditorToolbarToggle, new()
    {
        
        private static ToolToggleBase<T> _instance = null;
        public static ToolToggleBase<T> instance => _instance;
        public abstract string id { get; }
        public abstract ToolController.Tool tool { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public ToolToggleBase()
        {
            _instance = this;
            this.RegisterValueChangedCallback(OnValueChange);
            ToolController.OnToolChange -= OnToolChange;
            ToolController.OnToolChange += OnToolChange;
        }

        private void OnToolChange(ToolController.Tool prevTool)
        {
            if (tool == prevTool || tool == ToolController.current) PWBIO.OnToolChange(prevTool);
            if (tool == prevTool && tool != ToolController.current && value) value = false;
            if (tool == ToolController.current && !value) value = true;
        }

        private void OnValueChange(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                ToolController.current = tool;
                ToggleManager.DeselectOthers(id);
            }
            else if (tool == ToolController.current) ToolController.DeselectTool();
        }
    }
    #endregion
    #region TOOLBAR OVERLAY MANAGER
    public static class ToolbarOverlayManager
    {
        public static void OnToolbarDisplayedChanged()
        {
            if (!PWBCore.staticData.closeAllWindowsWhenClosingTheToolbar) return;
            if (PWBPropPlacementToolbarOverlay.isDisplayed) return;
            if (PWBSelectionToolbarOverlay.isDisplayed) return;
            if (PWBGridToolbarOverlay.isDisplayed) return;
            if (ModularEnvironmentsToolbarOverlay.isDisplayed) return;
            if (SettingsAndDocsToolbarOverlay.isDisplayed) return;
            PWBIO.CloseAllWindows();
        }
    }
    #endregion
}
#endif
#pragma warning restore UDR0001
