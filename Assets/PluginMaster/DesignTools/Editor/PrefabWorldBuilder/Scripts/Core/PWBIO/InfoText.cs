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
    public static class InfoText
    {
        private static readonly GUIStyle labelStyle = new GUIStyle(UnityEditor.EditorStyles.label)
        {
            normal = { textColor = new Color(1, 1, 1, 0.7f) },
            alignment = TextAnchor.UpperLeft
        };

        public static void Draw(UnityEditor.SceneView sceneView, string[] texts)
        {
            UnityEditor.Handles.BeginGUI();
            Vector2 mousePos = Event.current.mousePosition;
            const float lineHeight = 18f;
            const float topMargin = 4f;
            const float leftMargin = 4f;
            var numberOfLines = texts.Length;
            var totalHeight = numberOfLines * lineHeight + topMargin;
            var maxWidth = 0f;
            for (int i = 0; i < texts.Length; i++)
            {
                var width = labelStyle.CalcSize(new GUIContent(texts[i])).x;
                maxWidth = Mathf.Max(maxWidth, width);
            }
            var rectWidth = maxWidth + leftMargin * 2;

            var offsetX = 10f;
            if (mousePos.x + offsetX + rectWidth > sceneView.position.width - 50) offsetX = -(rectWidth + 10f);
            var offsetY = 10f;
            if (mousePos.y + offsetY + totalHeight > sceneView.position.height - 30) offsetY = -(totalHeight + 10f);

            var rect = new Rect(mousePos.x + offsetX, mousePos.y + offsetY, rectWidth, totalHeight);
            UnityEditor.EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.3f));
            var y = rect.y + topMargin;
            for (int i = 0; i < texts.Length; i++)
            {
                var lineRect = new Rect(rect.x + leftMargin, y, rectWidth, lineHeight);
                GUI.Label(lineRect, texts[i], labelStyle);
                y += lineHeight;
            }
            UnityEditor.Handles.EndGUI();
        }
    }
}
#pragma warning restore UDR0001
