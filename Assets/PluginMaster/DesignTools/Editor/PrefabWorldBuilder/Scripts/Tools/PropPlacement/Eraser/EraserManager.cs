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
    [System.Serializable]
    public class EraserSettings : CircleToolBase, ISelectionBrushTool, IModifierTool
    {
        [SerializeField] private ModifierToolSettings _modifierTool = new ModifierToolSettings();
        public EraserSettings() => _modifierTool.OnDataChanged += DataChanged;
        public ISelectionBrushTool.Command command { get => _modifierTool.command; set => _modifierTool.command = value; }

        public bool modifyAllButSelected
        {
            get => _modifierTool.modifyAllButSelected;
            set => _modifierTool.modifyAllButSelected = value;
        }

        public bool onlyTheClosest
        {
            get => _modifierTool.onlyTheClosest;
            set => _modifierTool.onlyTheClosest = value;
        }

        public bool outermostPrefabFilter
        {
            get => _modifierTool.outermostPrefabFilter;
            set => _modifierTool.outermostPrefabFilter = value;
        }

        public override void Copy(IToolSettings other)
        {
            var otherEraserSettings = other as EraserSettings;
            if (otherEraserSettings == null) return;
            base.Copy(other);
            _modifierTool.Copy(otherEraserSettings);
        }
    }

    [System.Serializable]
    public class EraserManager : ToolControllerBase<EraserSettings, EraserManager> { }
}
#pragma warning restore UDR0001
