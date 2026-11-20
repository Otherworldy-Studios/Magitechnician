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
    #region ITEM
    public struct BrushstrokeObject : System.IEquatable<BrushstrokeObject>
    {
        public readonly int objIdx;
        public readonly Vector3 objPosition;
        public readonly Quaternion objRotation;
        public readonly Vector3 additionalAngle;
        public readonly Vector3 objScale;
        public readonly bool flipX;
        public readonly bool flipY;
        public readonly float surfaceDistance;
        public readonly Vector3 brushstrokeDirection;

        public BrushstrokeObject(int objIdx, Vector3 objPosition, Quaternion objRotation, Vector3 additionalAngle,
            Vector3 objScale, bool flipX, bool flipY, float surfaceDistance, Vector3 brushstrokeDirection)
        {
            this.objIdx = objIdx;
            this.objPosition = objPosition;
            this.objRotation = objRotation;
            this.additionalAngle = additionalAngle;
            this.objScale = objScale;
            this.flipX = flipX;
            this.flipY = flipY;
            this.surfaceDistance = surfaceDistance;
            this.brushstrokeDirection = brushstrokeDirection;
        }

        public BrushstrokeObject Clone()
        {
            var clone = new BrushstrokeObject(objIdx, objPosition, objRotation, additionalAngle,
                objScale, flipX, flipY, surfaceDistance, brushstrokeDirection);
            return clone;
        }

        public bool Equals(BrushstrokeObject other)
        {
            return objPosition == other.objPosition && objRotation == other.objRotation
                && additionalAngle == other.additionalAngle && objScale == other.objScale
                && flipX == other.flipX && flipY == other.flipY && surfaceDistance == other.surfaceDistance
                && brushstrokeDirection == other.brushstrokeDirection;
        }
        public static bool operator ==(BrushstrokeObject lhs, BrushstrokeObject rhs) => lhs.Equals(rhs);
        public static bool operator !=(BrushstrokeObject lhs, BrushstrokeObject rhs) => !lhs.Equals(rhs);

        public override bool Equals(object obj) => obj is BrushstrokeObject other && Equals(other);

        public override int GetHashCode()
        {
            int hashCode = 861157388;
            hashCode = hashCode * -1521134295 + objIdx.GetHashCode();
            hashCode = hashCode * -1521134295 + objPosition.GetHashCode();
            hashCode = hashCode * -1521134295 + objRotation.GetHashCode();
            hashCode = hashCode * -1521134295 + additionalAngle.GetHashCode();
            hashCode = hashCode * -1521134295 + objScale.GetHashCode();
            hashCode = hashCode * -1521134295 + flipX.GetHashCode();
            hashCode = hashCode * -1521134295 + flipY.GetHashCode();
            hashCode = hashCode * -1521134295 + surfaceDistance.GetHashCode();
            hashCode = hashCode * -1521134295 + brushstrokeDirection.GetHashCode();
            return hashCode;
        }
    }

    public struct BrushstrokeItem : System.IEquatable<BrushstrokeItem>
    {
        public readonly MultibrushItemSettings settings;
        public Vector3 tangentPosition;
        public readonly Vector3 additionalAngle;
        public readonly Vector3 scaleMultiplier;
        public Vector3 nextTangentPosition;
        public readonly bool flipX;
        public readonly bool flipY;
        public readonly float surfaceDistance;
        public readonly int index;
        public readonly int tokenIndex;

        public BrushstrokeItem(int index, int tokenIndex, MultibrushItemSettings settings, Vector3 tangentPosition,
            Vector3 additionalAngle, Vector3 scaleMultiplier, bool flipX, bool flipY, float surfaceDistance)
        {
            this.settings = settings;
            this.tangentPosition = tangentPosition;
            this.additionalAngle = additionalAngle;
            this.scaleMultiplier = scaleMultiplier;
            nextTangentPosition = tangentPosition;
            this.flipX = flipX;
            this.flipY = flipY;
            this.surfaceDistance = surfaceDistance;
            this.index = index;
            this.tokenIndex = tokenIndex;
        }
        public BrushstrokeItem(Vector3 tangentPosition)
        {
            this.tangentPosition = tangentPosition;

            this.settings = null;
            this.additionalAngle = Vector3.zero;
            this.scaleMultiplier = Vector3.one;
            nextTangentPosition = tangentPosition;
            this.flipX = false;
            this.flipY = false;
            this.surfaceDistance = 0;
            this.index = 0;
            this.tokenIndex = 0;
        }

        public BrushstrokeItem Clone()
        {
            var clone = new BrushstrokeItem(index, tokenIndex, settings, tangentPosition, additionalAngle,
                scaleMultiplier, flipX, flipY, surfaceDistance);
            clone.nextTangentPosition = nextTangentPosition;
            return clone;
        }

        public bool Equals(BrushstrokeItem other)
        {
            return settings == other.settings && tangentPosition == other.tangentPosition
                && additionalAngle == other.additionalAngle && scaleMultiplier == other.scaleMultiplier
                && nextTangentPosition == other.nextTangentPosition;
        }
        public static bool operator ==(BrushstrokeItem lhs, BrushstrokeItem rhs) => lhs.Equals(rhs);
        public static bool operator !=(BrushstrokeItem lhs, BrushstrokeItem rhs) => !lhs.Equals(rhs);

        public override bool Equals(object obj) => obj is BrushstrokeItem other && Equals(other);

        public override int GetHashCode()
        {
            int hashCode = 861157388;
            hashCode = hashCode * -1521134295
                + System.Collections.Generic.EqualityComparer<MultibrushItemSettings>.Default.GetHashCode(settings);
            hashCode = hashCode * -1521134295 + tangentPosition.GetHashCode();
            hashCode = hashCode * -1521134295 + additionalAngle.GetHashCode();
            hashCode = hashCode * -1521134295 + scaleMultiplier.GetHashCode();
            hashCode = hashCode * -1521134295 + nextTangentPosition.GetHashCode();
            hashCode = hashCode * -1521134295 + flipX.GetHashCode();
            hashCode = hashCode * -1521134295 + flipY.GetHashCode();
            hashCode = hashCode * -1521134295 + surfaceDistance.GetHashCode();
            return hashCode;
        }
    }
    #endregion
    public static partial class BrushstrokeManager
    {

        private static System.Collections.Generic.List<BrushstrokeItem> _brushstroke
            = new System.Collections.Generic.List<BrushstrokeItem>();


        public static BrushstrokeItem[] brushstroke => _brushstroke.ToArray();
        public static int itemCount => _brushstroke.Count;
        private static int _triangleCount = 0;
        public static int triangleCount => _triangleCount;
        public static void ClearBrushstroke() => _brushstroke.Clear();

        public static BrushstrokeItem[] brushstrokeClone
        {
            get
            {
                var clone = new BrushstrokeItem[_brushstroke.Count];
                for (int i = 0; i < clone.Length; ++i) clone[i] = _brushstroke[i].Clone();
                return clone;
            }
        }

        public static bool BrushstrokeEqual(BrushstrokeItem[] lhs, BrushstrokeItem[] rhs)
        {
            if (lhs.Length != rhs.Length) return false;
            for (int i = 0; i < lhs.Length; ++i)
                if (lhs[i] != rhs[i]) return false;
            return true;
        }
        private static void AddBrushstrokeItem(int index, int tokenIndex, Vector3 tangentPosition,
            Vector3 angle, Vector3 scale, IPaintToolSettings paintToolSettings)
        {
            if (index < 0 || index >= PaletteManager.selectedBrush.itemCount) return;
            var multiBrushSettings = PaletteManager.selectedBrush;
            BrushSettings brushSettings = multiBrushSettings.items[index];
            if (paintToolSettings != null && paintToolSettings.overwriteBrushProperties)
                brushSettings = paintToolSettings.brushSettings;

            var additonalAngle = (Quaternion.Euler(angle) * Quaternion.Euler(brushSettings.GetAdditionalAngle())).eulerAngles;
            var flipX = brushSettings.GetFlipX();
            var flipY = brushSettings.GetFlipY();
            var surfaceDistance = brushSettings.GetSurfaceDistance();
            var brushItem = PaletteManager.selectedBrush.items[index];
            var strokeItem = new BrushstrokeItem(index, tokenIndex,
                brushItem, tangentPosition, additonalAngle,
                scale, flipX, flipY, surfaceDistance);
            if (_brushstroke.Count == 0) _triangleCount = 0;
            _triangleCount += brushItem.trianglesCount;
            if (_brushstroke.Count > 0)
            {
                var last = _brushstroke.Last();
                last.nextTangentPosition = tangentPosition;
                _brushstroke[_brushstroke.Count - 1] = last;
            }
            _brushstroke.Add(strokeItem);
            ToolProperties.RepainWindow();
        }

        private static Vector3 ScaleMultiplier(int itemIdx, IPaintToolSettings settings)
        {
            if (settings.overwriteBrushProperties) return settings.brushSettings.GetScaleMultiplier();
            if (PaletteManager.selectedBrush != null)
            {
                var nextItem = PaletteManager.selectedBrush.items[itemIdx];
                return nextItem.GetScaleMultiplier();
            }
            return Vector3.one;
        }
        public static void UpdateBrushstroke(bool brushChange = false)
        {
            if (ToolController.current == ToolController.Tool.SELECTION) return;
            if (ToolController.current == ToolController.Tool.LINE
                || ToolController.current == ToolController.Tool.SHAPE
                || ToolController.current == ToolController.Tool.TILING)
            {
                PWBIO.UpdateStroke();
                return;
            }
            if (!brushChange && ToolController.current == ToolController.Tool.PIN && PinManager.settings.repeat) return;
            _brushstroke.Clear();
            if (PaletteManager.selectedBrush == null) return;
            if (ToolController.current == ToolController.Tool.BRUSH) UpdateBrushBaseStroke(BrushManager.settings);
            else if (ToolController.current == ToolController.Tool.GRAVITY)
                UpdateBrushBaseStroke(GravityToolController.settings);
            else if (ToolController.current == ToolController.Tool.PIN) UpdateSingleBrushstroke(PinManager.settings);
            else if (ToolController.current == ToolController.Tool.REPLACER) _brushstroke.Clear();
            else if (ToolController.current == ToolController.Tool.FLOOR) UpdateFloorBrushstroke(setNextIdx: false);
            else if (ToolController.current == ToolController.Tool.WALL)
                UpdateWallBrushstroke(WallManager.wallLenghtAxis, cellsCount: 1, setNextIdx: false, deleteMode: false);
        }

    }
}
#pragma warning restore UDR0001
