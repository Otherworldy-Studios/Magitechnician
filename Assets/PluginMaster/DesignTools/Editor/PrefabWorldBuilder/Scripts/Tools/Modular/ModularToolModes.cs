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
    public class ModularToolModes
    {

        #region CHANGE NOTIFY
        public static event System.Action OnBlockToolModeChanged;
        public static event System.Action OnFloorToolModeChanged;
        protected static void NotifyChange()
        {
            if(ToolController.current == ToolController.Tool.BLOCK)
                OnBlockToolModeChanged?.Invoke();
            else if(ToolController.current == ToolController.Tool.FLOOR)
                OnFloorToolModeChanged?.Invoke();
        }
        #endregion

        #region EDIT MODES
        public enum EditMode
        {
            ATTACH,
            ERASE,
            MOVE,
            SELECT,
            REPLACE,
            PICK
        }

        private static EditMode _selectedEditMode = EditMode.ATTACH;

        public static EditMode selectedEditMode
        {
            get => _selectedEditMode;
            set
            {
                if (_selectedEditMode == value) return;
                _selectedEditMode = value;
                NotifyChange();
            }
        }

        public static void ResetEditMode()
        {
            _selectedEditMode = EditMode.ATTACH;
        }
        #endregion

        #region MIRROR MODES
        public static bool mirrorX { get; set; } = false;
        public static bool mirrorY { get; set; } = false;
        public static bool mirrorZ { get; set; } = false;

        public static void ResetMirrorModes()
        {
            mirrorX = false;
            mirrorY = false;
            mirrorZ = false;
        }

        public static bool reflectRotation { get; set; } = false;
        public static bool autoReflectRotation { get; set; } = true;
        public static bool reflectRotationX { get; set; } = false;
        public static bool reflectRotationY { get; set; } = false;
        public static bool reflectRotationZ { get; set; } = false;
        public static void ResetReflectRotation()
        {
            reflectRotationX = false;
            reflectRotationY = false;
            reflectRotationZ = false;
        }

        public static bool reflectScale { get; set; } = false;
        #endregion

        #region SYMMETRY ORIGIN
        private static Vector3 _symmetryOrigin = Vector3.zero;

        public static Vector3 symmetryOrigin
        {
            get => _symmetryOrigin;
            set
            {
                if (_symmetryOrigin == value) return;
                _symmetryOrigin = value;
            }
        }
        #endregion

    }
    public struct MirroredTransform
    {
        public Vector3 position;
        public Quaternion rotationOffset;
        public Vector3 scaleMultiplier;

        public MirroredTransform(Vector3 position, Quaternion rotationOffset, Vector3 scaleMultiplier)
        {
            this.position = position;
            this.rotationOffset = rotationOffset;
            this.scaleMultiplier = scaleMultiplier;
        }
    }
}
#pragma warning restore UDR0001
