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

namespace PluginMaster
{
    public static class SelectionUtils
    {
        public static void Swap<T>(int fromIdx, int toIdx, ref int[] selection, System.Collections.Generic.List<T> list)
        {
            if (fromIdx == toIdx) return;
            var newOrder = new System.Collections.Generic.List<T>();
            var newSelection = selection.ToArray();
            for (int idx = 0; idx <= list.Count; ++idx)
            {
                if (idx == toIdx)
                {
                    System.Array.Sort(selection);
                    int newSelectionIdx = 0;
                    foreach (var selectionIdx in selection)
                    {
                        newOrder.Add(list[selectionIdx]);
                        newSelection[newSelectionIdx++] = newOrder.Count - 1;
                    }
                    if (idx < list.Count && !selection.Contains(idx)) newOrder.Add(list[idx]);
                }
                else if (selection.Contains(idx)) continue;
                else if (idx < list.Count) newOrder.Add(list[idx]);
            }
            selection = newSelection;
            list.Clear();
            list.AddRange(newOrder);
            PWBCore.staticData.Save();
        }
    }
}
#pragma warning restore UDR0001
