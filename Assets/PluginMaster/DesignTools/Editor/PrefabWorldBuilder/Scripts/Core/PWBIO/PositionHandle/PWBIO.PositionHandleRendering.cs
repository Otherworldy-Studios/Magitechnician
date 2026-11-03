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
        private static void DrawPositionHandleMesh(Mesh mesh, Matrix4x4 matrix, Color color)
        {
            if (mesh == null || _positionHandleMeshMaterial == null) return;

            _positionHandleMeshMaterial.SetColor(_colorPropertyId, color);
            _positionHandleMeshMaterial.SetPass(0);

            Graphics.DrawMeshNow(mesh, matrix);
        }

        private static void DrawPositionHandleAxis(
            Vector3 origin,
            Vector3 direction,
            float length,
            float radius,
            float coneLength,
            float coneRadius,
            Color color)
        {
            var normalizedDirection = direction.normalized;
            var rotation = GetDirectionRotation(normalizedDirection);

            DrawPositionHandleMesh(
                _positionHandleAxisMesh,
                Matrix4x4.TRS(origin, rotation, new Vector3(radius, radius, length)),
                color);

            DrawPositionHandleMesh(
                _positionHandleConeMesh,
                Matrix4x4.TRS(origin + normalizedDirection * length, rotation,
                new Vector3(coneRadius, coneRadius, coneLength)), color);
        }

        private static void DrawPositionHandlePlane(
            Vector3 center,
            Vector3 axisA,
            Vector3 axisB,
            Vector3 normal,
            float halfSize,
            Color fillColor,
            Color outlineColor,
            float outlineRadius)
        {
            var planeMatrix = Matrix4x4.identity;
            planeMatrix.SetColumn(0, new Vector4(axisA.x * halfSize * 2f,
                axisA.y * halfSize * 2f, axisA.z * halfSize * 2f, 0f));
            planeMatrix.SetColumn(1, new Vector4(axisB.x * halfSize * 2f,
                axisB.y * halfSize * 2f, axisB.z * halfSize * 2f, 0f));
            planeMatrix.SetColumn(2, new Vector4(normal.x, normal.y, normal.z, 0f));
            planeMatrix.SetColumn(3, new Vector4(center.x, center.y, center.z, 1f));

            DrawPositionHandleMesh(_positionHandlePlaneMesh, planeMatrix, fillColor);

            var corner0 = center + (-axisA - axisB) * halfSize;
            var corner1 = center + (axisA - axisB) * halfSize;
            var corner2 = center + (axisA + axisB) * halfSize;
            var corner3 = center + (-axisA + axisB) * halfSize;

            DrawPositionHandleSegment(corner0, corner1, outlineRadius, outlineColor);
            DrawPositionHandleSegment(corner1, corner2, outlineRadius, outlineColor);
            DrawPositionHandleSegment(corner2, corner3, outlineRadius, outlineColor);
            DrawPositionHandleSegment(corner3, corner0, outlineRadius, outlineColor);
        }

        private static void DrawPositionHandleSegment(Vector3 start, Vector3 end, float radius, Color color)
        {
            var direction = end - start;
            var length = direction.magnitude;
            if (length <= Mathf.Epsilon) return;

            DrawPositionHandleMesh(
                _positionHandleAxisMesh,
                Matrix4x4.TRS(start, GetDirectionRotation(direction / length), new Vector3(radius, radius, length)),
                color);
        }

        private static Color GetHandlePartColor(
            Color baseColor,
            PositionHandleDragMode partMode,
            PositionHandleDragMode hoverMode,
            PositionHandleDragMode activeMode)
        {
            if (activeMode == partMode)
            {
                baseColor.r = Mathf.Min(1f, baseColor.r + 0.35f);
                baseColor.g = Mathf.Min(1f, baseColor.g + 0.35f);
                baseColor.b = Mathf.Min(1f, baseColor.b + 0.35f);
                baseColor.a = Mathf.Min(1f, baseColor.a + 0.25f);
            }
            else if (hoverMode == partMode)
            {
                baseColor.r = Mathf.Min(1f, baseColor.r + 0.2f);
                baseColor.g = Mathf.Min(1f, baseColor.g + 0.2f);
                baseColor.b = Mathf.Min(1f, baseColor.b + 0.2f);
                baseColor.a = Mathf.Min(1f, baseColor.a + 0.15f);
            }

            return baseColor;
        }

        private static Quaternion GetDirectionRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon) return Quaternion.identity;

            direction.Normalize();
            var upReference = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.95f
                ? Vector3.right
                : Vector3.up;

            return Quaternion.LookRotation(direction, upReference);
        }

        private static void EnsurePositionHandleMeshResources()
        {
            if (_positionHandleMeshMaterial == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null) shader = Shader.Find("Unlit/Color");

                _positionHandleMeshMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                _positionHandleMeshMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _positionHandleMeshMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _positionHandleMeshMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _positionHandleMeshMaterial.SetInt("_ZWrite", 0);
                _positionHandleMeshMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                _positionHandleMeshMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay;
            }

            if (_positionHandleAxisMesh == null)
            {
                _positionHandleAxisMesh = CreateCylinderMesh(16);
                _positionHandleAxisMesh.name = "PWB_PositionHandleAxisMesh";
                _positionHandleAxisMesh.hideFlags = HideFlags.HideAndDontSave;
            }

            if (_positionHandleConeMesh == null)
            {
                _positionHandleConeMesh = CreateConeMesh(20);
                _positionHandleConeMesh.name = "PWB_PositionHandleConeMesh";
                _positionHandleConeMesh.hideFlags = HideFlags.HideAndDontSave;
            }

            if (_positionHandlePlaneMesh == null)
            {
                _positionHandlePlaneMesh = CreatePlaneMesh();
                _positionHandlePlaneMesh.name = "PWB_PositionHandlePlaneMesh";
                _positionHandlePlaneMesh.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static Mesh CreatePlaneMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCylinderMesh(int segments)
        {
            var vertices = new Vector3[segments * 2 + 2];
            var triangles = new int[segments * 12];

            vertices[0] = Vector3.zero;
            vertices[1] = Vector3.forward;

            for (var i = 0; i < segments; ++i)
            {
                var angle = Mathf.PI * 2f * i / segments;
                vertices[2 + i * 2] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices[3 + i * 2] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 1f);
            }

            var triangleIndex = 0;
            for (var i = 0; i < segments; ++i)
            {
                var next = (i + 1) % segments;
                var bottom0 = 2 + i * 2;
                var top0 = bottom0 + 1;
                var bottom1 = 2 + next * 2;
                var top1 = bottom1 + 1;

                triangles[triangleIndex++] = bottom0;
                triangles[triangleIndex++] = bottom1;
                triangles[triangleIndex++] = top1;
                triangles[triangleIndex++] = bottom0;
                triangles[triangleIndex++] = top1;
                triangles[triangleIndex++] = top0;
                triangles[triangleIndex++] = 0;
                triangles[triangleIndex++] = bottom1;
                triangles[triangleIndex++] = bottom0;
                triangles[triangleIndex++] = 1;
                triangles[triangleIndex++] = top0;
                triangles[triangleIndex++] = top1;
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateConeMesh(int segments)
        {
            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 6];

            vertices[0] = Vector3.zero;
            vertices[1] = Vector3.forward;

            for (var i = 0; i < segments; ++i)
            {
                var angle = Mathf.PI * 2f * i / segments;
                vertices[2 + i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            }

            var triangleIndex = 0;
            for (var i = 0; i < segments; ++i)
            {
                var next = (i + 1) % segments;
                var base0 = 2 + i;
                var base1 = 2 + next;

                triangles[triangleIndex++] = base0;
                triangles[triangleIndex++] = base1;
                triangles[triangleIndex++] = 1;
                triangles[triangleIndex++] = 0;
                triangles[triangleIndex++] = base1;
                triangles[triangleIndex++] = base0;
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
#pragma warning restore UDR0001
