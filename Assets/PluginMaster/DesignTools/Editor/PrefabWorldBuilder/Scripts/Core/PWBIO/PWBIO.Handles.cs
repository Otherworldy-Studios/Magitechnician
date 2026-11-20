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

        private static float _blinkingDelta = 0.05f;
        private static float _blinkingValue = 1f;

        private static void DrawDotHandleCap(Vector3 point, float alpha = 1f,
            float scale = 1f, bool selected = false, bool isPivot = false)
        {
            var handleSize = UnityEditor.HandleUtility.GetHandleSize(point);
            var radius = handleSize * 0.0325f * scale * PWBCore.staticData.controPointSize;
            var sizeDelta = handleSize * 0.0125f;

            var camRot = UnityEngine.Camera.current != null
                ? UnityEngine.Camera.current.transform.rotation
                : Quaternion.identity;
            var normal = camRot * Vector3.back;

            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.7f * alpha);
            UnityEditor.Handles.DrawSolidDisc(point, normal, radius);

            var fillColor = selected ? PWBCore.staticData.selectedContolPointColor
                : (isPivot ? Color.green : UnityEditor.Handles.preselectionColor);
            fillColor.a *= alpha;

            if (selected && PWBCore.staticData.selectedControlPointBlink)
            {
                fillColor.a *= _blinkingValue;
                if (_blinkingValue >= 1) _blinkingDelta = -Mathf.Abs(_blinkingDelta);
                else if (_blinkingValue <= 0) _blinkingDelta = Mathf.Abs(_blinkingDelta);
                _blinkingValue += _blinkingDelta;
            }

            UnityEditor.Handles.color = fillColor;
            UnityEditor.Handles.DrawSolidDisc(point, normal, radius - sizeDelta);
        }

        private static bool _updateHandlePosition = false;
        private static Vector3 _handlePosition;

        public static void UpdateHandlePosition()
        {
            _updateHandlePosition = true;
            if (tool == ToolController.Tool.TILING && tilingData != null) ApplyTilingHandlePosition(tilingData);
            BrushstrokeManager.UpdateBrushstroke(false);
        }

        public static Vector3 handlePosition { get => _handlePosition; set => _handlePosition = value; }

        private static bool _updateHandleRotation = false;
        private static Quaternion _handleRotation;

        public static void UpdateHandleRotation()
        {
            _updateHandleRotation = true;
            BrushstrokeManager.UpdateBrushstroke(false);
        }

        public static Quaternion handleRotation { get => _handleRotation; set => _handleRotation = value; }

    }
}
#pragma warning restore UDR0001
