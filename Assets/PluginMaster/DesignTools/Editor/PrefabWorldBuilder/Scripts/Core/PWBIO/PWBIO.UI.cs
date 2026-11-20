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

namespace PluginMaster
{
    public static partial class PWBIO
    {
        public static void CloseAllWindows(bool closeToolbar = true)
        {
            ToolController.DeselectTool();
            BrushProperties.CloseWindow();
            ToolProperties.CloseWindow();
            PrefabPalette.CloseWindow();
            if (closeToolbar) PWBToolbar.CloseWindow();
        }

        private const string OVERLAYS_STATE_INITIALIZED_KEY       = "PWB_OverlaysStateInitialized";  
        private const string OVERLAYS_VISIBLE_KEY                 = "PWB_OverlaysVisible";           
        private const string GRID_OVERLAY_WAS_OPEN_KEY            = "PWB_GridOverlayWasOpen";        
        private const string PROP_PLACEMENT_OVERLAY_WAS_OPEN_KEY  = "PWB_PropPlacementOverlayWasOpen"; 
        private const string MODULAR_OVERLAY_WAS_OPEN_KEY         = "PWB_ModularOverlayWasOpen";     
        private const string SELECTION_OVERLAY_WAS_OPEN_KEY       = "PWB_SelectionOverlayWasOpen";   
        private const string SETTINGS_OVERLAY_WAS_OPEN_KEY        = "PWB_SettingsOverlayWasOpen";    
        private const string TOOL_MODES_OVERLAY_WAS_OPEN_KEY      = "PWB_ToolModesOverlayWasOpen";   
#if UNITY_2022_2_OR_NEWER
        private const string SHORTCUT_PANEL_WAS_OPEN_KEY          = "PWB_ShortcutPanelWasOpen";      
#endif

        private static bool overlaysVisible
        {
            get => UnityEditor.SessionState.GetBool(OVERLAYS_VISIBLE_KEY, true);
            set => UnityEditor.SessionState.SetBool(OVERLAYS_VISIBLE_KEY, value);
        }

        private static bool gridOverlayWasOpen
        {
            get => UnityEditor.EditorPrefs.GetBool(GRID_OVERLAY_WAS_OPEN_KEY, false);
            set => UnityEditor.EditorPrefs.SetBool(GRID_OVERLAY_WAS_OPEN_KEY, value);
        }

        private static bool propPlacementOverlayWasOpen
        {
            get => UnityEditor.EditorPrefs.GetBool(PROP_PLACEMENT_OVERLAY_WAS_OPEN_KEY, false);
            set => UnityEditor.EditorPrefs.SetBool(PROP_PLACEMENT_OVERLAY_WAS_OPEN_KEY, value);
        }

        private static bool modularOverlayWasOpen
        {
            get => UnityEditor.EditorPrefs.GetBool(MODULAR_OVERLAY_WAS_OPEN_KEY, false);
            set => UnityEditor.EditorPrefs.SetBool(MODULAR_OVERLAY_WAS_OPEN_KEY, value);
        }

        private static bool selectionOverlayWasOpen
        {
            get => UnityEditor.EditorPrefs.GetBool(SELECTION_OVERLAY_WAS_OPEN_KEY, false);
            set => UnityEditor.EditorPrefs.SetBool(SELECTION_OVERLAY_WAS_OPEN_KEY, value);
        }

        private static bool settingsOverlayWasOpen
        {
            get => UnityEditor.EditorPrefs.GetBool(SETTINGS_OVERLAY_WAS_OPEN_KEY, false);
            set => UnityEditor.EditorPrefs.SetBool(SETTINGS_OVERLAY_WAS_OPEN_KEY, value);
        }

