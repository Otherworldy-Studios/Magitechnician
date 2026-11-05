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
    public class MirrorSettings : SelectionToolBase,
        IPaintToolSettings, IToolParentingSettings, ISerializationCallbackReceiver
    {
        public enum MirrorAction { TRANSFORM, CREATE}
        [SerializeField] private bool _reflectRotation = true;
        [SerializeField] private MirrorAction _action = MirrorAction.CREATE;
        [SerializeField] private bool _sameParentAsSource = true;
        [SerializeField] private Pose _mirrorPose = new Pose(Vector3.zero, Quaternion.LookRotation(Vector3.right, Vector3.up));
        [SerializeField] private bool _invertScale = false;

        private const string COMMAND_NAME = "Edit Mirror";
        public bool reflectRotation
        {
            get => _reflectRotation;
            set
            {
                if (_reflectRotation == value) return;
                _reflectRotation = value;
                DataChanged();
            }
        }

        public MirrorAction action
        {
            get => _action;
            set
            {
                if (_action == value) return;
                _action = value;
                DataChanged();
            }
        }

        public bool sameParentAsSource
        {
            get => _sameParentAsSource;
            set
            {
                if (_sameParentAsSource == value) return;
                _sameParentAsSource = value;
                DataChanged();
            }
        }
        public Pose mirrorPose => _mirrorPose;

        public Vector3 mirrorPosition
        {
            get => _mirrorPose.position;
            set
            {
                if (_mirrorPose.position == value) return;
                ToolProperties.RegisterUndo(COMMAND_NAME);
                _mirrorPose.position = value;
                DataChanged();
            }
        }
        public Quaternion mirrorRotation
        {
            get => _mirrorPose.rotation;
            set
            {
                if (_mirrorPose.rotation == value) return;
                ToolProperties.RegisterUndo(COMMAND_NAME);
                _mirrorPose.rotation = value;
                DataChanged();
            }
        }
        public bool invertScale
        {
            get => _invertScale;
            set
            {
                if (_invertScale == value) return;
                _invertScale = value;
                DataChanged();
            }
        }

        public override void Copy(IToolSettings other)
        {
            var otherMirrorSettings = other as MirrorSettings;
            if (otherMirrorSettings == null) return;
            base.Copy(other);
            _paintTool.Copy(otherMirrorSettings._paintTool);
            _reflectRotation = otherMirrorSettings.reflectRotation;
            _action = otherMirrorSettings._action;
            _sameParentAsSource = otherMirrorSettings._sameParentAsSource;
            _mirrorPose = otherMirrorSettings._mirrorPose;
            _invertScale = otherMirrorSettings._invertScale;
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() => PWBIO.repaint = true;

        #region PAINT TOOL
        [SerializeField] private PaintToolSettings _paintTool = new PaintToolSettings();
        public Transform parent { get => _paintTool.parent; set => _paintTool.parent = value; }
        public bool overwritePrefabLayer { get => _paintTool.overwritePrefabLayer;
            set => _paintTool.overwritePrefabLayer = value; }
        public int layer { get => _paintTool.layer; set => _paintTool.layer = value; }
        public bool autoCreateParent { get => _paintTool.autoCreateParent; set => _paintTool.autoCreateParent = value; }
        public bool setSurfaceAsParent { get => _paintTool.setSurfaceAsParent; set => _paintTool.setSurfaceAsParent = value; }
        public bool setLastSelectedAsParent
        {
            get => _paintTool.setLastSelectedAsParent;
            set => _paintTool.setLastSelectedAsParent = value;
        }
        public bool createSubparentPerPalette
        {
            get => _paintTool.createSubparentPerPalette;
            set => _paintTool.createSubparentPerPalette = value;
        }
        public bool createSubparentPerTool
        {
            get => _paintTool.createSubparentPerTool;
            set => _paintTool.createSubparentPerTool = value;
        }
        public bool createSubparentPerBrush
        {
            get => _paintTool.createSubparentPerBrush;
            set => _paintTool.createSubparentPerBrush = value;
        }
        public bool createSubparentPerPrefab
        {
            get => _paintTool.createSubparentPerPrefab;
            set => _paintTool.createSubparentPerPrefab = value;
        }
        public bool overwriteBrushProperties {get => _paintTool.overwriteBrushProperties;
            set => _paintTool.overwriteBrushProperties = value; }
        public BrushSettings brushSettings => _paintTool.brushSettings;
        public bool overwriteParentingSettings
        {
            get => _paintTool.overwriteParentingSettings;
            set => _paintTool.overwriteParentingSettings = value;
        }
        public IToolParentingSettings GetParentingSettings() => _paintTool as ToolParentingSettings;
        #endregion
    }

    [System.Serializable]
    public class MirrorManager : ToolControllerBase<MirrorSettings, MirrorManager> { }
}
#pragma warning restore UDR0001
