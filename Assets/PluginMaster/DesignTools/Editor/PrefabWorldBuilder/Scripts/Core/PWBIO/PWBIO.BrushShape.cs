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
        #region BRUSH SHAPE INDICATOR
        private const float NormalOffset = 0.01f;
        private const float PolygonSideSize = 0.3f;
        private const int MinPolygonSides = 12;
        private const int MaxPolygonSides = 36;
        private const float ShadowOffset = 0.2f;

        private static readonly System.Collections.Generic.List<Vector3> _periPoints
            = new System.Collections.Generic.List<Vector3>();
        private static readonly System.Collections.Generic.List<Vector3> _dropAreaPeriPoints
            = new System.Collections.Generic.List<Vector3>();

        public static void DrawCircleIndicator(
            Vector3 hitPoint,
            Vector3 hitNormal,
            float radius,
            float height,
            Vector3 tangent,
            Vector3 bitangent,
            Vector3 normal,
            bool paintOnPalettePrefabs,
            bool castOnMeshesWithoutCollider,
            int layerMask = -1,
            string[] tags = null,
            bool drawDropArea = false)
        {
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            int polygonSides = Mathf.Clamp((int)(TAU * radius / PolygonSideSize), MinPolygonSides, MaxPolygonSides);

            Vector3 center = hitPoint + hitNormal * NormalOffset;
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.5f);
            UnityEditor.Handles.DrawWireDisc(center, hitNormal, radius, 3);
            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.5f);
            UnityEditor.Handles.DrawWireDisc(center, hitNormal, radius + ShadowOffset, 3);

            if (drawDropArea)
            {
                var heightOffset = normal * height;
                UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.5f);
                UnityEditor.Handles.DrawWireDisc(center + heightOffset, hitNormal, radius, 3);
            }
        }

        private const int MinSideSegments = 4;
        private const int MaxSideSegments = 15;
        private const float SegmentDivisor = 0.3f;

        public static void DrawSquareIndicator(
        Vector3 hitPoint,
        float radius,
        float height,
        Vector3 tangent,
        Vector3 bitangent,
        Vector3 normal,
        bool drawDropArea = false)
        {
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            int segments = Mathf.Clamp((int)(radius * 2f / SegmentDivisor), MinSideSegments, MaxSideSegments);
            int segmentCount = segments * 4;
            float segmentSize = radius * 2f / segments;

            _periPoints.Clear();
            _dropAreaPeriPoints.Clear();

            Vector3 heightOffset = normal * height;
            for (int i = 0; i < segmentCount; i++)
            {
                int side = i / segments;
                int idx = i % segments;
                Vector3 peri = hitPoint;
                switch (side)
                {
                    case 0: peri += tangent * (segmentSize * idx - radius) + bitangent * radius; break;
                    case 1: peri += bitangent * (radius - segmentSize * idx) + tangent * radius; break;
                    case 2: peri += tangent * (radius - segmentSize * idx) - bitangent * radius; break;
                    default: peri += bitangent * (segmentSize * idx - radius) - tangent * radius; break;
                }

                _periPoints.Add(peri);
                if (drawDropArea) _dropAreaPeriPoints.Add(peri + heightOffset);
            }

            if (_periPoints.Count > 0)
            {
                _periPoints.Add(_periPoints[0]);
                UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.7f);
                UnityEditor.Handles.DrawAAPolyLine(8, _periPoints.ToArray());

                UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.7f);
                UnityEditor.Handles.DrawAAPolyLine(4, _periPoints.ToArray());
            }

            if (drawDropArea && _dropAreaPeriPoints.Count > 0)
            {
                _dropAreaPeriPoints.Add(_dropAreaPeriPoints[0]);
                UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.5f);
                UnityEditor.Handles.DrawAAPolyLine(3, _dropAreaPeriPoints.ToArray());
            }
        }

        private static void BrushRadiusShortcuts(CircleToolBase settings)
        {
            if (PWBSettings.shortcuts.brushRadius.Check())
            {
                var combi = PWBSettings.shortcuts.brushRadius.combination;
                var delta = Mathf.Sign(combi.delta);
                settings.radius = Mathf.Max(settings.radius * (1f + delta * 0.03f), 0.05f);
                if (settings is BrushToolSettings)
                {
                    if (BrushManager.settings.heightType == BrushToolSettings.HeightType.RADIUS)
                        BrushManager.settings.maxHeightFromCenter = BrushManager.settings.radius;
                }
                ToolProperties.RepainWindow();
            }
        }
        #endregion

        #region CIRCLE TOOL
        private static void DrawCircleTool(Vector3 center, Camera camera, Color color, float radius)
        {

            const float polygonSideSize = 0.3f;
            const int minPolygonSides = 8;
            const int maxPolygonSides = 60;
            var polygonSides = Mathf.Clamp((int)(TAU * radius / polygonSideSize),
                minPolygonSides, maxPolygonSides);

            var periPoints = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < polygonSides; ++i)
            {
                var radians = TAU * i / (polygonSides - 1f);
                var tangentDir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                var worldDir = TangentSpaceToWorld(camera.transform.right, camera.transform.up, tangentDir);
                periPoints.Add(center + (worldDir * radius));
            }
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 1f);
            UnityEditor.Handles.DrawAAPolyLine(5, periPoints.ToArray());
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawAAPolyLine(5, periPoints.ToArray());
        }

        private static void GetCircleToolTargets(Ray mouseRay, Camera camera, ISelectionBrushTool selectionBrushTool,
            float radius, System.Collections.Generic.HashSet<GameObject> targets)
        {
#if UNITY_2021_1_OR_NEWER
            using (UnityEngine.Pool.ListPool<GameObject>
                .Get(out System.Collections.Generic.List<GameObject> nearbyObjectsList))
#else
            var nearbyObjectsList = new System.Collections.Generic.List<GameObject>();
#endif
            {
                boundsOctree.GetColliding(nearbyObjectsList, mouseRay, radius, maxDistance: float.PositiveInfinity);
                targets.Clear();
                if (selectionBrushTool.outermostPrefabFilter)
                {
                    foreach (var nearby in nearbyObjectsList)
                    {
                        if (nearby == null) continue;
                        var outermost = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(nearby);
                        if (outermost == null) targets.Add(nearby);
                        else if (!targets.Contains(outermost)) targets.Add(outermost);
                    }
                }
                else targets.UnionWith(nearbyObjectsList);
            }
#if UNITY_2021_1_OR_NEWER
            using (UnityEngine.Pool.ListPool<GameObject>
               .Get(out System.Collections.Generic.List<GameObject> toSelectList))
#else
            var toSelectList = new System.Collections.Generic.List<GameObject>();
#endif
            {
                toSelectList.AddRange(targets);
                targets.Clear();

                var closestDistSqr = float.MaxValue;
                int numToSelectListCount = toSelectList.Count;
                for (int i = 0; i < numToSelectListCount; ++i)
                {
                    var obj = toSelectList[i];
                    if (obj == null) continue;
                    var magnitude = BoundsUtils.GetAverageMagnitude(obj.transform);
                    if (radius < magnitude / 2) continue;

                    if (selectionBrushTool.onlyTheClosest)
                    {
                        var pos = obj.transform.position;
                        var distSqr = (pos - camera.transform.position).sqrMagnitude;
                        if (distSqr < closestDistSqr)
                        {
                            closestDistSqr = distSqr;
                            targets.Clear();
                            targets.Add(obj);
                        }
                        continue;
                    }
                    targets.Add(obj);
                }
            }

            if (selectionBrushTool.command == ISelectionBrushTool.Command.SELECT_PALETTE_PREFABS)
            {
                var palette = PaletteManager.selectedPalette;
                if (palette == null) targets.Clear();
                else targets.RemoveWhere(obj => obj == null || !palette.ContainsSceneObject(obj));
            }
            else if (selectionBrushTool.command == ISelectionBrushTool.Command.SELECT_BRUSH_PREFABS)
            {
                var brush = PaletteManager.selectedBrush;
                if (brush == null) targets.Clear();
                else targets.RemoveWhere(obj => obj == null || !brush.ContainsSceneObject(obj));
            }
        }
        #endregion
    }
}
#pragma warning restore UDR0001
