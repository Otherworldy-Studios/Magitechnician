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
        public static void ReplaceSelected()
        {
            var replacerSettings = new ReplacerSettings();
            _paintStroke.Clear();
            SelectionManager.UpdateSelection();
            var targets = SelectionManager.topLevelSelection;
            BrushstrokeManager.UpdateReplacerBrushstroke(clearDictionary: true, targets);
            ReplacePreview(UnityEditor.SceneView.lastActiveSceneView.camera, replacerSettings, targets);
            var newObjects = new System.Collections.Generic.HashSet<GameObject>();
            Replace(newObjects);
            if (newObjects != null)
                if (newObjects.Count > 0) UnityEditor.Selection.objects = newObjects.ToArray();
        }
        private static void PaletteInput(UnityEditor.SceneView sceneView)
        {
            void Repaint()
            {
                PrefabPalette.RepaintWindow();
                sceneView.Repaint();
                repaint = true;
                AsyncRepaint();
            }
            if (PWBSettings.shortcuts.palettePreviousBrush.Check())
            {
                PaletteManager.SelectPreviousBrush();
                Repaint();
            }
            else if (PWBSettings.shortcuts.paletteNextBrush.Check())
            {
                PaletteManager.SelectNextBrush();
                Repaint();
            }
            if (PWBSettings.shortcuts.paletteNextBrushScroll.Check())
            {
                Event.current.Use();
                if (PWBSettings.shortcuts.paletteNextBrushScroll.combination.delta > 0) PaletteManager.SelectNextBrush();
                else PaletteManager.SelectPreviousBrush();
                Repaint();
            }
            if (PWBSettings.shortcuts.paletteNextPaletteScroll.Check())
            {
                Event.current.Use();
                if (PWBSettings.shortcuts.paletteNextPaletteScroll.combination.delta > 0) PaletteManager.SelectNextPalette();
                else PaletteManager.SelectPreviousPalette();
                Repaint();
            }
            if (PWBSettings.shortcuts.palettePreviousPalette.Check())
            {
                PaletteManager.SelectPreviousPalette();
                Repaint();
            }
            else if (PWBSettings.shortcuts.paletteNextPalette.Check())
            {
                PaletteManager.SelectNextPalette();
                Repaint();
            }
            if (PWBSettings.shortcuts.paletteReplaceSceneSelection.Check())
            {
                ReplaceSelected();
            }
            var pickShortcutOn = PWBSettings.shortcuts.palettePickBrush.Check();
            var pickBrush = PaletteManager.pickingBrushes && Event.current.button == 0
                && Event.current.type == EventType.MouseDown;
            if (pickShortcutOn || pickBrush)
            {
                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (PWBToolRaycast(mouseRay, out RaycastHit mouseHit, out GameObject collider, float.MaxValue, layerMask: -1,
                    paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true, ignoreSceneColliders: true))
                {
                    var target = collider.gameObject;
                    var outermostPrefab = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(target);
                    if (outermostPrefab != null) target = outermostPrefab;
                    var brushIdx = PaletteManager.selectedPalette.FindBrushIdx(target);
                    if (brushIdx >= 0) PaletteManager.SelectBrush(brushIdx);
                    else if (outermostPrefab != null)
                    {
                        var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(outermostPrefab);
                        PrefabPalette.instance.CreateBrushFromSelection(prefabAsset);
                    }
                }
                Event.current.Use();
                if (!pickShortcutOn && pickBrush) PaletteManager.pickingBrushes = false;
            }
            if (PaletteManager.pickingBrushes
                && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Escape)
            {
                PaletteManager.pickingBrushes = false;
            }
            if (PaletteManager.pickingBrushes)
            {
                if (boundsOctree.Count == 0) UpdateOctree();
                var labelTexts = new string[] { $"Brush Picker", "Object: " };
                var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                var objName = "None";
                if (PWBToolRaycast(mouseRay, out RaycastHit mouseHit, out GameObject collider, float.MaxValue, layerMask: -1,
                    paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true, ignoreSceneColliders: true))
                {
                    var target = collider.gameObject;
                    var outermostPrefab = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(target);
                    if (outermostPrefab != null) objName = outermostPrefab.name;
                }
                labelTexts[1] += objName;
                InfoText.Draw(sceneView, labelTexts.ToArray());
            }
            if (PWBSettings.shortcuts.palettePickBrush.holdKeysAndClickCombination.holdingChanged)
                PaletteManager.pickingBrushes = PWBSettings.shortcuts.palettePickBrush.holdKeysAndClickCombination.holdingKeys;
        }
        async static void AsyncRepaint()
        {
            await System.Threading.Tasks.Task.Delay(500);
            repaint = true;
        }
    }
}
#pragma warning restore UDR0001
