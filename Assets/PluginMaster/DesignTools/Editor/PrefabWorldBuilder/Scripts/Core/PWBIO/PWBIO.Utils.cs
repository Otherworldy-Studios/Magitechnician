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
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    public static partial class PWBIO
    {
        #region SELECTION
        public static void UpdateSelection()
        {
            RepaintInspectorIfNeededAfterPaint();
            if (SelectionManager.topLevelSelection.Length == 0)
            {
                if (tool == ToolController.Tool.EXTRUDE)
                {
                    _initialExtrudePosition = _extrudeHandlePosition = _selectionSize = Vector3.zero;
                    _extrudeDirection = Vector3Int.zero;
                }
                return;
            }
            if (tool == ToolController.Tool.EXTRUDE)
            {
                var selectionBounds = ExtrudeManager.settings.space == Space.World
                    ? BoundsUtils.GetSelectionBounds(SelectionManager.topLevelSelection)
                    : BoundsUtils.GetSelectionBounds(SelectionManager.topLevelSelection,
                    ExtrudeManager.settings.rotationAccordingTo == ExtrudeSettings.RotationAccordingTo.FRIST_SELECTED
                    ? SelectionManager.topLevelSelection.First().transform.rotation
                    : SelectionManager.topLevelSelection.Last().transform.rotation);
                _initialExtrudePosition = _extrudeHandlePosition = selectionBounds.center;
                _selectionSize = selectionBounds.size;
                _extrudeDirection = Vector3Int.zero;
            }
            else if (tool == ToolController.Tool.SELECTION)
            {
                _selectedBoxPointIdx = 10;
                _selectionRotation = Quaternion.identity;
                _selectionChanged = true;
                _editingSelectionHandlePosition = false;
                var rotation = GetSelectionRotation();
                _selectionBounds = BoundsUtils.GetSelectionBounds(SelectionManager.topLevelSelection, rotation);
                _selectionRotation = rotation;
            }
        }

        private static bool _repaintInspectorAfterPaint = false;

        private static void RepaintInspectorAfterScenePaint()
        {
            GUIUtility.hotControl = 0;
            GUIUtility.keyboardControl = 0;
            _repaintInspectorAfterPaint = true;
            UnityEditor.EditorApplication.delayCall -= RepaintInspectorIfNeededAfterPaint;
            UnityEditor.EditorApplication.delayCall += RepaintInspectorIfNeededAfterPaint;
        }

        private static void RepaintInspectorIfNeededAfterPaint()
        {
            if (!_repaintInspectorAfterPaint) return;
            _repaintInspectorAfterPaint = false;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        #endregion

        #region UNSAVED CHANGES
        private const string UNSAVED_CHANGES_TITLE = "Unsaved Changes";
        private const string UNSAVED_CHANGES_MESSAGE = "There are unsaved changes.\nWhat would you like to do?";
        private const string UNSAVED_CHANGES_OK = "Save";
        private const string UNSAVED_CHANGES_CANCEL = "Don't Save";

        private static bool _showingSaveDialog = false;

        private static void DisplaySaveDialog(System.Action Save)
        {
            if (_showingSaveDialog) return;
            _showingSaveDialog = true;
            try
            {
                if (UnityEditor.EditorUtility.DisplayDialog(UNSAVED_CHANGES_TITLE,
                    UNSAVED_CHANGES_MESSAGE, UNSAVED_CHANGES_OK, UNSAVED_CHANGES_CANCEL)) Save();
                else repaint = true;
            }
            finally
            {
                _showingSaveDialog = false;
            }
        }
        private static void AskIfWantToSave(ToolController.ToolState state, System.Action Save)
        {
            switch (PWBCore.staticData.unsavedChangesAction)
            {
                case PWBData.UnsavedChangesAction.ASK:
                    if (state == ToolController.ToolState.EDIT) DisplaySaveDialog(Save);
                    break;
                case PWBData.UnsavedChangesAction.SAVE:
                    if (state == ToolController.ToolState.EDIT) Save();
                    BrushstrokeManager.ClearBrushstroke();
                    break;
                case PWBData.UnsavedChangesAction.DISCARD:
                    repaint = true;
                    return;
            }
        }

        #endregion

        #region SCENE COLLIDERS
#if UNITY_6000_3_OR_NEWER
        private static System.Collections.Generic.HashSet<EntityId> _sceneColliders
            = new System.Collections.Generic.HashSet<EntityId>();
#else
        private static System.Collections.Generic.HashSet<int> _sceneColliders
            = new System.Collections.Generic.HashSet<int>();
#endif
        public static void UpdateSceneColliderSet()
        {
            Collider[] allColliders;
            if (isInPrefabMode)
            {
                allColliders = prefabStage.prefabContentsRoot.GetComponentsInChildren<Collider>();
            }
            else
            {
#if UNITY_6000_4_OR_NEWER
                allColliders = Object.FindObjectsByType<Collider>();
#elif UNITY_2022_2_OR_NEWER
                allColliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
#else
                allColliders = Object.FindObjectsOfType<Collider>();
#endif
            }
            _sceneColliders.Clear();
#if UNITY_6000_3_OR_NEWER
            foreach (var c in allColliders) _sceneColliders.Add(c.GetEntityId());
#else
            foreach (var c in allColliders) _sceneColliders.Add(c.GetInstanceID());
#endif
        }
        #endregion

        #region DRAG AND DROP
        public class SceneDragReceiver : ISceneDragReceiver
        {
            private int _brushID = -1;
            public int brushId { get => _brushID; set => _brushID = value; }
            public void PerformDrag(Event evt) { }
            public void StartDrag() { }
            public void StopDrag() { }
            public UnityEditor.DragAndDropVisualMode UpdateDrag(Event evt, EventType eventType)
            {
                PrefabPalette.instance.DeselectAllButThis(_brushID);
                ToolController.current = ToolController.Tool.PIN;
                return UnityEditor.DragAndDropVisualMode.Generic;
            }
        }
        private static SceneDragReceiver _sceneDragReceiver = new SceneDragReceiver();
        public static SceneDragReceiver sceneDragReceiver => _sceneDragReceiver;

        #endregion

        #region TOOLBAR
        public static void ToogleTool(ToolController.Tool tool)
        {
#if UNITY_2021_2_OR_NEWER
#else
            if (PWBToolbar.instance == null) PWBToolbar.ShowWindow();
#endif
            ToolController.current = ToolController.current == tool ? ToolController.Tool.NONE : tool;
            PWBToolbar.RepaintWindow();
        }
        #endregion

        #region MODULAR
        private static bool _modularDeleteMode = false;
        private static Vector3 GetCenterToPivot(GameObject prefab, Vector3 scaleMult, Quaternion rotation)
        {
            var itemBounds = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation);
            var centerToPivotGlobal = prefab.transform.position - itemBounds.center;
            var centerToPivotLocal = Quaternion.Inverse(prefab.transform.rotation) * centerToPivotGlobal;
            var result = rotation * Vector3.Scale(centerToPivotLocal, scaleMult);
            return result;
        }
        #endregion

        #region GIZMOS
        private static void GizmosInput()
        {
            if (PWBSettings.shortcuts.gizmosToggleInfotext.Check())
            {
                PWBCore.staticData.ToggleInfoText();
            }
        }
        #endregion

        #region MATERIALS & MESHES
        private static Material _transparentRedMaterial = null;
        public static Material transparentRedMaterial
        {
            get
            {
                if (_transparentRedMaterial == null)
                    _transparentRedMaterial = new Material(Shader.Find("PluginMaster/TransparentRed"));
                return _transparentRedMaterial;
            }
        }

        private static Material _transparentRedMaterial2 = null;
        public static Material transparentRedMaterial2
        {
            get
            {
                if (_transparentRedMaterial2 == null)
                    _transparentRedMaterial2 = new Material(Shader.Find("PluginMaster/TransparentRed2"));
                return _transparentRedMaterial2;
            }
        }

        private static Material _snapBoxMaterial = null;
        public static Material snapBoxMaterial
        {
            get
            {
                if (_snapBoxMaterial == null)
                    _snapBoxMaterial = new Material(Shader.Find("PluginMaster/SnapBox"));
                return _snapBoxMaterial;
            }
        }

        private static Mesh _cubeMesh = null;
        private static Mesh cubeMesh
        {
            get
            {
                if (_cubeMesh == null) _cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                return _cubeMesh;
            }
        }
        #endregion

        #region PREFAB STAGE
#if UNITY_2021_1_OR_NEWER
        public static bool isInPrefabMode => prefabStage != null;

        public static UnityEditor.SceneManagement.PrefabStage prefabStage
            => UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

        private static void OnPrefabStageChanged(UnityEditor.SceneManagement.PrefabStage stage)
        {
            ClearPreviewState();
            if (ToolController.current == ToolController.Tool.NONE) return;
            SetOctreeDirty();
        }
#else
        public static bool isInPrefabMode => false;
        public class PrefabStage
        {
            public string assetPath = null;
            public GameObject prefabContentsRoot = null;
            public UnityEngine.SceneManagement.Scene scene;
        }
        public static PrefabStage prefabStage => null;
#endif
        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene,
            UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            ClearPreviewState();
            SetOctreeDirty();
        }
        #endregion

        #region BRUSHTROKE
        private static void BrushstrokeMouseEvents(BrushToolBase settings)
        {
            if (PaletteManager.selectedBrush == null) return;
            if (Event.current.button == 0 && !Event.current.alt && Event.current.type == EventType.MouseUp
                && PaletteManager.selectedBrush.patternMachine != null
                && PaletteManager.selectedBrush.restartPatternForEachStroke)
            {
                PaletteManager.selectedBrush.patternMachine.Reset();
                BrushstrokeManager.UpdateBrushstroke();
            }
            else if (PWBSettings.shortcuts.brushUpdatebrushstroke.Check())
            {
                BrushstrokeManager.UpdateBrushstroke();
                repaint = true;
            }
            else if (PWBSettings.shortcuts.brushResetRotation.Check()) _brushAngle = 0;
            else if (PWBSettings.shortcuts.brushDensity.Check()
                && settings.brushShape != BrushToolBase.BrushShape.POINT)
            {
                settings.density += (int)Mathf.Sign(PWBSettings.shortcuts.brushDensity.combination.delta);
                ToolProperties.RepainWindow();
            }
            else if (PWBSettings.shortcuts.brushRotate.Check())
                _brushAngle -= PWBSettings.shortcuts.brushRotate.combination.delta * 1.8f; //180deg/100px
            if (Event.current.button == 1)
            {
                if (Event.current.type == EventType.MouseDown && (Event.current.control || Event.current.shift))
                {
                    _pinned = true;
                    _pinMouse = Event.current.mousePosition;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp && !Event.current.control && !Event.current.shift)
                    _pinned = false;
            }
            if ((Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl
                || Event.current.keyCode == KeyCode.RightShift || Event.current.keyCode == KeyCode.LeftShift)
                && Event.current.type == EventType.KeyUp) _pinned = false;
        }

        private static Vector3 _prevMousePos = Vector3.zero;
        private static Vector3 _strokeDirection = Vector3.forward;
        private static void UpdateStrokeDirection(Vector3 hitPoint)
        {
            var dir = hitPoint - _prevMousePos;
            if (dir.sqrMagnitude > 0.3f)
            {
                _strokeDirection = hitPoint - _prevMousePos;
                _prevMousePos = hitPoint;
            }
        }
        #endregion

        #region GEOMETRY UTILITIES
        private static Quaternion GetRotationFromNormal(Vector3 normal)
        {
            bool GetYOnPlane(out float y)
            {
                y = 0;
                if (Mathf.Approximately(normal.y, 0f)) return false;
                y = -normal.x / normal.y;
                return true;
            }
            bool GetZOnPlane(out float z)
            {
                z = 0f;
                if (Mathf.Approximately(normal.z, 0f)) return false;
                z = -normal.x / normal.z;
                return true;
            }
            bool GetXOnPlane(out float x)
            {
                x = 0f;
                if (Mathf.Approximately(normal.x, 0f)) return false;
                x = -normal.z / normal.x;
                return true;
            }
            var right = Vector3.right;
            if (GetYOnPlane(out float y)) right = new Vector3(1, y, 0);
            else if (GetZOnPlane(out float z)) right = new Vector3(1, 0, z);
            else if (GetXOnPlane(out float x)) right = new Vector3(x, 0, 1);
            var forward = Vector3.Cross(right, normal);
            return Quaternion.LookRotation(forward, normal);
        }

        private static Quaternion GetRotationFromNormal(Vector3 normal, Quaternion currentRotation)
        {
            var rotation = GetRotationFromNormal(normal);
            var currentYRotaion = Quaternion.Euler(0, currentRotation.eulerAngles.y, 0);
            return rotation * currentYRotaion;
        }

        private static Vector3 TangentSpaceToWorld(Vector3 tangent, Vector3 bitangent, Vector2 tangentSpacePos)
            => (tangent * tangentSpacePos.x + bitangent * tangentSpacePos.y);
        #endregion

    }
}
#pragma warning restore UDR0001
