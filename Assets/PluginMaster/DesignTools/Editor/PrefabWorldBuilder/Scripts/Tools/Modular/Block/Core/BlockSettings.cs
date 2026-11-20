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
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    #region BLOCK CELL SIZE
    [System.Serializable]
    public struct BlockCellSize
    {
        [SerializeField] private string _name;
        [SerializeField] private Vector3 _size;
        public BlockCellSize(string name, Vector3 size)
        {
            _name = name;
            _size = size;
        }
        public string name { get => _name; set => _name = value; }
        public Vector3 size { get => _size; set => _size = value; }
    }
    #endregion

    #region BLOCK SETTINGS
    [System.Serializable]
    public class BlockSettings : ModularToolBase { }
    #endregion

    #region BLOCK TOOL MODES
    public class BlockToolModes : ModularToolModes
    {

        #region DRAW MODES
        public enum DrawMode
        {
            BLOCK_BY_BLOCK,
            LINE,
            FACE,
            BOX
        }
        private static DrawMode _selectedDrawMode = DrawMode.BLOCK_BY_BLOCK;
        public static DrawMode selectedDrawMode
        {
            get => _selectedDrawMode;
            set
            {
                if (_selectedDrawMode == value) return;
                _selectedDrawMode = value;
                ResetDrawModeState();
                NotifyChange();
            }
        }
        public static void ResetDrawModeState()
        {
            lineState = LineState.FIRST_POINT;
            blockBoxFirstPointSet = false;
        }
        

        #region BLOCK_BY_BLOCK
        private static int _brushSize = 1;
        public static int brushSize
        {
            get => _brushSize;
            set
            {
                value = Mathf.Clamp(value, 1, 64);
                if (value == _brushSize) return;
                _brushSize = value;
            }
        }
        public enum BrushShape
        {
            SQUARE,
            CIRCLE,
            CUBE,
            SPHERE
        }
        public static BrushShape selectedBrushShape { get; set; } = BrushShape.SQUARE;
        #endregion

        #region LINE
        public enum ProjectionAxis
        {
            NONE,
            CAMERA,
            DOWN,
            UP,
            BACK,
            FORWARD,
            LEFT,
            RIGHT
        }
        public static ProjectionAxis projectionAxis { get; set; } = ProjectionAxis.NONE;
        public enum LineState
        {
            FIRST_POINT,
            SECOND_POINT
        }
        public static LineState lineState { get; set; } = LineState.FIRST_POINT;
        public static Vector3 lineFirstPoint { get; set; } = Vector3.zero;
        public static Vector3 lineSecondPoint { get; set; } = Vector3.zero;
        #endregion

        #region FACE
        public enum  FaceConectivity
        {
            PREFAB,
            GEOMETRY
        }
        public static FaceConectivity faceConectivity { get; set; } = FaceConectivity.GEOMETRY;
        public enum FaceNeighborSearchingDirections
        {
            FOUR_DIRECTIONS,
            EIGHT_DIRECTIONS
        }
        public static FaceNeighborSearchingDirections faceNeighborSearchingDirections
            { get; set; } = FaceNeighborSearchingDirections.FOUR_DIRECTIONS;
        public enum FaceNormalDirection
        {
            UP,
            DOWN,
            LEFT,
            RIGHT,
            FORWARD,
            BACK
        }
        public static FaceNormalDirection faceNormalDirection { get; set; } = FaceNormalDirection.UP;
        public static Vector3 faceTargetCellCenter { get; set; } = Vector3.zero;
        #endregion
        
        #region BOX
        public static Vector3 boxFirstPoint { get; set; } = Vector3.zero;
        public static Vector3 boxSecondPoint { get; set; } = Vector3.zero;
        public static bool blockBoxFirstPointSet { get; set; } = false;

        #endregion

        #endregion

        #region MOVE MODE
        public enum MoveSelectionMode
        {
            CURRENT,
            FACE,
            BOX
        }
        public static MoveSelectionMode moveSelectionMode { get; set; } = MoveSelectionMode.CURRENT;
        public enum MoveNeighborSearchingDirections
        {
            FOUR_DIRECTIONS,
            EIGHT_DIRECTIONS
        }
        public static MoveNeighborSearchingDirections moveNeighborSearchingDirections
            { get; set; } = MoveNeighborSearchingDirections.FOUR_DIRECTIONS;
        public enum MoveConectivity
        {
            PREFAB,
            GEOMETRY
        }
        public static MoveConectivity moveConectivity { get; set; } = MoveConectivity.GEOMETRY;
        public enum MoveNormalDirection
        {
            UP,
            DOWN,
            LEFT,
            RIGHT,
            FORWARD,
            BACK
        }
        public static MoveNormalDirection moveNormalDirection { get; set; } = MoveNormalDirection.UP;
        #endregion

        #region SELECT MODE
        public enum  SelecMode
        {
            RECT,
            BRUSH,
            REGION
        }
        private static SelecMode _selectMode = SelecMode.RECT;
        public static SelecMode selectMode
        {
            get => _selectMode;
            set
            {
                if (_selectMode == value) return;
                _selectMode = value;
                NotifyChange();
            }
        }
        public enum RegionConectivity
        {
            PREFAB,
            GEOMETRY
        }
        public static RegionConectivity regionConectivity { get; set; } = RegionConectivity.GEOMETRY;

        public enum RegionSelectNeighborSearchingDirections
        {
            FOUR_DIRECTIONS,
            EIGHT_DIRECTIONS
        }
        public static RegionSelectNeighborSearchingDirections regionSelectNeighborSearchingDirections
        { get; set; } = RegionSelectNeighborSearchingDirections.FOUR_DIRECTIONS;
        #endregion

        #region REPLACE MODE
        public enum  ReplaceMode
        {
            SINGLE,
            REGION
        }
        public static ReplaceMode replaceMode { get; set; } = ReplaceMode.SINGLE;
        public enum ReplaceSelectionMode
        {
            CURRENT,
            FACE,
            BOX
        }
        public static ReplaceSelectionMode replaceSelectionMode { get; set; } = ReplaceSelectionMode.BOX;
        public enum ReplaceRegionConectivity
        {
            PREFAB,
            GEOMETRY
        }
        public static ReplaceRegionConectivity replaceRegionConectivity { get; set; } = ReplaceRegionConectivity.PREFAB;
        public enum ReplaceRegionSelectNeighborSearchingDirections
        {
            FOUR_DIRECTIONS,
            EIGHT_DIRECTIONS
        }
        public static ReplaceRegionSelectNeighborSearchingDirections replaceRegionSelectNeighborSearchingDirections
        { get; set; } = ReplaceRegionSelectNeighborSearchingDirections.FOUR_DIRECTIONS;
        #endregion

        #region AXIS MODES
        public static bool axisX { get; set; } = false;
        public static bool axisY { get; set; } = false;
        public static bool axisZ { get; set; } = false;
        public static void ResetAxisModes()
        {
            axisX = false;
            axisY = false;
            axisZ = false;
        }
        #endregion

    }
    #endregion
}
#pragma warning restore UDR0001
