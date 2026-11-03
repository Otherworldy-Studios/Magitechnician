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
    [System.Serializable]
    public class GravityToolSettings : BrushToolBase
    {
        [SerializeField] private SimulateGravityData _simData = new SimulateGravityData();
        [SerializeField] private float _height = 10f;
        
        public enum TempCollidersAction
        {
            NEVER_CREATE,
            CREATE_ALL,
            CREATE_WITHIN_FRUSTRUM
        }
        [SerializeField] private TempCollidersAction _tempCollidersAction = TempCollidersAction.CREATE_WITHIN_FRUSTRUM;
        
        public SimulateGravityData simData => _simData;
        public float height
        {
            get => _height;
            set
            {
                value = Mathf.Max(value, 0f);
                if (_height == value) return;
                _height = value;
            }
        }
        public TempCollidersAction tempCollidersAction
        {
            get => _tempCollidersAction;
            set
            {
                if (_tempCollidersAction == value) return;
                _tempCollidersAction = value;
                DataChanged();
            }
        }

        public bool createTempColliders => _tempCollidersAction != TempCollidersAction.NEVER_CREATE;

        public override void Copy(IToolSettings other)
        {
            var otherGravityToolSettings = other as GravityToolSettings;
            if (otherGravityToolSettings == null) return;
            base.Copy(other);
            _simData.Copy(otherGravityToolSettings._simData);
            _height = otherGravityToolSettings._height;
            _tempCollidersAction = otherGravityToolSettings._tempCollidersAction;
        }

        public GravityToolSettings Clone()
        {
            var clone = new GravityToolSettings();
            clone.Copy(this);
            return clone;
        }

        public GravityToolSettings() : base() => _brushShape = BrushShape.POINT;
    }

    [System.Serializable]
    public class GravityToolController : ToolControllerBase<GravityToolSettings, GravityToolController>
    {

        private static float _surfaceDistanceSensitivityStatic = 1.0f;
        [SerializeField] private float _surfaceDistanceSensitivity = _surfaceDistanceSensitivityStatic;
        public static float surfaceDistanceSensitivity
        {
            get => _surfaceDistanceSensitivityStatic;
            set
            {
                value = Mathf.Clamp(value, 0f, 1f);
                if (_surfaceDistanceSensitivityStatic == value) return;
                _surfaceDistanceSensitivityStatic = value;
                PWBCore.staticData.Save();
            }
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            _surfaceDistanceSensitivity = _surfaceDistanceSensitivityStatic;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            _surfaceDistanceSensitivityStatic = _surfaceDistanceSensitivity;
        }
    }
}
#pragma warning restore UDR0001
