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
    public static partial class PWBIO
    {
        private static double _lastSelectionChangeTime = -1;
        private static bool _pendingTrackerCheck = false;

        private static void OnSelectionChangedForTrackerMonitor()
        {
            _lastSelectionChangeTime = UnityEditor.EditorApplication.timeSinceStartup;
            if (_pendingTrackerCheck) return;
            _pendingTrackerCheck = true;
            UnityEditor.EditorApplication.update -= CheckTrackerAfterSelectionChange;
            UnityEditor.EditorApplication.update += CheckTrackerAfterSelectionChange;
        }

        private static readonly System.Collections.Generic.HashSet<string> _editorsExcludedFromRecovery
            = new System.Collections.Generic.HashSet<string>
        {
            "AvatarEditor",
            "TerrainInspector",
            "ClothInspector",
            "LightProbeGroupEditor",
            "ReflectionProbeEditor",
            "ParticleSystemEditor",
            "SkinnedMeshRendererEditor"
        };
        private static readonly System.Collections.Generic.HashSet<string> _builtinToolTypeNames
            = new System.Collections.Generic.HashSet<string>
        {
            "MoveTool",
            "RotateTool",
            "ScaleTool",
            "RectTool",
            "TransformTool",
            "NoneTool",
            "ViewTool"
        };

        private static bool IsSpecialEditModeActive()
        {
            var activeToolType = UnityEditor.EditorTools.ToolManager.activeToolType;
            if (activeToolType == null) return false;
            if (activeToolType.Namespace != "UnityEditor") return true;
            return !_builtinToolTypeNames.Contains(activeToolType.Name);
        }

        private static bool ActiveEditorIsExcludedFromRecovery(UnityEditor.Editor[] editors)
        {
            if (editors == null) return false;
            foreach (var editor in editors)
            {
                if (editor == null) continue;
                if (_editorsExcludedFromRecovery.Contains(editor.GetType().Name)) return true;
            }
            return false;
        }

        private static bool InspectorNeedsRecovery()
        {
            var selected = UnityEditor.Selection.objects;
            if (selected == null || selected.Length == 0) return false;

            if (IsSpecialEditModeActive()) return false;

            var tracker = UnityEditor.ActiveEditorTracker.sharedTracker;
            if (tracker.isDirty) return false;

            var editors = tracker.activeEditors;

            if (editors == null || editors.Length == 0) return true;

            if (ActiveEditorIsExcludedFromRecovery(editors)) return false;

            var primary = editors[0];
            if (primary == null || primary.target == null)
            {
                return true;
            }

            var targets = primary.targets;
            if (targets == null || targets.Length == 0)
            {
                return true;
            }
#if UNITY_6000_3_OR_NEWER
            var shown = new System.Collections.Generic.HashSet<EntityId>();
#else
            var shown = new System.Collections.Generic.HashSet<int>();
#endif
            foreach (var t in targets)
            {
                if (t == null) continue;
#if UNITY_6000_3_OR_NEWER
                shown.Add(t.GetEntityId());
                if (t is Component c && c != null) shown.Add(c.gameObject.GetEntityId());
#else
                shown.Add(t.GetInstanceID());
                if (t is Component c && c != null) shown.Add(c.gameObject.GetInstanceID());
#endif
            }

            foreach (var obj in selected)
            {
                if (obj == null) continue;
#if UNITY_6000_3_OR_NEWER
                if (!shown.Contains(obj.GetEntityId()))
#else
                if (!shown.Contains(obj.GetInstanceID()))
#endif
                {
                    return true;
                }
            }
            return false;
        }

        private static void RebuildInspector()
        {
            UnityEditor.ActiveEditorTracker.sharedTracker.ForceRebuild();
            RepaintInspectors();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            UnityEditor.EditorApplication.delayCall += () =>
            {
                UnityEditor.ActiveEditorTracker.sharedTracker.ForceRebuild();
                RepaintInspectors();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            };
        }

        private static void RepaintInspectors()
        {
            var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType == null) return;

            var inspectors = Resources.FindObjectsOfTypeAll(inspectorType);
            foreach (var inspector in inspectors)
            {
                if (inspector is UnityEditor.EditorWindow window)
                    window.Repaint();
            }
        }

        private static void CheckTrackerAfterSelectionChange()
        {
            if (UnityEditor.EditorApplication.timeSinceStartup - _lastSelectionChangeTime < 0.15) return;

            UnityEditor.EditorApplication.update -= CheckTrackerAfterSelectionChange;
            _pendingTrackerCheck = false;

            if (!InspectorNeedsRecovery()) return;
            RebuildInspector();
        }


        private static bool _inspectorRecoveryActive = false;
        private static double _inspectorRecoveryTimeout = 0;
        private const double INSPECTOR_RECOVERY_INTERVAL = 0.1;
        private static double _lastInspectorRecoveryTime = 0;
        private static bool _inspectorRebuildAttempted = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        public static void BeginInspectorRecovery()
        {
            _inspectorRecoveryTimeout = UnityEditor.EditorApplication.timeSinceStartup + 10.0;
            _lastInspectorRecoveryTime = 0;
            _inspectorRebuildAttempted = false;
            if (_inspectorRecoveryActive) return;
            _inspectorRecoveryActive = true;
            UnityEditor.EditorApplication.update -= InspectorRecoveryUpdate;
            UnityEditor.EditorApplication.update += InspectorRecoveryUpdate;
        }

        private static void InspectorRecoveryUpdate()
        {
            var now = UnityEditor.EditorApplication.timeSinceStartup;

            if (now > _inspectorRecoveryTimeout) { StopInspectorRecovery(); return; }

            if (!InspectorNeedsRecovery()) { StopInspectorRecovery(); return; }

            if (now - _lastInspectorRecoveryTime < INSPECTOR_RECOVERY_INTERVAL) return;
            _lastInspectorRecoveryTime = now;

            if (_inspectorRebuildAttempted) return;
            _inspectorRebuildAttempted = true;

            RebuildInspector();
        }

        private static void StopInspectorRecovery()
        {
            UnityEditor.EditorApplication.update -= InspectorRecoveryUpdate;
            _inspectorRecoveryActive = false;
        }
    }
}
#pragma warning restore UDR0001
