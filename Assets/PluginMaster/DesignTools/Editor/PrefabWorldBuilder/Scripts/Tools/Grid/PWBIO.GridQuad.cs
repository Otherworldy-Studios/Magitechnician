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

        private static Mesh _gridMesh = null;
        private static Material _gridMaterial = null;
        private static Material _grid64Material = null;
        private static Material _majorLinesGridMaterial = null;


        private static void DrawGridQuad(AxesUtils.Axis axis, UnityEditor.SceneView sceneView)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                if (Event.current != null && _gridNeedsRepaint)
                {
                    sceneView.Repaint();
                }
                return;
            }

            _lastGridRepaintFrame = Time.frameCount;
            _gridNeedsRepaint = false;

            if (_gridMaterial == null)
                _gridMaterial = new Material(Resources.Load<Material>("Materials/Grid"));
            if (_grid64Material == null)
                _grid64Material = new Material(Resources.Load<Material>("Materials/Grid64"));
            if (_majorLinesGridMaterial == null)
                _majorLinesGridMaterial = new Material(_gridMaterial);
            if (_gridMesh == null)
            {
                _gridMesh = new Mesh();
                _gridMesh.vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, 0),
                    new Vector3(0.5f, -0.5f, 0),
                    new Vector3(0.5f, 0.5f, 0),
                    new Vector3(-0.5f, 0.5f, 0)
                };
                _gridMesh.uv = new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 1)
                };
                _gridMesh.triangles = new int[] { 0, 1, 2, 2, 3, 0 };
                _gridMesh.RecalculateNormals();
            }

            var rotation = GridManager.settings.rotation;
            var origin = GridManager.settings.origin;
            Camera cam = sceneView.camera;
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;
            Vector3 camForward = cam.transform.forward;
            Vector3 planeNormal = rotation * (
                axis == AxesUtils.Axis.X ? Vector3.right :
                axis == AxesUtils.Axis.Y ? Vector3.up :
                Vector3.forward
            );
            Plane gridPlane = new Plane(planeNormal, origin);
            float enter;
            Vector3 focusPoint = origin;
            Ray camRay = new Ray(camPos, camForward);
            if (gridPlane.Raycast(camRay, out enter))
                focusPoint = camRay.GetPoint(enter);

            int cellsCount = 10;
            Vector2 sceneViewSize = sceneView.position.size;
            cellsCount = Mathf.Max((int)sceneViewSize.x, (int)sceneViewSize.y);
            if (cellsCount % 2 == 1) ++cellsCount;

            var snapSize = GridManager.settings.step;
            void DrawMesh(Vector3 snapStepFactor, Material mat, float alpha)
            {
                if (mat == null) return;

                var gridSize = Vector3.Scale(snapSize, snapStepFactor);
                float quadCellSizeX = 1f;
                float quadCellSizeY = 1f;
                Quaternion localRotation = Quaternion.identity;
                var color = new Color(0.5f, 0.5f, 0.5f, alpha);
                switch (axis)
                {
                    case AxesUtils.Axis.X:
                        quadCellSizeX = gridSize.y;
                        quadCellSizeY = gridSize.z;
                        localRotation = Quaternion.Euler(0, 90, 90);
                        color.r = 1f;
                        break;
                    case AxesUtils.Axis.Y:
                        quadCellSizeX = gridSize.x;
                        quadCellSizeY = gridSize.z;
                        localRotation = Quaternion.Euler(90, 0, 0);
                        color.g = 1f;
                        break;
                    case AxesUtils.Axis.Z:
                        quadCellSizeX = gridSize.x;
                        quadCellSizeY = gridSize.y;
                        color.b = 1f;
                        break;
                }
                Vector3 scale = new Vector3(cellsCount * quadCellSizeX, cellsCount * quadCellSizeY, 1);
                Quaternion quadRotation = rotation * localRotation;
                Vector3 quadPosition = SnapPosition(focusPoint, onGrid: true, applySettings: false,
                    snapStepFactor, ignoreMidpoints: true);

                var trs = Matrix4x4.TRS(quadPosition, quadRotation, scale);
                var tiling = new Vector2(cellsCount, cellsCount);

                mat.SetVector("_Tiling", tiling);
                mat.SetTextureScale("_MainTex", tiling);
                mat.SetColor("_Color", color);

                mat.SetPass(0);
                Graphics.DrawMeshNow(_gridMesh, trs);
            }

            var majorMat = _majorLinesGridMaterial;
            var f = Mathf.Min(GridManager.settings.majorLinesGap.x,
                GridManager.settings.majorLinesGap.y, GridManager.settings.majorLinesGap.z);
            if (f <= 4) majorMat = _grid64Material;

            DrawMesh(snapStepFactor: GridManager.settings.majorLinesGap, majorMat, alpha: 0.3f);
            DrawMesh(snapStepFactor: Vector3.one, _gridMaterial, alpha: 0.7f);
        }

    }
}
#pragma warning restore UDR0001
