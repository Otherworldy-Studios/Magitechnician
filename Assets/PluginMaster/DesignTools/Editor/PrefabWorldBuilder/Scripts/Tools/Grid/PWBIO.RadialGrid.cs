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
        private static void DrawRadialGrid(AxesUtils.Axis axis, UnityEditor.SceneView sceneView, int maxCells, float snapSize)
        {
            var rotation = GridManager.settings.rotation;
            var otherAxes = AxesUtils.GetOtherAxes(axis);
            var normal = rotation * AxesUtils.GetVector(1, axis);
            var tangent = rotation * AxesUtils.GetVector(1, otherAxes[0]);
            var bitangent = rotation * AxesUtils.GetVector(1, otherAxes[1]);
            float radius = 0f;
            for (int i = 1; i < maxCells; ++i)
            {
                radius += snapSize;
                var alpha = (maxCells - i) * 0.5f / (float)maxCells;
                switch (axis)
                {
                    case AxesUtils.Axis.X:
                        UnityEditor.Handles.color = new Color(1f, 0.5f, 0.5f, alpha);
                        break;
                    case AxesUtils.Axis.Y:
                        UnityEditor.Handles.color = new Color(0.5f, 1f, 0.5f, alpha);
                        break;
                    case AxesUtils.Axis.Z:
                        UnityEditor.Handles.color = new Color(0.5f, 0.5f, 1f, alpha);
                        break;
                }
                DrawGridCricle(GridManager.settings.origin, normal, tangent, bitangent, radius);

                for (int j = 0; j < GridManager.settings.radialSectors; ++j)
                {
                    var radians = TAU * j / GridManager.settings.radialSectors;
                    var tangentDir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                    var worldDir = TangentSpaceToWorld(tangent, bitangent, tangentDir);
                    var points = new Vector3[]
                    {
                    GridManager.settings.origin + (worldDir * (radius - snapSize)),
                    GridManager.settings.origin + (worldDir * (radius))
                    };
                    UnityEditor.Handles.DrawAAPolyLine(1, points);
                }
            }
        }

        private static void DrawGridCricle(Vector3 center, Vector3 normal,
            Vector3 tangent, Vector3 bitangent, float radius)
        {
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            const float polygonSideSize = 0.3f;
            const int minPolygonSides = 12;
            const int maxPolygonSides = 60;
            var polygonSides = Mathf.Clamp((int)(TAU * radius / polygonSideSize), minPolygonSides, maxPolygonSides);

            var periPoints = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < polygonSides; ++i)
            {
                var radians = TAU * i / (polygonSides - 1f);
                var tangentDir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                var worldDir = TangentSpaceToWorld(tangent, bitangent, tangentDir);
                var periPoint = center + (worldDir * (radius));
                periPoints.Add(periPoint);
            }
            UnityEditor.Handles.DrawAAPolyLine(4 * UnityEditor.Handles.color.a, periPoints.ToArray());
        }
    }
}
#pragma warning restore UDR0001
