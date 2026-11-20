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
        #region MIRROR REFLECTION HELPERS
        private static Quaternion GetMirrorReflectionRotation(Vector3 localCellCenter, 
            bool isMirroredX, bool isMirroredY, bool isMirroredZ)
        {
            if (!ModularToolModes.reflectRotation) return Quaternion.identity;

            var euler = Vector3.zero;
            if (ModularToolModes.autoReflectRotation)
            {
                ModularToolModes.ResetReflectRotation();
                if (ModularToolModes.mirrorX || ModularToolModes.mirrorZ) ModularToolModes.reflectRotationY = true;
                if (ModularToolModes.mirrorY) ModularToolModes.reflectRotationX = true;
            }
            if (ModularToolModes.reflectRotationX) euler.x += 180f;
            if (ModularToolModes.reflectRotationY) euler.y += 180f;
            if (ModularToolModes.reflectRotationZ) euler.z += 180f;

            void RotateXZ()
            {
                if (isMirroredX && !isMirroredZ)
                {
                    if ((localCellCenter.x > 0f && localCellCenter.z > 0f)
                        || (localCellCenter.x < 0f && localCellCenter.z < 0f))
                    {
                        if (ModularToolModes.reflectRotationY) euler.y += 90f;
                    }
                    else
                    {
                        if (ModularToolModes.reflectRotationY) euler.y -= 90f;
                    }
                }
                else if (isMirroredZ && !isMirroredX)
                {
                    if ((localCellCenter.x > 0f && localCellCenter.z > 0f)
                        || (localCellCenter.x < 0f && localCellCenter.z < 0f))
                    {
                        if (ModularToolModes.reflectRotationY) euler.y -= 90f;
                    }
                    else
                    {
                        if (ModularToolModes.reflectRotationY) euler.y += 90f;
                    }
                }
            }
            void RotateXY()
            {
                if (isMirroredX && !isMirroredY)
                {
                    if ((localCellCenter.x > 0f && localCellCenter.y > 0f)
                        || (localCellCenter.x < 0f && localCellCenter.y < 0f))
                    {
                        if (ModularToolModes.reflectRotationY) euler.z += 90f;
                    }
                    else
                    {
                        if (ModularToolModes.reflectRotationY) euler.z -= 90f;
                    }
                }
                else if (isMirroredY && !isMirroredX)
                {
                    if ((localCellCenter.x > 0f && localCellCenter.y > 0f)
                        || (localCellCenter.x < 0f && localCellCenter.y < 0f))
                    {
                        if (ModularToolModes.reflectRotationY) euler.z -= 90f;
                    }
                    else
                    {
                        if (ModularToolModes.reflectRotationY) euler.z += 90f;
                    }
                }
            }

            void RotateYZ()
            {
                if (isMirroredY && !isMirroredZ)
                {
                    if ((localCellCenter.y > 0f && localCellCenter.z > 0f)
                        || (localCellCenter.y < 0f && localCellCenter.z < 0f))
                    {
                        if (ModularToolModes.reflectRotationY) euler.x += 90f;
                    }
                    else
                    {
                        if (ModularToolModes.reflectRotationY) euler.x -= 90f;
                    }
                }
                else if (isMirroredZ && !isMirroredY)
                {
                    if ((localCellCenter.y > 0f && localCellCenter.z > 0f)
                        || (localCellCenter.y < 0f && localCellCenter.z < 0f))
                    {
                        if (ModularToolModes.reflectRotationY) euler.x -= 90f;
                    }
                    else
                    {
                        if (ModularToolModes.reflectRotationY) euler.x += 90f;
                    }
                }
            }
            if (ModularToolModes.mirrorX && ModularToolModes.mirrorY && ModularToolModes.mirrorZ)
            {
                RotateXZ();
                RotateXY();
                RotateYZ(); 
            }
            else if (ModularToolModes.mirrorX && ModularToolModes.mirrorZ)
            {
                RotateXZ();
            }
            else if (ModularToolModes.mirrorX && ModularToolModes.mirrorY)
            {
                RotateXY();
            }
            else if (ModularToolModes.mirrorY && ModularToolModes.mirrorZ)
            {
                RotateYZ();
            }
            return Quaternion.Euler(euler);
        }

        private static Vector3 GetMirrorReflectionScale(bool isMirroredX, bool isMirroredY, bool isMirroredZ)
        {
            if (!BlockToolModes.reflectScale) return Vector3.one;
            return new Vector3(
                isMirroredX ? -1f : 1f,
                isMirroredY ? -1f : 1f,
                isMirroredZ ? -1f : 1f);
        }
        #endregion

        #region MIRRORED TRANSFORMS
        public static System.Collections.Generic.List<MirroredTransform> GetMirroredTransforms(Vector3 position)
        {
            return GetMirroredTransforms(position, Vector3.zero, Quaternion.identity);
        }

        public static System.Collections.Generic.List<MirroredTransform> GetMirroredTransforms(
            Vector3 baseCellCenter, Vector3 brushOffset, Quaternion itemRotation)
        {
            bool mx = ModularToolModes.mirrorX;
            bool my = ModularToolModes.mirrorY;
            bool mz = ModularToolModes.mirrorZ;

            var result = new System.Collections.Generic.List<MirroredTransform>();
            if (!mx && !my && !mz) return result;

            var origin = ModularToolModes.symmetryOrigin;
            var rotation = GridManager.settings.rotation;
            var invRotation = Quaternion.Inverse(rotation);

            var localBrushOffset = brushOffset;
            var localCenter = invRotation * (baseCellCenter - origin);
            
            var toolSettings = BlockManager.settings;

            if (BlockManager.quarterTurns > 0)
                localBrushOffset = Quaternion.AngleAxis(BlockManager.quarterTurns * 90,
                    toolSettings.upwardAxis) * localBrushOffset;
           
            if (mx)
            {
                var localRotOffset = GetMirrorReflectionRotation(localCenter, 
                    isMirroredX: true, isMirroredY: false, isMirroredZ: false);
                var mirroredOffset = toolSettings.subtractBrushOffset
                    ? localRotOffset * (localBrushOffset * 0.5f)
                    : Vector3.zero;

                var mirroredCenter = new Vector3(-localCenter.x, localCenter.y, localCenter.z);
                var mirroredPosition = rotation * (mirroredCenter + mirroredOffset) + origin;
                result.Add(new MirroredTransform(
                    mirroredPosition,
                    rotation * localRotOffset * invRotation,
                    GetMirrorReflectionScale(true, false, false)));
            }
            if (my)
            {
                var localRotOffset = GetMirrorReflectionRotation(localCenter,
                    isMirroredX: false, isMirroredY: true, isMirroredZ: false);
                var mirroredOffset = toolSettings.subtractBrushOffset
                     ? localRotOffset * (localBrushOffset * 0.5f)
                     : Vector3.zero;

                var mirroredCenter = new Vector3(localCenter.x, -localCenter.y, localCenter.z);
                result.Add(new MirroredTransform(
                    rotation * (mirroredCenter + mirroredOffset) + origin,
                    rotation * localRotOffset * invRotation,
                    GetMirrorReflectionScale(false, true, false)));
            }
            if (mz)
            {
                var localRotOffset = GetMirrorReflectionRotation(localCenter,
                    isMirroredX: false, isMirroredY: false, isMirroredZ: true);
                var mirroredOffset = toolSettings.subtractBrushOffset
                     ? localRotOffset * (localBrushOffset * 0.5f)
                     : Vector3.zero;

                var mirroredCenter = new Vector3(localCenter.x, localCenter.y, -localCenter.z);
                result.Add(new MirroredTransform(
                    rotation * (mirroredCenter + mirroredOffset) + origin,
                    rotation * localRotOffset * invRotation,
                    GetMirrorReflectionScale(false, false, true)));
            }
            if (mx && my)
            {
                var localRotOffset = GetMirrorReflectionRotation(localCenter,
                    isMirroredX: true, isMirroredY: true, isMirroredZ: false);
                var mirroredOffset = toolSettings.subtractBrushOffset
                    ? localRotOffset * (localBrushOffset * 0.5f)
                    : Vector3.zero;

                var mirroredCenter = new Vector3(-localCenter.x, -localCenter.y, localCenter.z);
                result.Add(new MirroredTransform(
                    rotation * (mirroredCenter + mirroredOffset) + origin,
                    rotation * localRotOffset * invRotation,
                    GetMirrorReflectionScale(true, true, false)));
            }
            if (mx && mz)
            {
                var localRotOffset = GetMirrorReflectionRotation(localCenter,
                    isMirroredX: true, isMirroredY: false, isMirroredZ: true);
                var mirroredOffset = toolSettings.subtractBrushOffset
                    ? localRotOffset * (localBrushOffset * 0.5f)
                    : Vector3.zero;

                var mirroredCenter = new Vector3(-localCenter.x, localCenter.y, -localCenter.z);
                result.Add(new MirroredTransform(
                    rotation * (mirroredCenter + mirroredOffset) + origin,
                    rotation * localRotOffset * invRotation,
                    GetMirrorReflectionScale(true, false, true)));
            }
            if (my && mz)
            {
                var localRotOffset = GetMirrorReflectionRotation(localCenter,
                    isMirroredX: false, isMirroredY: true, isMirroredZ: true);
                var mirroredOffset = toolSettings.subtractBrushOffset
                    ? localRotOffset * (localBrushOffset * 0.5f)
                    : Vector3.zero;

                var mirroredCenter = new Vector3(localCenter.x, -localCenter.y, -localCenter.z);
                result.Add(new MirroredTransform(
                    rotation * (mirroredCenter + mirroredOffset) + origin,
                    rotation * localRotOffset * invRotation,
                    GetMirrorReflectionScale(false, true, true)));
            }
            if (mx && my && mz)
            {
                var localRotOffset = GetMirrorReflectionRotation(localCenter,
                    isMirroredX: true, isMirroredY: true, isMirroredZ: true);
                var mirroredOffset = toolSettings.subtractBrushOffset
                    ? localRotOffset * (localBrushOffset * 0.5f)
                    : Vector3.zero;

                var mirroredCenter = new Vector3(-localCenter.x, -localCenter.y, -localCenter.z);
                result.Add(new MirroredTransform(
                    rotation * (mirroredCenter + mirroredOffset) + origin,
                    rotation * localRotOffset * invRotation,
                    GetMirrorReflectionScale(true, true, true)));
            }

            return result;
        }
        public static System.Collections.Generic.List<Vector3> GetMirroredPositions(Vector3 position)
        {
            return GetMirroredTransforms(position).ConvertAll(t => t.position);
        }
        #endregion

        #region MIRROR AND AXIS COMBINED
        public static System.Collections.Generic.List<Vector3> GetAxisPositions(Vector3 position)
        {
            var result = new System.Collections.Generic.List<Vector3>();
            if (BlockToolModes.selectedDrawMode != BlockToolModes.DrawMode.BLOCK_BY_BLOCK) return result;
            var origin = ModularToolModes.symmetryOrigin;
            var rotation = GridManager.settings.rotation;
            var stepSize = GridManager.settings.step;

            var localPos = Quaternion.Inverse(rotation) * (position - origin);

            bool ax = BlockToolModes.axisX;
            bool ay = BlockToolModes.axisY;
            bool az = BlockToolModes.axisZ;

            if (!ax && !ay && !az) return result;

            var endLocalPos = new Vector3(
                ax ? -localPos.x : localPos.x,
                ay ? -localPos.y : localPos.y,
                az ? -localPos.z : localPos.z
            );

            int stepsX = ax ? Mathf.Abs(Mathf.RoundToInt((endLocalPos.x - localPos.x) / stepSize.x)) : 0;
            int stepsY = ay ? Mathf.Abs(Mathf.RoundToInt((endLocalPos.y - localPos.y) / stepSize.y)) : 0;
            int stepsZ = az ? Mathf.Abs(Mathf.RoundToInt((endLocalPos.z - localPos.z) / stepSize.z)) : 0;

            int xRange = ax ? stepsX + 1 : 1;
            int yRange = ay ? stepsY + 1 : 1;
            int zRange = az ? stepsZ + 1 : 1;

            float xDirection = ax ? Mathf.Sign(endLocalPos.x - localPos.x) : 0f;
            float yDirection = ay ? Mathf.Sign(endLocalPos.y - localPos.y) : 0f;
            float zDirection = az ? Mathf.Sign(endLocalPos.z - localPos.z) : 0f;

            for (int ix = 0; ix < xRange; ix++)
            {
                for (int iy = 0; iy < yRange; iy++)
                {
                    for (int iz = 0; iz < zRange; iz++)
                    {
                        if (ix == 0 && iy == 0 && iz == 0) continue;

                        float xOffset = xDirection * ix * stepSize.x;
                        float yOffset = yDirection * iy * stepSize.y;
                        float zOffset = zDirection * iz * stepSize.z;

                        var intermediateLocalPos = new Vector3(
                            localPos.x + xOffset,
                            localPos.y + yOffset,
                            localPos.z + zOffset
                        );

                        result.Add(rotation * intermediateLocalPos + origin);
                    }
                }
            }
            return result;
        }

        public static System.Collections.Generic.List<MirroredTransform> GetMirrorAndAxisModesTransforms(
            Vector3 position)
        {
            return GetMirrorAndAxisModesTransforms(position, Vector3.zero, Quaternion.identity);
        }

        public static System.Collections.Generic.List<MirroredTransform> GetMirrorAndAxisModesTransforms(
            Vector3 baseCellCenter, Vector3 brushOffset, Quaternion itemRotation)
        {
            var result = new System.Collections.Generic.List<MirroredTransform>();
            var processedPositions = new System.Collections.Generic.HashSet<Vector3>();

            var originalOffset = itemRotation * brushOffset;

            var axisPositions = GetAxisPositions(baseCellCenter);
            foreach (var axisPos in axisPositions)
            {
                var adjustedPos = axisPos + originalOffset;
                if (processedPositions.Add(adjustedPos))
                    result.Add(new MirroredTransform(adjustedPos, Quaternion.identity, Vector3.one));
            }

            var mirroredTransforms = GetMirroredTransforms(baseCellCenter, brushOffset, itemRotation);
            foreach (var mt in mirroredTransforms)
            {
                if (processedPositions.Add(mt.position))
                    result.Add(mt);
            }

            foreach (var axisPos in axisPositions)
            {
                var axisMirroredTransforms = GetMirroredTransforms(axisPos, brushOffset, itemRotation);
                foreach (var mt in axisMirroredTransforms)
                {
                    if (processedPositions.Add(mt.position))
                        result.Add(mt);
                }
            }

            return result;
        }

        public static System.Collections.Generic.List<Vector3> GetMirrorAndAxisModesPositions(Vector3 position)
        {
            return GetMirrorAndAxisModesTransforms(position).ConvertAll(t => t.position);
        }
        #endregion
    }
}
#pragma warning restore UDR0001
