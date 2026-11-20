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

        private static System.Collections.Generic.List<System.Collections.Generic.List<Vector3>> _initialPinBoundPoints
            = new System.Collections.Generic.List<System.Collections.Generic.List<Vector3>>();
        private static System.Collections.Generic.List<System.Collections.Generic.List<Vector3>> _pinBoundPoints
            = new System.Collections.Generic.List<System.Collections.Generic.List<Vector3>>();
        private static int _pinBoundPointIdx = 0;
        private static int pinBoundPointIdx
        {
            get
            {
                if (PinManager.settings.flattenTerrain && _pinBoundPointIdx == 0
                    && _pinBoundPoints[pinBoundLayerIdx].Count > 0) return 1;
                return _pinBoundPointIdx;
            }
            set => _pinBoundPointIdx = value;
        }
        private static int _pinBoundLayerIdx = 0;
        private static int pinBoundLayerIdx
        {
            get
            {
                if (PinManager.settings.flattenTerrain) return 0;
                return _pinBoundLayerIdx;
            }
            set => _pinBoundLayerIdx = value; 
        }

        private static void UpdatePinScale()
        {
            for (int l = 0; l < _pinBoundPoints.Count; ++l)
                for (int p = 0; p < _pinBoundPoints[l].Count; ++p)
                    _pinBoundPoints[l][p] = _initialPinBoundPoints[l][p] * _pinScale;
            _pinOffset = _pinBoundPoints[pinBoundLayerIdx][pinBoundPointIdx];
        }
        private static void UpdatePinScale(float value)
        {
            if (_pinScale == value) return;
            _pinScale = value;
            UpdatePinScale();
            UnityEditor.SceneView.RepaintAll();
        }
        private static Vector3 pivotBoundPoint
        {
            get
            {
                pinBoundPointIdx = 0;
                return _pinBoundPoints[pinBoundLayerIdx][pinBoundPointIdx];
            }
        }
        private static Vector3 nextBoundPoint
        {
            get
            {
                ++pinBoundPointIdx;
                if (pinBoundPointIdx >= _pinBoundPoints[pinBoundLayerIdx].Count) pinBoundPointIdx = 0;
                return _pinBoundPoints[pinBoundLayerIdx][pinBoundPointIdx];
            }
        }

        private static Vector3 prevBoundPoint
        {
            get
            {
                --pinBoundPointIdx;
                if (pinBoundPointIdx < 0) pinBoundPointIdx = _pinBoundPoints[pinBoundLayerIdx].Count - 1;
                return _pinBoundPoints[pinBoundLayerIdx][pinBoundPointIdx];
            }
        }

        private static Vector3 nextBoundLayer
        {
            get
            {
                ++pinBoundLayerIdx;
                if (pinBoundLayerIdx >= _pinBoundPoints.Count) pinBoundLayerIdx = 0;
                return _pinBoundPoints[pinBoundLayerIdx][pinBoundPointIdx];
            }
        }

        private static Vector3 prevBoundLayer
        {
            get
            {
                --pinBoundLayerIdx;
                if (pinBoundLayerIdx < 0) pinBoundLayerIdx = _pinBoundPoints.Count - 1;

                return _pinBoundPoints[pinBoundLayerIdx][pinBoundPointIdx];
            }
        }

        

        private static void SetPinValues(Quaternion additionRotation)
        {
            var strokeItem = BrushstrokeManager.brushstroke[0];
            var prefab = strokeItem.settings.prefab;
            if (prefab == null) return;

            var bounds = BoundsUtils.GetBoundsRecursive(prefab.transform, prefab.transform.rotation);

            _pinBoundPoints.Clear();
            _initialPinBoundPoints.Clear();

            var centerToPivot = GetCenterToPivot(prefab, strokeItem.scaleMultiplier, Quaternion.identity);

            var pointRotation = additionRotation;

            var xProjection = Vector3.Project(_pinHit.normal, additionRotation * Vector3.right);
            var yProjection = Vector3.Project(_pinHit.normal, additionRotation * Vector3.up);
            var zProjection = Vector3.Project(_pinHit.normal, additionRotation * Vector3.forward);

            var xProjectionMagnitude = xProjection.magnitude;
            var yProjectionMagnitude = yProjection.magnitude;
            var zProjectionMagnitude = zProjection.magnitude;

            var nearestAxisToSurfaceNormal = AxesUtils.Axis.Y;

            var maxProjectionMagnitude = yProjectionMagnitude;
            if (xProjectionMagnitude > maxProjectionMagnitude)
            {
                nearestAxisToSurfaceNormal = AxesUtils.Axis.X;
                maxProjectionMagnitude = xProjectionMagnitude;
            }
            if (zProjectionMagnitude > maxProjectionMagnitude) nearestAxisToSurfaceNormal = AxesUtils.Axis.Z;
            var halfSize = Vector3.Scale(bounds.size, strokeItem.scaleMultiplier) * 0.5f;

            int l = 0;
            var pointsNormalized = new Vector2[] { new Vector2(0,0),
                    new Vector2(-1,0), new Vector2(0,1), new Vector2(1,0),  new Vector2(0,-1),
                    new Vector2(-1,-1), new Vector2(-1,1), new Vector2(1,1), new Vector2(1,-1)};

            if (nearestAxisToSurfaceNormal == AxesUtils.Axis.Y)
            {
                var sign = 1;
                if (!strokeItem.settings.rotateToTheSurface) sign = Vector3.Dot(Vector3.up, yProjection) > 0 ? 1 : -1;
                _pinProjectionDirection = additionRotation * (Vector3.down * sign);
                for (int y = -1; y <= 1; y += 2)
                {
                    _pinBoundPoints.Add(new System.Collections.Generic.List<Vector3>());
                    _initialPinBoundPoints.Add(new System.Collections.Generic.List<Vector3>());
                    _pinBoundPoints[l].Add(Vector3.zero);
                    _initialPinBoundPoints[l].Add(Vector3.zero);
                    foreach (var n in pointsNormalized)
                    {
                        var point = strokeItem.settings.isAsset2D
                            ? new Vector3(n.x, n.y, -y) : new Vector3(n.x, -y * sign, n.y);
                        point = Vector3.Scale(point, halfSize) + centerToPivot;
                        point = pointRotation * point;
                        _pinBoundPoints[l].Add(point);
                        _initialPinBoundPoints[l].Add(point);
                    }
                    ++l;
                }
            }
            else if (nearestAxisToSurfaceNormal == AxesUtils.Axis.X)
            {
                var sign = 1;
                if (!strokeItem.settings.rotateToTheSurface) sign = Vector3.Dot(Vector3.right, xProjection) > 0 ? 1 : -1;
                _pinProjectionDirection = additionRotation * (Vector3.left * sign);
                for (int x = -1; x <= 1; x += 2)
                {
                    _pinBoundPoints.Add(new System.Collections.Generic.List<Vector3>());
                    _initialPinBoundPoints.Add(new System.Collections.Generic.List<Vector3>());
                    _pinBoundPoints[l].Add(Vector3.zero);
                    _initialPinBoundPoints[l].Add(Vector3.zero);

                    foreach (var n in pointsNormalized)
                    {
                        var point = strokeItem.settings.isAsset2D
                            ? new Vector3(n.x, n.y, -x) : new Vector3(-x * sign, n.y, n.x);
                        point = Vector3.Scale(point, halfSize) + centerToPivot;
                        point = pointRotation * point;
                        _pinBoundPoints[l].Add(point);
                        _initialPinBoundPoints[l].Add(point);
                    }
                    ++l;
                }
            }
            else if (nearestAxisToSurfaceNormal == AxesUtils.Axis.Z)
            {
                var sign = 1;
                if (!strokeItem.settings.rotateToTheSurface) sign = Vector3.Dot(Vector3.forward, zProjection) > 0 ? 1 : -1;
                _pinProjectionDirection = additionRotation * (Vector3.back * sign);
                for (int z = -1; z <= 1; z += 2)
                {
                    _pinBoundPoints.Add(new System.Collections.Generic.List<Vector3>());
                    _initialPinBoundPoints.Add(new System.Collections.Generic.List<Vector3>());
                    _pinBoundPoints[l].Add(Vector3.zero);
                    _initialPinBoundPoints[l].Add(Vector3.zero);
                    foreach (var n in pointsNormalized)
                    {
                        var point = strokeItem.settings.isAsset2D
                            ? new Vector3(n.x, n.y, -z) : new Vector3(n.x, n.y, -z * sign);
                        point = Vector3.Scale(point, halfSize) + centerToPivot;
                        point = pointRotation * point;
                        _pinBoundPoints[l].Add(point);
                        _initialPinBoundPoints[l].Add(point);
                    }
                    ++l;
                }
            }
        }
        public static void ResetPinValues()
        {
            _pinned = false;
            _pinMouse = Vector3.zero;
            _pinHit = new RaycastHit();
            _pinAngle = Vector3.zero;
            _pinScale = 1f;
            _pinDistanceFromSurface = 0f;
            if (BrushstrokeManager.brushstroke.Length == 0) return;
            var strokeItem = BrushstrokeManager.brushstroke[0];
            SetPinValues(Quaternion.Euler(strokeItem.additionalAngle));
            BrushSettings brushSettings = strokeItem.settings;
            if (PinManager.settings.overwriteBrushProperties) brushSettings = PinManager.settings.brushSettings;
            repaint = true;
            _pinOffset = _pinBoundPoints[pinBoundLayerIdx][pinBoundPointIdx];
            UnityEditor.SceneView.RepaintAll();
        }
        public static void UpdatePinValues(GameObject prefab, Quaternion rotation)
        {
            if (prefab == null) return;
            var additionalRotation = rotation;
            float RoundToStraightAngle(float angle) => Mathf.Round(angle / 90f) * 90f;
            var up = additionalRotation * Vector3.up;
            var fromUpToNormalRotation = Quaternion.FromToRotation(up, _pinHit.normal);
            Vector3 RoundEulerToStraightAngles(Vector3 euler)
                => new Vector3(RoundToStraightAngle(euler.x), RoundToStraightAngle(euler.y), RoundToStraightAngle(euler.z));
            var fromUpToNormalEulerRounded = RoundEulerToStraightAngles(fromUpToNormalRotation.eulerAngles);
            fromUpToNormalRotation = Quaternion.Euler(fromUpToNormalEulerRounded);
            SetPinValues(additionalRotation);
            var layerIdx = Mathf.Clamp(pinBoundLayerIdx, 0, _pinBoundPoints.Count - 1);
            var pointIdx = Mathf.Clamp(pinBoundPointIdx, 0, _pinBoundPoints[layerIdx].Count - 1);
            UpdatePinScale();
            repaint = true;
            UnityEditor.SceneView.RepaintAll();
        }
    }
}
#pragma warning restore UDR0001
