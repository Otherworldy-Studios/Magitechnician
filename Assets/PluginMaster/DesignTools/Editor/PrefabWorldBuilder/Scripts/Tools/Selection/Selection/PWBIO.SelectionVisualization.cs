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
        private static System.Collections.Generic.List<Vector3> SelectionPoints(Camera camera)
        {
            var rotation = GetSelectionRotation();
            var bounds = _selectionBounds;
            var halfSizeRotated = rotation * bounds.size / 2;
            var min = bounds.center - halfSizeRotated;
            var max = bounds.center + halfSizeRotated;
            var points = new System.Collections.Generic.List<Vector3>
            {
                min,
                min + rotation * new Vector3(bounds.size.x, 0f, 0f),
                min + rotation * new Vector3(bounds.size.x, 0f, bounds.size.z),
                min + rotation * new Vector3(0f, 0f, bounds.size.z),
                min + rotation * new Vector3(0f, bounds.size.y, 0f),
                min + rotation * new Vector3(bounds.size.x, bounds.size.y, 0f),
                max,
                min + rotation * new Vector3(0f, bounds.size.y, bounds.size.z),
                min + rotation * new Vector3(bounds.size.x, 0f, bounds.size.z) / 2,
                max - rotation * new Vector3(bounds.size.x, 0f, bounds.size.z) / 2,
            };

            var visibleIdx = GetVisiblePoints(points.ToArray(), camera);

            points.Add(bounds.center);
            points.Add(bounds.center + _selectionRotation * _tempSelectionHandle);

            void DrawLine(Vector3[] line, float alpha = 1f)
            {
                UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.5f);
                UnityEditor.Handles.DrawAAPolyLine(10, line);
                UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.3f * alpha);
                UnityEditor.Handles.DrawAAPolyLine(4, line);
            }

            var visibleLines = new System.Collections.Generic.List<Vector3[]>();
            float ocludedAlpha = 0.5f;
            for (int i = 0; i < 8; ++i)
            {
                var visibleLine = visibleIdx.Contains(i) && visibleIdx.Contains(i + 4);
                if (i < 4)
                {
                    var vLine = new Vector3[] { points[i],
                        points[i] + rotation * new Vector3(0f, bounds.size.y, 0f) };
                    if (visibleLine) visibleLines.Add(vLine);
                    else DrawLine(vLine, ocludedAlpha);
                    points.Add(vLine[0] + (vLine[1] - vLine[0]) / 2);
                }
                int nextI = ((i + 1) % 4) + 4 * (i / 4);
                visibleLine = visibleIdx.Contains(i) && visibleIdx.Contains(nextI);
                var hLine = new Vector3[] { points[i], points[nextI] };
                if (visibleLine) visibleLines.Add(hLine);
                else DrawLine(hLine, ocludedAlpha);
                var midpoint = hLine[0] + (hLine[1] - hLine[0]) / 2;
                points.Add(midpoint);
                if (i < 4) points.Add(midpoint + rotation * new Vector3(0f, bounds.size.y / 2, 0f));
            }
            foreach (var line in visibleLines) DrawLine(line);
            for (int i = 0; i < 8; ++i)
            {
                var alpha = visibleIdx.Contains(i) ? 1f : 0.3f;
                DrawDotHandleCap(points[i], alpha);
            }
            DrawDotHandleCap(points[11], 1);
            if (SelectionManager.topLevelSelection.Length == 1)
            {
                var pivotPosition = SelectionManager.topLevelSelection[0].transform.position;
                points.Add(pivotPosition);
                DrawDotHandleCap(pivotPosition, isPivot: true);
            }
            return points;
        }

        private static System.Collections.Generic.HashSet<int> GetVisiblePoints(Vector3[] points, Camera camera)
        {
            var resultSet = new System.Collections.Generic.HashSet<int>(GrahamScan(points));
            if (resultSet.Count == 6)
            {
                var ocluded = new System.Collections.Generic.List<int>();
                for (int i = 0; i < points.Length; ++i)
                {
                    if (resultSet.Contains(i)) continue;
                    ocluded.Add(i);
                }
                if ((ocluded[0] / 4 == ocluded[1] / 4) || (ocluded[1] == ocluded[0] + 4))
                    return resultSet;
                var nearestIdx = camera.transform.InverseTransformPoint(points[ocluded[0]]).z
                    < camera.transform.InverseTransformPoint(points[ocluded[1]]).z ? ocluded[0] : ocluded[1];
                resultSet.Add(nearestIdx);
            }
            return resultSet;
        }

        private static int[] GrahamScan(Vector3[] points)
        {
            var screenPoints = new System.Collections.Generic.List<BoxPoint>();
            for (int i = 0; i < points.Length; ++i)
                screenPoints.Add(new BoxPoint(i, UnityEditor.HandleUtility.WorldToGUIPoint(points[i])));
            var p0 = screenPoints[0];
            foreach (var value in screenPoints)
            {
                if (p0.point.y > value.point.y) p0 = value;
            }
            var order = new System.Collections.Generic.List<BoxPoint>();
            foreach (var point in screenPoints)
            {
                if (p0 != point) order.Add(point);
            }
            order = MergeSort(p0, order);
            var result = new System.Collections.Generic.List<BoxPoint>();
            result.Add(p0);
            result.Add(order[0]);
            result.Add(order[1]);
            order.RemoveAt(0);
            order.RemoveAt(0);
            foreach (var value in order) KeepLeft(result, value);
            var resultIdx = new int[result.Count];
            for (int i = 0; i < result.Count; ++i) resultIdx[i] = result[i];
            return resultIdx;
        }

        private class BoxPoint
        {
            public int idx = -1;
            public Vector2 point = Vector2.zero;
            public BoxPoint(int idx, Vector2 point) => (this.idx, this.point) = (idx, point);
            public override int GetHashCode()
            {
                int hashCode = 386348313;
                hashCode = hashCode * -1521134295 + idx.GetHashCode();
                hashCode = hashCode * -1521134295 + point.GetHashCode();
                return hashCode;
            }
            public bool Equals(BoxPoint other) => GetHashCode() == other.GetHashCode();
            public override bool Equals(object obj) => Equals(obj as BoxPoint);
            public static bool operator ==(BoxPoint l, BoxPoint r) => l.Equals(r);
            public static bool operator !=(BoxPoint l, BoxPoint r) => !l.Equals(r);
            public static implicit operator Vector2(BoxPoint value) => value.point;
            public static implicit operator int(BoxPoint value) => value.idx;
        }

        private static System.Collections.Generic.List<BoxPoint> MergeSort(BoxPoint p0,
            System.Collections.Generic.List<BoxPoint> pointList)
        {
            if (pointList.Count == 1) return pointList;
            var sortedList = new System.Collections.Generic.List<BoxPoint>();
            int middle = pointList.Count / 2;
            var leftArray = pointList.GetRange(0, middle);
            var rightArray = pointList.GetRange(middle, pointList.Count - middle);
            leftArray = MergeSort(p0, leftArray);
            rightArray = MergeSort(p0, rightArray);
            int leftptr = 0;
            int rightptr = 0;
            for (int i = 0; i < leftArray.Count + rightArray.Count; i++)
            {
                if (leftptr == leftArray.Count)
                {
                    sortedList.Add(rightArray[rightptr]);
                    rightptr++;
                }
                else if (rightptr == rightArray.Count)
                {
                    sortedList.Add(leftArray[leftptr]);
                    leftptr++;
                }
                else if (GetAngle(p0, leftArray[leftptr]) < GetAngle(p0, rightArray[rightptr]))
                {
                    sortedList.Add(leftArray[leftptr]);
                    leftptr++;
                }
                else
                {
                    sortedList.Add(rightArray[rightptr]);
                    rightptr++;
                }
            }
            return sortedList;
        }

        private static double GetAngle(Vector2 p1, Vector2 p2)
        {
            float xDiff = p2.x - p1.x;
            float yDiff = p2.y - p1.y;
            return Mathf.Atan2(yDiff, xDiff) * 180f / Mathf.PI;
        }

        private static void KeepLeft(System.Collections.Generic.List<BoxPoint> hull, BoxPoint point)
        {
            int turn(Vector2 p, Vector2 q, Vector2 r)
                => ((q.x - p.x) * (r.y - p.y) - (r.x - p.x) * (q.y - p.y)).CompareTo(0);
            while (hull.Count > 1 && turn(hull[hull.Count - 2], hull[hull.Count - 1], point) != 1)
                hull.RemoveAt(hull.Count - 1);
            if (hull.Count == 0 || hull[hull.Count - 1] != point) hull.Add(point);
        }
    }
}
#pragma warning restore UDR0001
