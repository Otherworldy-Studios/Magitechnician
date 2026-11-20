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
#if UNITY_2019_1_OR_NEWER
using UnityEngine;

namespace PluginMaster
{
    public static partial class Shortcuts
    {
        #region TOGGLE TOOLS
        public const string PWB_TOGGLE_FLOOR_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Floor Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_FLOOR_SHORTCUT_ID,
            KeyCode.F, UnityEditor.ShortcutManagement.ShortcutModifiers.Shift
            | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleFloor() => PWBIO.ToogleTool(ToolController.Tool.FLOOR);

        public const string PWB_TOGGLE_WALL_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Wall Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_WALL_SHORTCUT_ID,
            KeyCode.W, UnityEditor.ShortcutManagement.ShortcutModifiers.Shift
            | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleWall() => PWBIO.ToogleTool(ToolController.Tool.WALL);
        public const string PWB_TOGGLE_BLOCK_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Block Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_BLOCK_SHORTCUT_ID,
            KeyCode.B, UnityEditor.ShortcutManagement.ShortcutModifiers.Shift
            | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleBlock() => PWBIO.ToogleTool(ToolController.Tool.BLOCK);
        public const string PWB_TOGGLE_PIN_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Pin Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_PIN_SHORTCUT_ID,
            KeyCode.Alpha1, UnityEditor.ShortcutManagement.ShortcutModifiers.Shift
            | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void TogglePin() => PWBIO.ToogleTool(ToolController.Tool.PIN);

        public const string PWB_TOGGLE_BRUSH_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Brush Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_BRUSH_SHORTCUT_ID, KeyCode.Alpha2,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleBrush() => PWBIO.ToogleTool(ToolController.Tool.BRUSH);

        public const string PWB_TOGGLE_GRAVITY_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Gravity Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_GRAVITY_SHORTCUT_ID, KeyCode.Alpha3,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleGravity() => PWBIO.ToogleTool(ToolController.Tool.GRAVITY);

        public const string PWB_TOGGLE_LINE_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Line Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_LINE_SHORTCUT_ID, KeyCode.Alpha4,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleLine() => PWBIO.ToogleTool(ToolController.Tool.LINE);

        public const string PWB_TOGGLE_SHAPE_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Shape Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_SHAPE_SHORTCUT_ID, KeyCode.Alpha5,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleShape() => PWBIO.ToogleTool(ToolController.Tool.SHAPE);

        public const string PWB_TOGGLE_TILING_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Tiling Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_TILING_SHORTCUT_ID, KeyCode.Alpha6,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleTiling() => PWBIO.ToogleTool(ToolController.Tool.TILING);

        public const string PWB_TOGGLE_REPLACER_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Replacer Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_REPLACER_SHORTCUT_ID, KeyCode.Alpha7,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleReplacer() => PWBIO.ToogleTool(ToolController.Tool.REPLACER);

        public const string PWB_TOGGLE_ERASER_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Eraser Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_ERASER_SHORTCUT_ID, KeyCode.Alpha8,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleEraser() => PWBIO.ToogleTool(ToolController.Tool.ERASER);

        public const string PWB_TOGGLE_SELECTION_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Selection Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_SELECTION_SHORTCUT_ID, KeyCode.Alpha9,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleSelection() => PWBIO.ToogleTool(ToolController.Tool.SELECTION);

        public const string PWB_TOGGLE_CIRCLE_SELECT_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Circle Selection Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_CIRCLE_SELECT_SHORTCUT_ID, KeyCode.O,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleCircleSelect() => PWBIO.ToogleTool(ToolController.Tool.CIRCLE_SELECT);

        public const string PWB_TOGGLE_EXTRUDE_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Extrude Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_EXTRUDE_SHORTCUT_ID, KeyCode.X,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleExtrude() => PWBIO.ToogleTool(ToolController.Tool.EXTRUDE);

        public const string PWB_TOGGLE_MIRROR_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Mirror Tool";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_MIRROR_SHORTCUT_ID, KeyCode.M,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void ToggleMirror() => PWBIO.ToogleTool(ToolController.Tool.MIRROR);
        #endregion
        #region WINDOWS & OVERLAYS
        public const string PWB_CLOSE_ALL_WINDOWS_ID = "Prefab World Builder/Close All Windows";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_CLOSE_ALL_WINDOWS_ID, KeyCode.End,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void PWBCloseAllWindows()
        {
            PWBIO.CloseAllWindows();
        }


        public const string PWB_TOGGLE_OVERLAYS_ID = "Prefab World Builder/Toggle Overlays";
        [UnityEditor.ShortcutManagement.Shortcut(PWB_TOGGLE_OVERLAYS_ID, KeyCode.P,
            UnityEditor.ShortcutManagement.ShortcutModifiers.Shift | UnityEditor.ShortcutManagement.ShortcutModifiers.Alt)]
        private static void PWBToggleOverlays()
        {
            PWBIO.ToggleOverlays();
        }
        #endregion

    }
}
#endif
#pragma warning restore UDR0001
