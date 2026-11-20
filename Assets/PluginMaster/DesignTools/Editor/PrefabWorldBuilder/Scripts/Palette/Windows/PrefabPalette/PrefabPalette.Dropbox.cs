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
    public partial class PrefabPalette : UnityEditor.EditorWindow, ISerializationCallbackReceiver
    {
        private void DropBox()
        {
            GUIStyle dragAndDropBoxStyle = new GUIStyle();
            dragAndDropBoxStyle.alignment = TextAnchor.MiddleCenter;
            dragAndDropBoxStyle.fontStyle = FontStyle.Italic;
            dragAndDropBoxStyle.fontSize = 12;
            dragAndDropBoxStyle.normal.textColor = Color.white;
            dragAndDropBoxStyle.wordWrap = true;
            GUI.Box(_scrollViewRect, "Drag and Drop Prefabs Or Folders Here", dragAndDropBoxStyle);
        }

        private void AddLabels(GameObject obj)
        {
            if (!PaletteManager.selectedPalette.brushCreationSettings.addLabelsToDroppedPrefabs) return;
            var labels = new System.Collections.Generic.HashSet<string>(UnityEditor.AssetDatabase.GetLabels(obj));
            int labelCount = labels.Count;
            if (PaletteManager.selectedPalette.brushCreationSettings.addLabelsToDroppedPrefabs)
                labels.UnionWith(PaletteManager.selectedPalette.brushCreationSettings.labels);
            if (labelCount != labels.Count) UnityEditor.AssetDatabase.SetLabels(obj, labels.ToArray());
        }

        private void DropPrefab()
        {
            if (_scrollViewRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Copy;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    bool paletteChanged = false;
                    var items = DropUtils.GetDroppedPrefabs();
                    if (items.Length > 0) PaletteManager.ClearSelection();
                    var i = 0;
                    foreach (var item in items)
                    {
                        AddLabels(item.obj);
                        var brush = MultibrushSettings.Create(item.obj, PaletteManager.selectedPalette);
                        if (brush == null) continue; 
                        
                        RegisterUndo("Add Brush");
                        if (_moveBrush.to < 0)
                        {
                            PaletteManager.selectedPalette.AddBrush(brush);
                            PaletteManager.selectedBrushIdx = PaletteManager.selectedPalette.brushes.Length - 1;
                        }
                        else
                        {
                            var idx = _moveBrush.to + i++;
                            PaletteManager.selectedPalette.InsertBrushAt(brush, idx);
                            PaletteManager.selectedBrushIdx = _moveBrush.to;
                        }
                        paletteChanged = true;
                    }
                    if (paletteChanged) OnPaletteChange();
                    if (draggingBrush && _moveBrush.to >= 0)
                    {
                        _moveBrush.perform = _moveBrush.from != _moveBrush.to;
                        draggingBrush = false;
                    }
                    _showCursor = false;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragExited) _showCursor = false;
            }
        }
    }
}
#pragma warning restore UDR0001
