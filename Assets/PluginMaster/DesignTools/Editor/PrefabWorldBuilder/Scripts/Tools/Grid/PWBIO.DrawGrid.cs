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
        private static void DrawGrid(AxesUtils.Axis axis, Vector3 focusPoint, int maxCells, Vector3 snapSize)
        {
            var rotation = GridManager.settings.rotation;
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            var focusOffset = Quaternion.Inverse(GridManager.settings.rotation) * (focusPoint - GridManager.settings.origin);
            var focusOffsetInt = new Vector3Int(Mathf.RoundToInt(focusOffset.x / snapSize.x),
              Mathf.RoundToInt(focusOffset.y / snapSize.y), Mathf.RoundToInt(focusOffset.z / snapSize.z));
            float GetAlpha(float cell, int majorLinesGap) => (cell % majorLinesGap == 0) ? 0.5f : 0.2f;

            for (int i = 0; i < maxCells; ++i)
            {
                for (int j = 1; j < maxCells; ++j)
                {
                    var p1 = Vector3.zero;
                    var p2 = Vector3.zero;
                    var p3 = Vector3.zero;
                    var p4 = Vector3.zero;

                    var alpha1 = (maxCells - Mathf.Max(i, j - 1)) / (float)maxCells;
                    var alpha2 = alpha1;
                    var alpha3 = alpha1;
                    var alpha4 = alpha1;
                    var alpha1R = alpha1;
                    var alpha2R = alpha1;
                    var alpha4R = alpha1;
                    var alpha3R = alpha1;

                    var color = new Color(0.5f, 1f, 0.5f, 0f);
                    switch (axis)
                    {
                        case AxesUtils.Axis.X:
                            color = new Color(1f, 0.5f, 0.5f, 0f);
                            alpha1 *= GetAlpha(i + focusOffsetInt.y, GridManager.settings.majorLinesGap.y);
                            alpha2 *= GetAlpha(i - focusOffsetInt.y, GridManager.settings.majorLinesGap.y);
                            alpha3 *= GetAlpha(i + focusOffsetInt.z, GridManager.settings.majorLinesGap.z);
                            alpha4 *= GetAlpha(i - focusOffsetInt.z, GridManager.settings.majorLinesGap.z);
                            alpha1R = alpha1;
                            alpha2R = alpha2;
                            alpha3R = alpha4;
                            alpha4R = alpha3;
                            p1 += rotation * Vector3.Scale(new Vector3(0f, i, j - 1), snapSize);
                            p2 += rotation * Vector3.Scale(new Vector3(0f, i, j), snapSize);
                            p3 += rotation * Vector3.Scale(new Vector3(0f, j - 1, i), snapSize);
                            p4 += rotation * Vector3.Scale(new Vector3(0f, j, i), snapSize);
                            break;
                        case AxesUtils.Axis.Y:
                            alpha1 *= GetAlpha(i + focusOffsetInt.x, GridManager.settings.majorLinesGap.x);
                            alpha2 *= GetAlpha(i - focusOffsetInt.x, GridManager.settings.majorLinesGap.x);
                            alpha3 *= GetAlpha(i + focusOffsetInt.z, GridManager.settings.majorLinesGap.z);
                            alpha4 *= GetAlpha(i - focusOffsetInt.z, GridManager.settings.majorLinesGap.z);
                            alpha1R = alpha2;
                            alpha2R = alpha1;
                            alpha3R = alpha3;
                            alpha4R = alpha4;
                            p1 += rotation * Vector3.Scale(new Vector3(i, 0f, j - 1), snapSize);
                            p2 += rotation * Vector3.Scale(new Vector3(i, 0f, j), snapSize);
                            p3 += rotation * Vector3.Scale(new Vector3(j - 1, 0f, i), snapSize);
                            p4 += rotation * Vector3.Scale(new Vector3(j, 0f, i), snapSize);
                            break;
                        case AxesUtils.Axis.Z:
                            color = new Color(0.5f, 0.5f, 1f, 0f);
                            alpha1 *= GetAlpha(i + focusOffsetInt.x, GridManager.settings.majorLinesGap.x);
                            alpha2 *= GetAlpha(i - focusOffsetInt.x, GridManager.settings.majorLinesGap.x);
                            alpha3 *= GetAlpha(i + focusOffsetInt.y, GridManager.settings.majorLinesGap.y);
                            alpha4 *= GetAlpha(i - focusOffsetInt.y, GridManager.settings.majorLinesGap.y);
                            alpha1R = alpha1;
                            alpha2R = alpha2;
                            alpha3R = alpha4;
                            alpha4R = alpha3;
                            p1 += rotation * Vector3.Scale(new Vector3(i, j - 1, 0f), snapSize);
                            p2 += rotation * Vector3.Scale(new Vector3(i, j, 0f), snapSize);
                            p3 += rotation * Vector3.Scale(new Vector3(j - 1, i, 0f), snapSize);
                            p4 += rotation * Vector3.Scale(new Vector3(j, i, 0f), snapSize);
                            break;
                    }
                    UnityEditor.Handles.color = color + new Color(0f, 0f, 0f, alpha1);
                    UnityEditor.Handles.DrawLine(focusPoint + p1, focusPoint + p2);
                    UnityEditor.Handles.color = color + new Color(0f, 0f, 0f, alpha2);
                    UnityEditor.Handles.DrawLine(focusPoint - p1, focusPoint - p2);
                    UnityEditor.Handles.color = color + new Color(0f, 0f, 0f, alpha3);
                    UnityEditor.Handles.DrawLine(focusPoint + p3, focusPoint + p4);
                    UnityEditor.Handles.color = color + new Color(0f, 0f, 0f, alpha4);
                    UnityEditor.Handles.DrawLine(focusPoint - p3, focusPoint - p4);
                    if (i == 0) continue;
                    var r180 = Quaternion.AngleAxis(180, rotation * (axis == AxesUtils.Axis.X ? Vector3.up :
                        axis == AxesUtils.Axis.Y ? Vector3.forward : Vector3.right));
                    UnityEditor.Handles.color = color + new Color(0f, 0f, 0f, alpha1R);
                    UnityEditor.Handles.DrawLine(focusPoint + r180 * p1, focusPoint + r180 * p2);
                    UnityEditor.Handles.color = color + new Color(0f, 0f, 0f, alpha2R);
                    UnityEditor.Handles.DrawLine(focusPoint - r180 * p1, focusPoint - r180 * p2);
                    UnityEditor.Handles.color = color + new Color(0f, 0f, 0f, alpha3R);
                    UnityEditor.Handles.DrawLine(focusPoint + r180 * p3, focusPoint + r180 * p4);
                    UnityEditor.Handles.color = color + new Color(0f, 0f, 0f, alpha4R);
                    UnityEditor.Handles.DrawLine(focusPoint - r180 * p3, focusPoint - r180 * p4);
                }
            }
        }

    }
}
#pragma warning restore UDR0001
