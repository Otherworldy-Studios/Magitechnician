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
    public static class SceneViewCameraOrientation
    {
        public enum Orientation
        {
            Top,
            Bottom,
            Left,
            Right,
            Front,
            Back,
            NotAligned
        }

        public static Orientation GetSceneViewOrientation()
        {
            if (UnityEditor.SceneView.lastActiveSceneView == null) return Orientation.NotAligned;
            var cam = UnityEditor.SceneView.lastActiveSceneView.camera;
            if (cam == null) return Orientation.NotAligned;

            Vector3 forward = cam.transform.forward;

            if (Vector3.Dot(forward, Vector3.down) > 0.99f) return Orientation.Top;
            if (Vector3.Dot(forward, Vector3.up) > 0.99f) return Orientation.Bottom;
            if (Vector3.Dot(forward, Vector3.left) > 0.99f) return Orientation.Right;
            if (Vector3.Dot(forward, Vector3.right) > 0.99f) return Orientation.Left;
            if (Vector3.Dot(forward, Vector3.back) > 0.99f) return Orientation.Front;
            if (Vector3.Dot(forward, Vector3.forward) > 0.99f) return Orientation.Back;

            return Orientation.NotAligned;
        }
    }
}
#pragma warning restore UDR0001
