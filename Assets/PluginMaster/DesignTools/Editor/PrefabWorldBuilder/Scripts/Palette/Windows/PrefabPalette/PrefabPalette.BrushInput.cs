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
    public partial class PrefabPalette : UnityEditor.EditorWindow, ISerializationCallbackReceiver
    {
        private void BrushMouseEventHandler(BrushInputData data)
        {
            void DeselectAllButCurrent()
            {
                PaletteManager.ClearSelection();
                PaletteManager.selectedBrushIdx = data.index;
                PaletteManager.AddToSelection(data.index);
            }
            if (data == null) return;
            if (data.eventType == EventType.MouseMove) Event.current.Use();
            if (data.eventType == EventType.MouseDown && Event.current.button == 0)
            {
                void DeselectAll() => PaletteManager.ClearSelection();
                void ToggleCurrent()
                {
                    if (PaletteManager.SelectionContains(data.index)) PaletteManager.RemoveFromSelection(data.index);
                    else PaletteManager.AddToSelection(data.index);
                    PaletteManager.selectedBrushIdx = PaletteManager.selectionCount == 1
                        ? PaletteManager.idxSelection[0] : -1;
                }
                if (data.shift)
                {
                    var selectedIdx = PaletteManager.selectedBrushIdx;
                    var sign = (int)Mathf.Sign(data.index - selectedIdx);
                    if (sign != 0)
                    {
                        PaletteManager.ClearSelection();
                        for (int i = selectedIdx; i != data.index; i += sign)
                        {
                            if (FilteredListContains(i)) PaletteManager.AddToSelection(i);
                        }
                        PaletteManager.AddToSelection(data.index);
                        PaletteManager.selectedBrushIdx = selectedIdx;
                    }
                    else DeselectAllButCurrent();
                }
                else
                {
                    if (data.control && PaletteManager.selectionCount < 2)
                    {
                        if (PaletteManager.selectedBrushIdx == data.index) DeselectAll();
                        else ToggleCurrent();
                    }
                    else if (data.control && PaletteManager.selectionCount > 1) ToggleCurrent();
                    else if (!data.control && PaletteManager.selectionCount < 2)
                    {
                        if (PaletteManager.selectedBrushIdx == data.index) DeselectAll();
                        else DeselectAllButCurrent();
                    }
                    else if (!data.control && PaletteManager.selectionCount > 1) DeselectAllButCurrent();
                }
                Event.current.Use();
                Repaint();
            }
            else if (data.eventType == EventType.ContextClick)
            {
                BrushContext(data.index);
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0
               && Event.current.delta != Vector2.zero)
            {
                if (!PaletteManager.SelectionContains(data.index)) DeselectAllButCurrent();
                UnityEditor.DragAndDrop.PrepareStartDrag();
                if (Event.current.control)
                {
                    UnityEditor.DragAndDrop.StartDrag("Dragging brush");
                    UnityEditor.DragAndDrop.objectReferences = new Object[]
                        { PaletteManager.selectedBrush.GetItemAt(0).prefab };
                    UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Move;
                }
                else
                {
                    PWBIO.sceneDragReceiver.brushId = data.index;
                    SceneDragAndDrop.StartDrag(PWBIO.sceneDragReceiver, "Dragging brush");
                    UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Copy;
                }
                draggingBrush = true;
                _moveBrush.from = data.index;
                _moveBrush.perform = false;
                _moveBrush.to = -1;
            }
            else if (data.eventType == EventType.DragUpdated && Event.current.button == 0)
            {
                if (Event.current.control) UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Move;
                else
                {
                    UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Copy;
                    var size = new Vector2(4, PaletteManager.iconSize);
                    var min = data.rect.min;
                    bool toTheRight = data.mouseX - data.rect.center.x > 0;
                    min.x = toTheRight ? data.rect.max.x : min.x - size.x;
                    _cursorRect = new Rect(min, size);
                    _showCursor = true;
                    _moveBrush.to = data.index;
                    if (toTheRight) ++_moveBrush.to;
                }
            }
            else if (data.eventType == EventType.DragPerform && Event.current.button == 0 && !Event.current.control)
            {
                _moveBrush.to = data.index;
                bool toTheRight = data.mouseX - data.rect.center.x > 0;
                if (toTheRight) ++_moveBrush.to;
                if (draggingBrush)
                {
                    _moveBrush.perform = _moveBrush.from != _moveBrush.to;
                    draggingBrush = false;
                }
                _showCursor = false;
            }
            else if (data.eventType == EventType.DragExited && Event.current.button == 0 && !Event.current.control)
            {
                _showCursor = false;
                draggingBrush = false;
                _moveBrush.to = -1;
            }
        }
    }
}
#pragma warning restore UDR0001