        private static bool toolModesOverlayWasOpen
        {
            get => UnityEditor.EditorPrefs.GetBool(TOOL_MODES_OVERLAY_WAS_OPEN_KEY, false);
            set => UnityEditor.EditorPrefs.SetBool(TOOL_MODES_OVERLAY_WAS_OPEN_KEY, value);
        }

#if UNITY_2022_2_OR_NEWER
        private static bool shortcutPanelWasOpen
        {
            get => UnityEditor.EditorPrefs.GetBool(SHORTCUT_PANEL_WAS_OPEN_KEY, false);
            set => UnityEditor.EditorPrefs.SetBool(SHORTCUT_PANEL_WAS_OPEN_KEY, value);
        }
#endif

#if UNITY_2021_2_OR_NEWER
        internal static void EnsureOverlayStateInitialized()
        {
            if (UnityEditor.SessionState.GetBool(OVERLAYS_STATE_INITIALIZED_KEY, false)) return;

            bool instancesReady = PWBGridToolbarOverlay.hasInstance
                                && PWBPropPlacementToolbarOverlay.hasInstance
                                && ModularEnvironmentsToolbarOverlay.hasInstance
                                && PWBSelectionToolbarOverlay.hasInstance
                                && SettingsAndDocsToolbarOverlay.hasInstance
                                && ToolModesOverlay.hasInstance
#if UNITY_2022_2_OR_NEWER
                                && PWBShortcutPanel.hasInstance
#endif
                                ;
            if (!instancesReady) return;

            bool anyVisible = PWBGridToolbarOverlay.isDisplayed
                           || PWBPropPlacementToolbarOverlay.isDisplayed
                           || ModularEnvironmentsToolbarOverlay.isDisplayed
                           || PWBSelectionToolbarOverlay.isDisplayed
                           || SettingsAndDocsToolbarOverlay.isDisplayed
                           || ToolModesOverlay.isDisplayed
#if UNITY_2022_2_OR_NEWER
                           || PWBShortcutPanel.isDisplayed
#endif
                           ;

            if (anyVisible)
            {
                gridOverlayWasOpen          = PWBGridToolbarOverlay.isDisplayed;
                propPlacementOverlayWasOpen = PWBPropPlacementToolbarOverlay.isDisplayed;
                modularOverlayWasOpen       = ModularEnvironmentsToolbarOverlay.isDisplayed;
                selectionOverlayWasOpen     = PWBSelectionToolbarOverlay.isDisplayed;
                settingsOverlayWasOpen      = SettingsAndDocsToolbarOverlay.isDisplayed;
                toolModesOverlayWasOpen     = ToolModesOverlay.isDisplayed;
#if UNITY_2022_2_OR_NEWER
                shortcutPanelWasOpen        = PWBShortcutPanel.isDisplayed;
#endif
                overlaysVisible = true;
            }
            else
            {
                overlaysVisible = false;
            }

            UnityEditor.SessionState.SetBool(OVERLAYS_STATE_INITIALIZED_KEY, true);
        }

        internal static void SaveOverlayStates()
        {
            if (!UnityEditor.SessionState.GetBool(OVERLAYS_STATE_INITIALIZED_KEY, false)) return;

            if (overlaysVisible)
            {
                gridOverlayWasOpen          = PWBGridToolbarOverlay.isDisplayed;
                propPlacementOverlayWasOpen = PWBPropPlacementToolbarOverlay.isDisplayed;
                modularOverlayWasOpen       = ModularEnvironmentsToolbarOverlay.isDisplayed;
                selectionOverlayWasOpen     = PWBSelectionToolbarOverlay.isDisplayed;
                settingsOverlayWasOpen      = SettingsAndDocsToolbarOverlay.isDisplayed;
                toolModesOverlayWasOpen     = ToolModesOverlay.isDisplayed;
#if UNITY_2022_2_OR_NEWER
                shortcutPanelWasOpen        = PWBShortcutPanel.isDisplayed;
#endif
            }
        }
#endif

        public static void ToggleOverlays()
        {
#if UNITY_2021_2_OR_NEWER
            EnsureOverlayStateInitialized();

            if (overlaysVisible)
            {
                gridOverlayWasOpen = PWBGridToolbarOverlay.isDisplayed;
                propPlacementOverlayWasOpen = PWBPropPlacementToolbarOverlay.isDisplayed;
                modularOverlayWasOpen = ModularEnvironmentsToolbarOverlay.isDisplayed;
                selectionOverlayWasOpen = PWBSelectionToolbarOverlay.isDisplayed;
                settingsOverlayWasOpen = SettingsAndDocsToolbarOverlay.isDisplayed;
                toolModesOverlayWasOpen = ToolModesOverlay.isDisplayed;

                PWBGridToolbarOverlay.SetDisplayed(false);
                PWBPropPlacementToolbarOverlay.SetDisplayed(false);
                ModularEnvironmentsToolbarOverlay.SetDisplayed(false);
                PWBSelectionToolbarOverlay.SetDisplayed(false);
                SettingsAndDocsToolbarOverlay.SetDisplayed(false);
                ToolModesOverlay.SetDisplayed(false);
#if UNITY_2022_2_OR_NEWER
                shortcutPanelWasOpen = PWBShortcutPanel.isDisplayed;
                PWBShortcutPanel.SetDisplayed(false);
#endif
                overlaysVisible = false;
            }
            else
            {
                if (gridOverlayWasOpen) PWBGridToolbarOverlay.SetDisplayed(true);
                if (propPlacementOverlayWasOpen) PWBPropPlacementToolbarOverlay.SetDisplayed(true);
                if (modularOverlayWasOpen) ModularEnvironmentsToolbarOverlay.SetDisplayed(true);
                if (selectionOverlayWasOpen) PWBSelectionToolbarOverlay.SetDisplayed(true);
                if (settingsOverlayWasOpen) SettingsAndDocsToolbarOverlay.SetDisplayed(true);
                if (toolModesOverlayWasOpen) ToolModesOverlay.SetDisplayed(true);
#if UNITY_2022_2_OR_NEWER
                if (shortcutPanelWasOpen)        PWBShortcutPanel.SetDisplayed(true);
#endif
                overlaysVisible = true;
            }
#endif
        }
    }
}
#pragma warning restore UDR0001
