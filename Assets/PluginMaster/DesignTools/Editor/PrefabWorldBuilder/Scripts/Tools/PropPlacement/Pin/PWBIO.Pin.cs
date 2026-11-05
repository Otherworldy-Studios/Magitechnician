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

        private static bool _pinned = false;
        private static Vector3 _pinMouse = Vector3.zero;
        private static RaycastHit _pinHit = new RaycastHit();
        private static Vector3 _pinAngle = Vector3.zero;
        private static Vector3 _previousPinAngle = Vector3.zero;
        private static float _pinScale = 1f;
        private static Vector3 _pinOffset = Vector3.zero;
        private static Transform _pinSurface = null;
        private static bool _snapToVertex = false;
        private static float _pinDistanceFromSurface = 0f;
        private static Vector3 _pinProjectionDirection = Vector3.down;
        private static bool _pinFlipX = false;

        private static void PinDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            PinInput(sceneView);
            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) return;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            bool snappedToVertex = false;
            var closestVertexInfo = new RaycastHit();
            var settings = PinManager.settings;
            if (_snapToVertex)
                snappedToVertex = SnapToVertex(mouseRay, out closestVertexInfo, sceneView.in2DMode);
            if (snappedToVertex)
                DrawPin(sceneView, closestVertexInfo, false);
            else
            {
                if (settings.mode == PinSettings.PaintMode.ON_SHAPE)
                {
                    if (GridRaycast(mouseRay, out RaycastHit planeHit))
                        DrawPin(sceneView, planeHit, GridManager.settings.snappingEnabled);
                    else _paintStroke.Clear();
                }
                else
                {
                    if (PWBToolRaycast(mouseRay, out RaycastHit mouseHit, out GameObject collider, float.MaxValue,
                        -1, settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider,
                        ignoreSceneColliders: settings.ignoreSceneColliders))
                    {
                        DrawPin(sceneView, mouseHit, GridManager.settings.snappingEnabled);
                        _pinSurface = collider.transform;
                    }
                    else if (_pinned) DrawPin(sceneView, _pinHit, GridManager.settings.snappingEnabled);
                    else if (settings.mode == PinSettings.PaintMode.AUTO)
                    {
                        if (GridRaycast(mouseRay, out RaycastHit planeHit))
                            DrawPin(sceneView, planeHit, GridManager.settings.snappingEnabled);
                    }
                    else _paintStroke.Clear();
                }
            }
            PinInfoText(sceneView);
        }

        private static void PinInfoText(UnityEditor.SceneView sceneView)
        {
            if (!PWBCore.staticData.showInfoText) return;
            if (_paintStroke.Count == 0) return;
            var p = _paintStroke[0].position;
            var r = _paintStroke[0].rotation.eulerAngles;
            var s = _paintStroke[0].scale;
            var labelTexts = new System.Collections.Generic.List<string>
            { _paintStroke[0].prefab.name, $"P: {p.x.ToString("F2")}, {p.y.ToString("F2")}, {p.z.ToString("F2")}"};
            if (r != Vector3.zero) labelTexts.Add($"R: {r.x.ToString("F2")}, {r.y.ToString("F2")}, {r.z.ToString("F2")}");
            if (s != Vector3.one) labelTexts.Add($"S: {s.x.ToString("F2")}, {s.y.ToString("F2")}, {s.z.ToString("F2")}");
            if (!Mathf.Approximately(_pinDistanceFromSurface, 0f))
                labelTexts.Add($"Surface offset: {_pinDistanceFromSurface.ToString("F2")}");
            InfoText.Draw(sceneView, labelTexts.ToArray());
        }

        private static void PinInput(UnityEditor.SceneView sceneView)
        {
            if (PaletteManager.selectedBrush == null) return;
            var keyCode = Event.current.keyCode;
            if (Event.current.button == 0)
            {
                if (Event.current.type == EventType.MouseUp && !Event.current.alt)
                {
                    if (PinManager.settings.flattenTerrain) FlatenTerrain();
                    Paint(PinManager.settings);
                    _pinned = false;
                    RepaintInspectorAfterScenePaint();
                    Event.current.Use();
                }
                if (Event.current.type == EventType.KeyDown)
                {
                    if (PWBSettings.shortcuts.pinMoveHandlesUp.Check()) _pinOffset = nextBoundLayer;
                    else if (PWBSettings.shortcuts.pinMoveHandlesDown.Check()) _pinOffset = prevBoundLayer;
                    else if (PWBSettings.shortcuts.pinSelectNextHandle.Check()) _pinOffset = nextBoundPoint;
                    else if (PWBSettings.shortcuts.pinSelectPrevHandle.Check()) _pinOffset = prevBoundPoint;
                    else if (PWBSettings.shortcuts.pinSelectPivotHandle.Check()) _pinOffset = pivotBoundPoint;
                    //add rotation around Y
                    else if (PWBSettings.shortcuts.pinRotate90YCW.Check()) _pinAngle.y = (_pinAngle.y + 90) % 360;
                    else if (PWBSettings.shortcuts.pinRotate90YCCW.Check()) _pinAngle.y = (_pinAngle.y - 90) % 360;
                    else if (PWBSettings.shortcuts.pinRotateAStepYCW.Check()) _pinAngle.y -= PinManager.rotationSnapValue;
                    else if (PWBSettings.shortcuts.pinRotateAStepYCCW.Check()) _pinAngle.y += PinManager.rotationSnapValue;
                    //add rotation around X
                    else if (PWBSettings.shortcuts.pinRotate90XCW.Check()) _pinAngle.x = (_pinAngle.x + 90) % 360;
                    else if (PWBSettings.shortcuts.pinRotate90XCCW.Check()) _pinAngle.x = (_pinAngle.x - 90) % 360;
                    else if (PWBSettings.shortcuts.pinRotateAStepXCW.Check()) _pinAngle.x -= PinManager.rotationSnapValue;
                    else if (PWBSettings.shortcuts.pinRotateAStepXCCW.Check()) _pinAngle.x += PinManager.rotationSnapValue;
                    //add rotation around Z
                    else if (PWBSettings.shortcuts.pinRotate90ZCW.Check()) _pinAngle.z = (_pinAngle.z + 90) % 360;
                    else if (PWBSettings.shortcuts.pinRotate90ZCCW.Check()) _pinAngle.z = (_pinAngle.z - 90) % 360;
                    else if (PWBSettings.shortcuts.pinRotateAStepZCW.Check()) _pinAngle.z -= PinManager.rotationSnapValue;
                    else if (PWBSettings.shortcuts.pinRotateAStepZCCW.Check()) _pinAngle.z += PinManager.rotationSnapValue;
                    //reset rotation
                    else if (PWBSettings.shortcuts.pinResetRotation.Check()) _pinAngle = Vector3.zero;
                    else if (PWBSettings.shortcuts.pinSnapRotationToGrid.Check())
                    {
                        snapPinRotationToGrid = true;
                        sceneView.Repaint();
                        repaint = true;
                    }
                    //distance to surface
                    else if (PWBSettings.shortcuts.pinSubtract1UnitFromSurfDist.Check()) _pinDistanceFromSurface -= 1f;
                    else if (PWBSettings.shortcuts.pinAdd1UnitToSurfDist.Check()) _pinDistanceFromSurface += 1f;
                    else if (PWBSettings.shortcuts.pinSubtract01UnitFromSurfDist.Check()) _pinDistanceFromSurface -= 0.1f;
                    else if (PWBSettings.shortcuts.pinAdd01UnitToSurfDist.Check()) _pinDistanceFromSurface += 0.1f;
                    else if (PWBSettings.shortcuts.pinResetSurfDist.Check()) _pinDistanceFromSurface = 0;
                    else if (PWBSettings.shortcuts.pinResetScale.Check()) UpdatePinScale(1f);
                    //Flip
                    else if (PWBSettings.shortcuts.pinFlipX.Check()) _pinFlipX = !_pinFlipX;

                    else if (PWBSettings.shortcuts.pinToggleRepeatItem.Check())
                    {
                        PinManager.settings.repeat = !PinManager.settings.repeat;
                        ToolProperties.RepainWindow();
                    }
                    else if (PWBSettings.shortcuts.pinSelectPreviousItem.Check())
                    {
                        BrushstrokeManager.SetNextPinBrushstroke(-1);
                        sceneView.Repaint();
                        repaint = true;
                    }
                    else if (PWBSettings.shortcuts.pinSelectNextItem.Check())
                    {
                        BrushstrokeManager.SetNextPinBrushstroke(1);
                        sceneView.Repaint();
                        repaint = true;
                    }
                }
            }
            else
            {
                if (Event.current.type == EventType.MouseDown && Event.current.control)
                {
                    _pinned = true;
                    _pinMouse = Event.current.mousePosition;
                    _previousPinAngle = _pinAngle;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp && !Event.current.control) _pinned = false;
            }
            const float DEG_PER_PIXEL = 1.8f; //180deg/100px

            if (PWBSettings.shortcuts.pinSelectNextItemScroll.Check())
            {
                var scrollSign = Mathf.Sign(Event.current.delta.y);
                Event.current.Use();
                BrushstrokeManager.SetNextPinBrushstroke((int)scrollSign);
                sceneView.Repaint();
                repaint = true;
            }
            else if (PWBSettings.shortcuts.pinRotateAroundY.Check())
            {
                var combi = PWBSettings.shortcuts.pinRotateAroundY.combination;
                if (combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL) _pinAngle.y += combi.delta;
                else if (combi.isMouseDragEvent) _pinAngle.y -= combi.delta * DEG_PER_PIXEL;
                _previousPinAngle = _pinAngle;
            }
            else if (PWBSettings.shortcuts.pinRotateAroundYSnaped.Check())
            {
                var combi = PWBSettings.shortcuts.pinRotateAroundYSnaped.combination;
                if (combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL)
                {
                    var scrollSign = Mathf.Sign(Event.current.delta.y);
                    _pinAngle.y += scrollSign * PinManager.rotationSnapValue;
                }
                else if (combi.isMouseDragEvent)
                {
                    _pinAngle.y = _previousPinAngle.y - combi.delta * DEG_PER_PIXEL;
                    _previousPinAngle.y = _pinAngle.y;
                    if (PinManager.rotationSnapValue > 0)
                        _pinAngle.y = Mathf.Round(_pinAngle.y / PinManager.rotationSnapValue) * PinManager.rotationSnapValue;
                }
            }
            else if (PWBSettings.shortcuts.pinRotateAroundX.Check())
            {
                var combi = PWBSettings.shortcuts.pinRotateAroundX.combination;
                if (combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL) _pinAngle.x += Event.current.delta.y;
                else if (combi.isMouseDragEvent) _pinAngle.x -= combi.delta * DEG_PER_PIXEL;
                _previousPinAngle = _pinAngle;
            }
            else if (PWBSettings.shortcuts.pinRotateAroundXSnaped.Check())
            {
                var combi = PWBSettings.shortcuts.pinRotateAroundXSnaped.combination;
                if (combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL)
                {
                    var scrollSign = Mathf.Sign(Event.current.delta.y);
                    _pinAngle.x += scrollSign * PinManager.rotationSnapValue;
                }
                else if (combi.isMouseDragEvent)
                {
                    _pinAngle.x = _previousPinAngle.x + combi.delta * DEG_PER_PIXEL;
                    _previousPinAngle.x = _pinAngle.x;
                    if (PinManager.rotationSnapValue > 0)
                        _pinAngle.x = Mathf.Round(_pinAngle.x / PinManager.rotationSnapValue) * PinManager.rotationSnapValue;
                }
            }
            else if (PWBSettings.shortcuts.pinRotateAroundZ.Check())
            {
                var combi = PWBSettings.shortcuts.pinRotateAroundZ.combination;
                if (combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL) _pinAngle.z += Event.current.delta.y;
                else if (combi.isMouseDragEvent) _pinAngle.z -= combi.delta * DEG_PER_PIXEL;
                _previousPinAngle = _pinAngle;
            }
            else if (PWBSettings.shortcuts.pinRotateAroundZSnaped.Check())
            {
                var combi = PWBSettings.shortcuts.pinRotateAroundZSnaped.combination;
                if (combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL)
                {
                    var scrollSign = Mathf.Sign(Event.current.delta.y);
                    _pinAngle.z += scrollSign * PinManager.rotationSnapValue;
                }
                else if (combi.isMouseDragEvent)
                {
                    _pinAngle.z = _previousPinAngle.z + combi.delta * DEG_PER_PIXEL;
                    _previousPinAngle.z = _pinAngle.z;
                    if (PinManager.rotationSnapValue > 0)
                        _pinAngle.z = Mathf.Round(_pinAngle.z / PinManager.rotationSnapValue) * PinManager.rotationSnapValue;
                }
            }
            else if (PWBSettings.shortcuts.pinSurfDist.Check())
            {
                var combi = PWBSettings.shortcuts.pinSurfDist.combination;
                if (combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL)
                    _pinDistanceFromSurface += Event.current.delta.y * 0.04f;
                else if (combi.isMouseDragEvent) _pinDistanceFromSurface += combi.delta * 0.04f;
            }
            else if (PWBSettings.shortcuts.pinScale.Check())
            {

                if (PWBSettings.shortcuts.pinScale.combination.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL)
                {
                    var scrollSign = Mathf.Sign(Event.current.delta.y);
                    UpdatePinScale(Mathf.Max(_pinScale * (1f + scrollSign * 0.05f), 0.01f));
                    sceneView.Repaint();
                    repaint = true;
                }
                else if (PWBSettings.shortcuts.pinScale.combination.isMouseDragEvent)
                {
                    UpdatePinScale(Mathf.Max(_pinScale * (1f + PWBSettings.shortcuts.pinScale.combination.delta * 0.003f),
                        0.01f));
                    sceneView.Repaint();
                    repaint = true;
                }
            }

            if ((keyCode == KeyCode.LeftControl || keyCode == KeyCode.RightControl)
                && Event.current.type == EventType.KeyUp) _pinned = false;
        }
    }
}
#pragma warning restore UDR0001
