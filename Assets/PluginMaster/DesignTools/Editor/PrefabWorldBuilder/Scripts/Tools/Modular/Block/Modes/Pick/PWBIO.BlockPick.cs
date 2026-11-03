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

        private static string _blockPickObjectName = "None";
        private static string _blockPickBrushName = "None";


        private static void BlockPickInput()
        {
            if (boundsOctree.Count == 0) UpdateOctree();

            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);

            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            boundsOctree.GetColliding(nearbyObjects, mouseRay, float.MaxValue);

            GameObject targetObj = null;
            float minDistance = float.MaxValue;

            foreach (var obj in nearbyObjects)
            {
                if (obj == null) continue;
                if (!obj.activeInHierarchy) continue;

                var objBounds = BoundsUtils.GetBoundsRecursive(obj.transform);
                if (objBounds.IntersectRay(mouseRay, out float distance))
                {
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        targetObj = obj;
                    }
                }
            }

            _blockPickObjectName = "None";
            _blockPickBrushName = "None";

            if (targetObj != null)
            {
                var outermostPrefab = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(targetObj);
                if (outermostPrefab != null)
                {
                    _blockPickObjectName = outermostPrefab.name;

                    var brushIdx = PaletteManager.selectedPalette.FindBrushIdx(outermostPrefab);
                    if (brushIdx >= 0)
                    {
                        var brush = PaletteManager.selectedPalette.GetBrush(brushIdx);
                        if (brush != null) _blockPickBrushName = brush.name;
                    }
                }
                else
                {
                    _blockPickObjectName = targetObj.name;
                }

                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
                {
                    if (outermostPrefab != null)
                    {
                        var brushIdx = PaletteManager.selectedPalette.FindBrushIdx(outermostPrefab);
                        if (brushIdx >= 0)
                        {
                            PaletteManager.SelectBrush(brushIdx);
                        }
                        else
                        {
                            var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(outermostPrefab);
                            if (prefabAsset != null)
                                PrefabPalette.instance.CreateBrushFromSelection(prefabAsset);
                        }
                        PrefabPalette.RepaintWindow();
                        repaint = true;
                    }
                    Event.current.Use();
                }
            }
        }
    }
}
#pragma warning restore UDR0001
