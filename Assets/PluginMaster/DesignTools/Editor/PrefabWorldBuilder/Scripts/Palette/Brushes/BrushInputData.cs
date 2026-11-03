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
    public class BrushInputData
    {
        public readonly int index;
        public readonly Rect rect;
        public readonly EventType eventType;
        public readonly bool control;
        public readonly bool shift;
        public readonly float mouseX;
        public BrushInputData(int index, Rect rect, EventType eventType, bool control, bool shift, float mouseX)
        {
            this.index = index;
            this.rect = rect;
            this.eventType = eventType;
            this.control = !shift && control;
            this.shift = shift;
            this.mouseX = mouseX;
        }
    }
}
#pragma warning restore UDR0001
