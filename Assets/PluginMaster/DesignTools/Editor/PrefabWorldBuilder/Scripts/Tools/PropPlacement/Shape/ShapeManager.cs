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
    public class ShapeSettings : LineSettings
    {
        public enum ShapeType { CIRCLE, POLYGON }
        [SerializeField] private ShapeType _shapeType = ShapeType.POLYGON;
        [SerializeField] private int _sidesCount = 5;
        public enum NormalDirection
        {
            X, NEGA_X,
            Y, NEGA_Y,
            Z, NEGA_Z,
            SURFACE_NORMAL
        }
        [SerializeField] private NormalDirection _initialPlaneNormalDirection = NormalDirection.SURFACE_NORMAL;

        public enum ShapeProjectionDirection
        {
            AXIS,
            PLANE_NORMAL,
            FROM_CENTER,
            TO_CENTER
        }
        [SerializeField] private ShapeProjectionDirection _projectionDirectionType = ShapeProjectionDirection.AXIS;

        public ShapeType shapeType
        {
            get => _shapeType;
            set
            {
                if (_shapeType == value) return;
                _shapeType = value;
                OnDataChanged();
            }
        }
        public int sidesCount
        {
            get => _sidesCount;
            set
            {
                value = Mathf.Max(value, 3);
                if (_sidesCount == value) return;
                _sidesCount = value;
                OnDataChanged();
            }
        }
        public override bool objectsOrientedAlongTheLine
        {
            get
            {
                if (!_objectsOrientedAlongTheLine && (_projectionDirectionType == ShapeProjectionDirection.FROM_CENTER
                    || _projectionDirectionType == ShapeProjectionDirection.TO_CENTER))
                    _objectsOrientedAlongTheLine = true;
                return base.objectsOrientedAlongTheLine;
            }
            set => base.objectsOrientedAlongTheLine = value;
        }
        public Vector3 initialPlaneNormal
        {
            get
            {
                switch (initialPlaneNormalDirection)
                {
                    case NormalDirection.X: return Vector3.right;
                    case NormalDirection.NEGA_X: return Vector3.left;
                    case NormalDirection.Y: return Vector3.up;
                    case NormalDirection.NEGA_Y: return Vector3.down;
                    case NormalDirection.Z: return Vector3.forward;
                    case NormalDirection.NEGA_Z: return Vector3.back;
                    default: return Vector3.up;
                }
            }
        }
        public ShapeProjectionDirection projectionDirectionType
        {
            get => _projectionDirectionType;
            set
            {
                if (_projectionDirectionType == value) return;
                _projectionDirectionType = value;
                if (_projectionDirectionType == ShapeProjectionDirection.FROM_CENTER
                    || _projectionDirectionType == ShapeProjectionDirection.TO_CENTER)
                    _objectsOrientedAlongTheLine = true;
                OnDataChanged();
            }
        }

        public NormalDirection initialPlaneNormalDirection
        {
            get => _initialPlaneNormalDirection;
            set
            {
                if (_initialPlaneNormalDirection == value) return;
                _initialPlaneNormalDirection = value;
                OnDataChanged();
            }
        }

        public override void Copy(IToolSettings other)
        {
            base.Copy(other);
            var otherShapeSettings = other as ShapeSettings;
            if (otherShapeSettings == null) return;
            _shapeType = otherShapeSettings._shapeType;
            _sidesCount = otherShapeSettings._sidesCount;
            initialPlaneNormalDirection = otherShapeSettings.initialPlaneNormalDirection;
            _projectionDirectionType = otherShapeSettings._projectionDirectionType;
        }
        public override void DataChanged()
        {
            base.DataChanged();
            if (!ToolController.editMode) ShapeData.instance.Update(true);
            PWBIO.OnShapeSettingsChanged();
        }
    }

    public class ShapeToolName : IToolName { public string value => "Shape"; }

    [System.Serializable]
    public class ShapeSceneData : SceneData<ShapeToolName, ShapeSettings, ControlPoint, ShapeData>
    {
        public ShapeSceneData() : base() { }
        public ShapeSceneData(string sceneGUID) : base(sceneGUID) { }
    }

    [System.Serializable]
    public class ShapeManager : PersistentToolControllerBase<ShapeToolName, ShapeSettings,
        ControlPoint, ShapeData, ShapeSceneData, ShapeManager>
    { }
}
#pragma warning restore UDR0001
