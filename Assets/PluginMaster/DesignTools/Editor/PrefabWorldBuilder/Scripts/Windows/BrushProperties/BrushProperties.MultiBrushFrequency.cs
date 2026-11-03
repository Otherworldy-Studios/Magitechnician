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
    public partial class BrushProperties : UnityEditor.EditorWindow, ISerializationCallbackReceiver
    {
        private readonly string[] FREQUENCY_MODES = new string[] { "Random", "Pattern" };

        private Texture2D _warningTexture = null;
        private string _patternWarningMsg = null;
        private void FrequencyGroup()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {

                var brush = PaletteManager.selectedBrush;
                var changed = false;
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    var frequencyMode = (MultibrushSettings.FrequencyMode)
                        UnityEditor.EditorGUILayout.Popup("Frequency Mode", (int)brush.frequencyMode, FREQUENCY_MODES);
                    if (check.changed)
                    {
                        changed = true;
                        brush.frequencyMode = frequencyMode;
                    }
                }

                var item = GetSelectedItem(brush);
                if (brush.frequencyMode == MultibrushSettings.FrequencyMode.RANDOM)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (_selection.Count <= 1)
                        {
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                var frequency = UnityEditor.EditorGUILayout.FloatField("Frequency", item.frequency);
                                if (check.changed)
                                {
                                    changed = true;
                                    item.frequency = frequency;
                                }
                            }
                            GUILayout.Label("in " + brush.totalFrequency);
                        }
                        else
                        {
                            GUILayout.Box(GetStateGUIContent(_itemSelectionState.frequency),
                                UnityEditor.EditorStyles.label);
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                _itemSelectionSettings.frequency = UnityEditor.EditorGUILayout.FloatField("Frequency",
                                    _itemSelectionSettings.frequency);
                                if (check.changed)
                                {
                                    foreach (var selectedIdx in _selection)
                                    {
                                        var selectedItem = PaletteManager.selectedBrush.GetItemAt(selectedIdx);
                                        selectedItem.frequency = _itemSelectionSettings.frequency;
                                    }
                                    brush.UpdateTotalFrequency();
                                    _itemSelectionState.frequency = SelectionFieldState.CHANGED;
                                }
                            }
                            GUILayout.Label("in " + brush.totalFrequency);
                            GUILayout.FlexibleSpace();
                        }
                    }
                }
                else
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var pattern = UnityEditor.EditorGUILayout.TextField("Pattern", brush.pattern);
                            if (check.changed || brush.patternMachine == null)
                            {
                                _patternWarningMsg = null;
                                switch (PatternMachine.Validate(pattern, brush.items.Length,
                                    out PatternMachine.Token[] tokens, out PatternMachine.Token[] endTokens))
                                {
                                    case PatternMachine.ValidationResult.EMPTY:
                                        _patternWarningMsg = "Empty pattern"; break;
                                    case PatternMachine.ValidationResult.INDEX_OUT_OF_RANGE:
                                        _patternWarningMsg = "Index out of range"; break;
                                    case PatternMachine.ValidationResult.MISPLACED_PERIOD:
                                        _patternWarningMsg = "Misplaced period"; break;
                                    case PatternMachine.ValidationResult.MISPLACED_ASTERISK:
                                        _patternWarningMsg = "Misplaced asterisk"; break;
                                    case PatternMachine.ValidationResult.MISSING_COMMA:
                                        _patternWarningMsg = "Missing comma"; break;
                                    case PatternMachine.ValidationResult.MISPLACED_COMMA:
                                        _patternWarningMsg = "Mispalced comma"; break;
                                    case PatternMachine.ValidationResult.UNPAIRED_PARENTHESIS:
                                        _patternWarningMsg = "Unpaired parenthesis"; break;
                                    case PatternMachine.ValidationResult.EMPTY_PARENTHESIS:
                                        _patternWarningMsg = "Empty parenthesis"; break;
                                    case PatternMachine.ValidationResult.INVALID_MULTIPLIER:
                                        _patternWarningMsg = "The multiplier must be greater than one"; break;
                                    case PatternMachine.ValidationResult.UNPAIRED_BRACKET:
                                        _patternWarningMsg = "Unpaired bracket"; break;
                                    case PatternMachine.ValidationResult.EMPTY_BRACKET:
                                        _patternWarningMsg = "Empty bracket"; break;
                                    case PatternMachine.ValidationResult.INVALID_NESTED_BRACKETS:
                                        _patternWarningMsg = "Invalid nested bracket"; break;
                                    case PatternMachine.ValidationResult.INVALID_PARENTHESES_WITHIN_BRACKETS:
                                        _patternWarningMsg = "Invalid parentheses within brackets"; break;
                                    case PatternMachine.ValidationResult.MISPLACED_VERTICAL_BAR:
                                        _patternWarningMsg = "Misplaced vertical bar"; break;
                                    case PatternMachine.ValidationResult.MISPLACED_COLON:
                                        _patternWarningMsg = "Misplaced Colon"; break;
                                    case PatternMachine.ValidationResult.INVALID_CHARACTER:
                                        _patternWarningMsg = "Invalid character"; break;
                                    default:
                                        brush.pattern = pattern;
                                        brush.patternMachine = new PatternMachine(tokens, endTokens);
                                        break;
                                }
                            }
                            if (_patternWarningMsg != null && _patternWarningMsg != string.Empty)
                            {
                                var style = new GUIStyle();
                                style.margin.top = 4;
                                if (_warningTexture == null)
                                    _warningTexture = Resources.Load<Texture2D>("Sprites/Warning");
                                GUILayout.Box(new GUIContent(_warningTexture, _patternWarningMsg), style,
                                    GUILayout.Width(14), GUILayout.Height(14));
                            }
                        }
                    }

                    using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
                    {
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            var restartPatternForEachStroke
                                = UnityEditor.EditorGUILayout.ToggleLeft("Restart the pattern for each stroke",
                                brush.restartPatternForEachStroke, GUILayout.Width(220));
                            if (check.changed)
                            {
                                changed = true;
                                brush.restartPatternForEachStroke = restartPatternForEachStroke;
                            }
                        }
                        if (!brush.restartPatternForEachStroke)
                        {
                            if (GUILayout.Button("Restart Pattern"))
                            {
                                brush.patternMachine.Reset();
                                BrushstrokeManager.UpdateBrushstroke();
                            }
                        }
                    }
                }
                if (changed)
                {
                    BrushstrokeManager.UpdateBrushstroke(false);
                    PaletteManager.selectedPalette.Save();
                    UnityEditor.SceneView.RepaintAll();
                }
            }
        }
    }
}
#pragma warning restore UDR0001
