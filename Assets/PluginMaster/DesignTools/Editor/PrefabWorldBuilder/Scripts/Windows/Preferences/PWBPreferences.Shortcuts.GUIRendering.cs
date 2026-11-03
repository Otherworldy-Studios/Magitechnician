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
    public partial class PWBPreferences : UnityEditor.EditorWindow
    {
        private void Shortcuts()
        {
            if (_multiColumnHeader == null) InitializeMultiColumn();

            string shortcutString(PWBKeyShortcut shortcut)
            {
                if ((object)shortcut == (object)_selectedShortcut) return string.Empty;
                return shortcut.combination.ToString();
            }
            GUIStyle shortcutStyle(PWBKeyShortcut shortcut)
            {
                if ((object)shortcut == (object)_selectedShortcut) return UnityEditor.EditorStyles.textField;
                return UnityEditor.EditorStyles.label;
            }

            var categoryButton = new GUIStyle(UnityEditor.EditorStyles.toolbarButton);
            categoryButton.alignment = TextAnchor.UpperLeft;

            using (new GUILayout.HorizontalScope(UnityEditor.EditorStyles.helpBox))
            {
                GUILayout.Label("Profile");
                if (GUILayout.Button(PWBSettings.shortcuts.profileName,
                    UnityEditor.EditorStyles.popup, GUILayout.MinWidth(100)))
                {
                    GUI.FocusControl(null);
                    var menu = new UnityEditor.GenericMenu();
                    var profileNames = PWBSettings.shotcutProfileNames;
                    for (int i = 0; i < profileNames.Length; ++i)
                        menu.AddItem(new GUIContent(profileNames[i]),
                            PWBSettings.selectedProfileIdx == i, SelectProfileItem, i);
                    menu.AddSeparator(string.Empty);
                    menu.AddItem(new GUIContent("Factory Reset Selected Profile"), false, PWBSettings.ResetSelectedProfile);
                    menu.ShowAsContext();
                }
                GUILayout.FlexibleSpace();
            }

            using (new GUILayout.HorizontalScope())
            {
                const int categoryColumnW = 100;
                using (new GUILayout.VerticalScope(GUILayout.Width(categoryColumnW)))
                {
                    if (GUILayout.Toggle(_toolbarCategory, "Toolbar", categoryButton)) SelectCategory(ref _toolbarCategory);
                    if (GUILayout.Toggle(_gizmosCategory, "Gizmos", categoryButton)) SelectCategory(ref _gizmosCategory);
                    if (GUILayout.Toggle(_pinCategory, "Pin", categoryButton)) SelectCategory(ref _pinCategory);
                    if (GUILayout.Toggle(_brushCategory, "Brush", categoryButton)) SelectCategory(ref _brushCategory);
                    if (GUILayout.Toggle(_gravityCategory, "Gravity", categoryButton)) SelectCategory(ref _gravityCategory);
                    if (GUILayout.Toggle(_lineCategory, "Line", categoryButton)) SelectCategory(ref _lineCategory);
                    if (GUILayout.Toggle(_shapeCategory, "Shape", categoryButton)) SelectCategory(ref _shapeCategory);
                    if (GUILayout.Toggle(_tilingCategory, "Tiling", categoryButton)) SelectCategory(ref _tilingCategory);
                    if (GUILayout.Toggle(_eraserCategory, "Eraser", categoryButton)) SelectCategory(ref _eraserCategory);
                    if (GUILayout.Toggle(_replacerCategory, "Replacer", categoryButton)) SelectCategory(ref _replacerCategory);

                    if (GUILayout.Toggle(_floorCategory, "Floor", categoryButton)) SelectCategory(ref _floorCategory);
                    if (GUILayout.Toggle(_wallCategory, "Wall", categoryButton)) SelectCategory(ref _wallCategory);
                    if (GUILayout.Toggle(_blockCategory, "Block", categoryButton)) SelectCategory(ref _blockCategory);

                    if (GUILayout.Toggle(_selectionCategory, "Selection", categoryButton))
                        SelectCategory(ref _selectionCategory);
                    if (GUILayout.Toggle(_circleSelectCategory, "Circle Select", categoryButton))
                        SelectCategory(ref _circleSelectCategory);

                    if (GUILayout.Toggle(_gridCategory, "Grid", categoryButton)) SelectCategory(ref _gridCategory);
                    if (GUILayout.Toggle(_snapCategory, "Snap", categoryButton)) SelectCategory(ref _snapCategory);
                    if (GUILayout.Toggle(_paletteCategory, "Palette", categoryButton)) SelectCategory(ref _paletteCategory);
                    if (GUILayout.Toggle(_uiCategory, "UI", categoryButton)) SelectCategory(ref _uiCategory);
                    if (GUILayout.Toggle(_toolModesCategory, "Tool Modes", categoryButton))
                        SelectCategory(ref _toolModesCategory);

                    using (new UnityEditor.EditorGUI.DisabledGroupScope(true))
                        GUILayout.Box(new GUIContent(), new GUIStyle(categoryButton) { fixedHeight = 427 });
                }
                GUILayout.Space(2);
                using (new GUILayout.VerticalScope())
                {
                    var minX = categoryColumnW + 10;
                    var shorcutPanelRect = new Rect(minX, 28, position.width - categoryColumnW - 20, position.height);

                    float columnHeight = UnityEditor.EditorGUIUtility.singleLineHeight;
                    Rect columnRectPrototype = new Rect(shorcutPanelRect) { height = columnHeight };

                    _multiColumnHeader.OnGUI(rect: columnRectPrototype, xScroll: 0.0f);

                    void ContextMenu(PWBShortcut shortcut, UnityEditor.GenericMenu.MenuFunction DisableFunction)
                    {
                        bool shortcutIsUSM = false;
                        if (shortcut is PWBKeyShortcut)
                        {
                            var keyShortcut = shortcut as PWBKeyShortcut;
                            shortcutIsUSM = keyShortcut.combination is PWBKeyCombinationUSM;
                        }
                        void ResetToDefault()
                        {
                            PWBSettings.ResetShortcutToDefault(shortcut);
                            if (shortcutIsUSM)
                            {
                                var keyShortcut = shortcut as PWBKeyShortcut;
                                (keyShortcut.combination as PWBKeyCombinationUSM).Reset();
                            }
                            PWBSettings.UpdateShrotcutsConflictsAndSaveFile();
                        }
                        var menu = new UnityEditor.GenericMenu();
                        menu.AddItem(new GUIContent("Reset to default"), false, ResetToDefault);
                        if (!shortcutIsUSM) menu.AddItem(new GUIContent("Disable shortcut"), false, DisableFunction);
                        menu.ShowAsContext();
                    }

                    int row = 0;
                    void ShortcutRow(PWBKeyShortcut shortcut)
                    {
                        Rect rowRect = new Rect(columnRectPrototype);

                        rowRect.y += columnHeight * (++row);
                        UnityEditor.EditorGUI.DrawRect(rowRect, row % 2 == 0 ? _darkerColor : _lighterColor);

                        Rect columnRect = _multiColumnHeader.GetColumnRect(0);
                        columnRect.y = rowRect.y;

                        var cellRect = _multiColumnHeader.GetCellRect(0, columnRect);
                        cellRect.x += minX;


                        UnityEditor.EditorGUI.LabelField(cellRect, new GUIContent(shortcut.name));

                        columnRect = _multiColumnHeader.GetColumnRect(1);
                        columnRect.y = rowRect.y;

                        cellRect = _multiColumnHeader.GetCellRect(1, columnRect);
                        var cellW = cellRect.width;
                        var shortcutText = shortcutString(shortcut);
                        if ((shortcut is PWBTwoStepKeyShortcut && (shortcut.group & PWBShortcut.Group.GRID) == 0) 
                            || (shortcut is PWBTwoStepKeyShortcut && (shortcut.group & PWBShortcut.Group.GRID) != 0
                            && !PWBSettings.shortcuts.gridEnableShortcuts.combination.isDissabled()))
                        {
                            cellRect.x += minX;
                            cellRect.width = 20;
                            var twoStepShortcut = shortcut as PWBTwoStepKeyShortcut;
                            using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                            {
                                var firstStepEnabled = UnityEditor.EditorGUI.Toggle(cellRect,
                                twoStepShortcut.firstStepEnabled);
                                if (check.changed)
                                {
                                    twoStepShortcut.firstStepEnabled = firstStepEnabled;
                                    PWBSettings.UpdateShrotcutsConflictsAndSaveFile();
                                }
                            }
                            cellRect.x += 20;
                            cellRect.width = cellW - 40;
                            if (twoStepShortcut.firstStepEnabled)
                                shortcutText = PWBSettings.shortcuts.gridEnableShortcuts.combination.ToString() + ", "
                                    + shortcutText;
                        }
                        else
                        {
                            cellRect.x += minX;
                            cellRect.width = cellW - 20;
                        }

                        UnityEditor.EditorGUI.LabelField(cellRect, new GUIContent(shortcutText),
                            shortcutStyle(shortcut));

                        if (cellRect.Contains(Event.current.mousePosition)
                            && Event.current.type == EventType.MouseDown)
                        {
                            if (Event.current.button == 0)
                            {
                                _selectedShortcut = shortcut;
                                Repaint();
                            }
                            else if (Event.current.button == 1)
                            {
                                void Remove()
                                {
                                    shortcut.combination.Set(KeyCode.None);
                                    PWBSettings.UpdateShrotcutsConflictsAndSaveFile();
                                }
                                ContextMenu(shortcut, Remove);
                            }
                        }

                        if (!shortcut.conflicted) return;
                        cellRect.x += cellW - 20;
                        if (shortcut is PWBTwoStepKeyShortcut) cellRect.x -= 20;
                        cellRect.width = 20;

                        string conflictGroup = string.Empty;
                        PWBKeyShortcut conflictedShortcut;
                        if (PWBSettings.GetShortcutConflict(shortcut, out conflictedShortcut))
                        {
                            if ((conflictedShortcut.group & PWBShortcut.Group.GRID) != 0)
                                conflictGroup = "Grid - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.PIN) != 0)
                                conflictGroup = "Pin - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.BRUSH) != 0)
                                conflictGroup = "Brush - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.GRAVITY) != 0)
                                conflictGroup = "Gravity - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.LINE) != 0)
                                conflictGroup = "Line - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.SHAPE) != 0)
                                conflictGroup = "Shape - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.TILING) != 0)
                                conflictGroup = "Tiling - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.ERASER) != 0)
                                conflictGroup = "Eraser - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.REPLACER) != 0)
                                conflictGroup = "Replacer - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.SELECTION) != 0)
                                conflictGroup = "Selection - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.PALETTE) != 0)
                                conflictGroup = "Palette - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.CIRCLE_SELECT) != 0)
                                conflictGroup = "Circle Select - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.EXTRUDE) != 0)
                                conflictGroup = "Extrude - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.MIRROR) != 0)
                                conflictGroup = "Mirror - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.FLOOR) != 0)
                                conflictGroup = "Floor - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.WALL) != 0)
                                conflictGroup = "Wall - ";
                            else if ((conflictedShortcut.group & PWBShortcut.Group.BLOCK) != 0)
                                conflictGroup = "Block - ";
                        }
                        var conflictText = $"Conflict with {conflictGroup}{conflictedShortcut.name}";
                        UnityEditor.EditorGUI.LabelField(cellRect, new GUIContent(warningTexture, conflictText));
                        if (cellRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
                            UnityEditor.EditorUtility.DisplayDialog("Shortcut Conflict", conflictText, "OK");
                    }

                    void MouseShortcutRow(PWBMouseShortcut shortcut, bool scrollWheelOnly = false)
                    {
                        Rect rowRect = new Rect(columnRectPrototype);

                        rowRect.y += columnHeight * (++row);
                        UnityEditor.EditorGUI.DrawRect(rowRect, row % 2 == 0 ? _darkerColor : _lighterColor);

                        Rect columnRect = _multiColumnHeader.GetColumnRect(0);
                        columnRect.y = rowRect.y;

                        var cellRect = _multiColumnHeader.GetCellRect(0, columnRect);
                        cellRect.x += minX;
                        UnityEditor.EditorGUI.LabelField(cellRect, new GUIContent(shortcut.name));

                        columnRect = _multiColumnHeader.GetColumnRect(1);
                        columnRect.y = rowRect.y;

                        cellRect = _multiColumnHeader.GetCellRect(1, columnRect);
                        cellRect.x += minX;

                        if (cellRect.Contains(Event.current.mousePosition)
                           && Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            void Remove()
                            {
                                shortcut.combination.Set(EventModifiers.None, PWBMouseCombination.MouseEvents.NONE);
                                PWBSettings.UpdateShrotcutsConflictsAndSaveFile();
                            }
                            ContextMenu(shortcut, Remove);
                        }

                        cellRect.width = 100;

                        int modId = System.Array.IndexOf(_modifierOptions, shortcut.combination.modifiers);
                        PWBMouseCombination.MouseEvents mouseEvent = shortcut.combination.mouseEvent;
                        void SetCombination()
                        {
                            shortcut.combination.Set(_modifierOptions[modId], mouseEvent);
                            PWBSettings.UpdateShrotcutsConflictsAndSaveFile();
                        }
                        using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                        {
                            modId = UnityEditor.EditorGUI.Popup(cellRect, modId,
                                _modifierDisplayedOptions);
                            if (check.changed)
                            {
                                var combi = new PWBMouseCombination(_modifierOptions[modId], mouseEvent);
                                var combiString = _modifierDisplayedOptions[modId];
                                if (modId > 0) combiString += " + Mouse scroll wheel";
                                if (PWBSettings.shortcuts.CheckMouseConflicts(combi, shortcut, out string conflicts))
                                {
                                    if (BindingConflictDialog(combiString, conflicts)) SetCombination();
                                }
                                else SetCombination();
                            }
                        }

                        cellRect.x += cellRect.width;
                        cellRect.width = 149;
                        if (shortcut.combination.modifiers != EventModifiers.None)
                        {
                            if (scrollWheelOnly) UnityEditor.EditorGUI.LabelField(cellRect, "+ Mouse scroll wheel");
                            else
                            {
                                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                                {
                                    mouseEvent = (PWBMouseCombination.MouseEvents)(UnityEditor.EditorGUI.Popup(cellRect,
                                        (int)mouseEvent - 1, _mouseEventsDisplayedOptions) + 1);
                                    if (check.changed)
                                    {
                                        var combi = new PWBMouseCombination(_modifierOptions[modId], mouseEvent);
                                        var combiString = _modifierDisplayedOptions[modId];
                                        if (modId > 0)
                                            combiString += " + " + _mouseEventsDisplayedOptions[(int)mouseEvent - 1];
                                        if (PWBSettings.shortcuts.CheckMouseConflicts(combi, shortcut, out string conflicts))
                                        {
                                            if (BindingConflictDialog(combiString, conflicts)) SetCombination();
                                        }
                                        else SetCombination();
                                    }
                                }
                            }
                        }

                        if (!shortcut.conflicted) return;
                        cellRect.x += cellRect.width;
                        cellRect.width = 20;
                        UnityEditor.EditorGUI.LabelField(cellRect, new GUIContent(warningTexture));
                    }

                    void EditModeRows()
                    {
                        ShortcutRow(PWBSettings.shortcuts.editModeToggle);
                        ShortcutRow(PWBSettings.shortcuts.editModeDeleteItemAndItsChildren);
                        ShortcutRow(PWBSettings.shortcuts.editModeDeleteItemButNotItsChildren);
                        ShortcutRow(PWBSettings.shortcuts.editModeSelectParent);
                        ShortcutRow(PWBSettings.shortcuts.editModeDuplicate);
                    }
                    void SelectionRows()
                    {
                        ShortcutRow(PWBSettings.shortcuts.selectionRotate90YCW);
                        ShortcutRow(PWBSettings.shortcuts.selectionRotate90YCCW);
                        ShortcutRow(PWBSettings.shortcuts.selectionRotate90XCW);
                        ShortcutRow(PWBSettings.shortcuts.selectionRotate90XCCW);
                        ShortcutRow(PWBSettings.shortcuts.selectionRotate90ZCW);
                        ShortcutRow(PWBSettings.shortcuts.selectionRotate90ZCCW);
                        ShortcutRow(PWBSettings.shortcuts.selectionToggleSpace);
                    }
                    if (_toolbarCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.toolbarPinToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarBrushToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarGravityToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarLineToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarShapeToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarTilingToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarReplacerToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarEraserToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarSelectionToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarExtrudeToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarMirrorToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarFloorToggle);
                        ShortcutRow(PWBSettings.shortcuts.toolbarWallToggle);
                    }
                    else if (_gizmosCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.gizmosToggleInfotext);
                    }
                    else if (_pinCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.pinMoveHandlesUp);
                        ShortcutRow(PWBSettings.shortcuts.pinMoveHandlesDown);
                        ShortcutRow(PWBSettings.shortcuts.pinSelectPrevHandle);
                        ShortcutRow(PWBSettings.shortcuts.pinSelectNextHandle);
                        ShortcutRow(PWBSettings.shortcuts.pinSelectPivotHandle);
                        ShortcutRow(PWBSettings.shortcuts.pinToggleRepeatItem);
                        ShortcutRow(PWBSettings.shortcuts.pinResetScale);

                        ShortcutRow(PWBSettings.shortcuts.pinRotate90YCW);
                        ShortcutRow(PWBSettings.shortcuts.pinRotate90YCCW);
                        ShortcutRow(PWBSettings.shortcuts.pinRotateAStepYCW);
                        ShortcutRow(PWBSettings.shortcuts.pinRotateAStepYCCW);

                        ShortcutRow(PWBSettings.shortcuts.pinRotate90XCW);
                        ShortcutRow(PWBSettings.shortcuts.pinRotate90XCCW);
                        ShortcutRow(PWBSettings.shortcuts.pinRotateAStepXCW);
                        ShortcutRow(PWBSettings.shortcuts.pinRotateAStepXCCW);

                        ShortcutRow(PWBSettings.shortcuts.pinRotate90ZCW);
                        ShortcutRow(PWBSettings.shortcuts.pinRotate90ZCCW);
                        ShortcutRow(PWBSettings.shortcuts.pinRotateAStepZCW);
                        ShortcutRow(PWBSettings.shortcuts.pinRotateAStepZCCW);

                        ShortcutRow(PWBSettings.shortcuts.pinResetRotation);
                        ShortcutRow(PWBSettings.shortcuts.pinSnapRotationToGrid);

                        ShortcutRow(PWBSettings.shortcuts.pinAdd1UnitToSurfDist);
                        ShortcutRow(PWBSettings.shortcuts.pinSubtract1UnitFromSurfDist);
                        ShortcutRow(PWBSettings.shortcuts.pinAdd01UnitToSurfDist);
                        ShortcutRow(PWBSettings.shortcuts.pinSubtract01UnitFromSurfDist);

                        ShortcutRow(PWBSettings.shortcuts.pinResetSurfDist);

                        ShortcutRow(PWBSettings.shortcuts.pinSelectPreviousItem);
                        ShortcutRow(PWBSettings.shortcuts.pinSelectNextItem);

                        ShortcutRow(PWBSettings.shortcuts.pinFlipX);

                        MouseShortcutRow(PWBSettings.shortcuts.pinSelectNextItemScroll, true);
                        MouseShortcutRow(PWBSettings.shortcuts.pinScale);

                        MouseShortcutRow(PWBSettings.shortcuts.pinRotateAroundY);
                        MouseShortcutRow(PWBSettings.shortcuts.pinRotateAroundYSnaped);
                        MouseShortcutRow(PWBSettings.shortcuts.pinRotateAroundX);
                        MouseShortcutRow(PWBSettings.shortcuts.pinRotateAroundXSnaped);
                        MouseShortcutRow(PWBSettings.shortcuts.pinRotateAroundZ);
                        MouseShortcutRow(PWBSettings.shortcuts.pinRotateAroundZSnaped);

                        MouseShortcutRow(PWBSettings.shortcuts.pinSurfDist);
                    }
                    else if (_brushCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.brushUpdatebrushstroke);
                        ShortcutRow(PWBSettings.shortcuts.brushResetRotation);

                        MouseShortcutRow(PWBSettings.shortcuts.brushRadius);
                        MouseShortcutRow(PWBSettings.shortcuts.brushDensity);
                        MouseShortcutRow(PWBSettings.shortcuts.brushRotate);
                    }
                    else if (_gravityCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.brushUpdatebrushstroke);
                        ShortcutRow(PWBSettings.shortcuts.brushResetRotation);

                        ShortcutRow(PWBSettings.shortcuts.gravityAdd1UnitToSurfDist);
                        ShortcutRow(PWBSettings.shortcuts.gravitySubtract1UnitFromSurfDist);
                        ShortcutRow(PWBSettings.shortcuts.gravityAdd01UnitToSurfDist);
                        ShortcutRow(PWBSettings.shortcuts.gravitySubtract01UnitFromSurfDist);

                        MouseShortcutRow(PWBSettings.shortcuts.gravitySurfDist);
                        MouseShortcutRow(PWBSettings.shortcuts.brushRadius);
                        MouseShortcutRow(PWBSettings.shortcuts.brushDensity);
                        MouseShortcutRow(PWBSettings.shortcuts.brushRotate);
                    }
                    else if (_lineCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.lineSelectAllPoints);
                        ShortcutRow(PWBSettings.shortcuts.lineDeselectAllPoints);
                        ShortcutRow(PWBSettings.shortcuts.lineToggleCurve);
                        ShortcutRow(PWBSettings.shortcuts.lineToggleClosed);
                        EditModeRows();
                        ShortcutRow(PWBSettings.shortcuts.lineEditModeTypeToggle);
                        MouseShortcutRow(PWBSettings.shortcuts.lineEditGap);
                    }
                    else if (_shapeCategory)
                    {
                        EditModeRows();
                    }
                    else if (_tilingCategory)
                    {
                        SelectionRows();
                        EditModeRows();
                        MouseShortcutRow(PWBSettings.shortcuts.tilingEditSpacing1);
                        MouseShortcutRow(PWBSettings.shortcuts.tilingEditSpacing2);
                    }
                    else if (_eraserCategory)
                    {
                        MouseShortcutRow(PWBSettings.shortcuts.brushRadius);
                    }
                    else if (_replacerCategory)
                    {
                        MouseShortcutRow(PWBSettings.shortcuts.brushRadius);
                    }
                    else if (_selectionCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.selectionTogglePositionHandle);
                        ShortcutRow(PWBSettings.shortcuts.selectionToggleRotationHandle);
                        ShortcutRow(PWBSettings.shortcuts.selectionToggleScaleHandle);
                        ShortcutRow(PWBSettings.shortcuts.selectionEditCustomHandle);
                        ShortcutRow(PWBSettings.shortcuts.selectionMoveToMousePosition);
                        SelectionRows();
                    }
                    else if (_floorCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.floorRotate90YCW);
                    }
                    else if (_wallCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.wallHalfTurn);
                    }
                    else if (_blockCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.blockPreviewRotate90YCW);
                        ShortcutRow(PWBSettings.shortcuts.blockRotate90YCW);
                        ShortcutRow(PWBSettings.shortcuts.blockRotate90YCCW);
                        ShortcutRow(PWBSettings.shortcuts.blockRotate90XCW);
                        ShortcutRow(PWBSettings.shortcuts.blockRotate90XCCW);
                        ShortcutRow(PWBSettings.shortcuts.blockRotate90ZCW);
                        ShortcutRow(PWBSettings.shortcuts.blockRotate90ZCCW);
                    }
                    else if (_circleSelectCategory)
                    {
                        MouseShortcutRow(PWBSettings.shortcuts.brushRadius);
                    }
                    else if (_gridCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.gridEnableShortcuts);
                        ShortcutRow(PWBSettings.shortcuts.gridToggle);
                        ShortcutRow(PWBSettings.shortcuts.gridToggleSnaping);
                        ShortcutRow(PWBSettings.shortcuts.gridToggleLock);
                        ShortcutRow(PWBSettings.shortcuts.gridSetOriginPosition);
                        ShortcutRow(PWBSettings.shortcuts.gridSetOriginRotation);
                        ShortcutRow(PWBSettings.shortcuts.gridSetSize);
                        ShortcutRow(PWBSettings.shortcuts.gridFrameOrigin);
                        ShortcutRow(PWBSettings.shortcuts.gridTogglePositionHandle);
                        ShortcutRow(PWBSettings.shortcuts.gridToggleRotationHandle);
                        ShortcutRow(PWBSettings.shortcuts.gridToggleSpacingHandle);
                        ShortcutRow(PWBSettings.shortcuts.gridMoveOriginUp);
                        ShortcutRow(PWBSettings.shortcuts.gridMoveOriginDown);
                        ShortcutRow(PWBSettings.shortcuts.gridNextOrigin);
                        ShortcutRow(PWBSettings.shortcuts.gridMoveOriginToMousePos);
                    }
                    else if (_snapCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.snapToggleBoundsSnapping);
                    }
                    else if (_paletteCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.paletteDeleteBrush);
                        ShortcutRow(PWBSettings.shortcuts.palettePreviousBrush);
                        ShortcutRow(PWBSettings.shortcuts.paletteNextBrush);
                        ShortcutRow(PWBSettings.shortcuts.palettePreviousPalette);
                        ShortcutRow(PWBSettings.shortcuts.paletteNextPalette);
                        ShortcutRow(PWBSettings.shortcuts.palettePickBrush);
                        ShortcutRow(PWBSettings.shortcuts.paletteReplaceSceneSelection);
                        MouseShortcutRow(PWBSettings.shortcuts.paletteNextBrushScroll, true);
                        MouseShortcutRow(PWBSettings.shortcuts.paletteNextPaletteScroll, true);
                    }
                    else if (_uiCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.closeAllWindows);
                        ShortcutRow(PWBSettings.shortcuts.toggleOverlays);
                    }
                    else if (_toolModesCategory)
                    {
                        ShortcutRow(PWBSettings.shortcuts.toolModesMoveSymmetryOriginToMousePos);
                    }
                    GUILayout.Space((row + 2) * columnHeight);
                    if (_gridCategory && !PWBSettings.shortcuts.gridEnableShortcuts.combination.isDissabled())
                    {
                        UnityEditor.EditorGUILayout.HelpBox("These shortcuts work in two steps."
                        + "\nFirst you have to activate the shortcuts with "
                        + PWBSettings.shortcuts.gridEnableShortcuts.combination
                        + ".\nFor example to toggle the grid you have to press "
                        + PWBSettings.shortcuts.gridEnableShortcuts.combination + " and then "
                        + PWBSettings.shortcuts.gridToggle.combination + "."
                        + "\nUnchecking disables the first step.",
                       UnityEditor.MessageType.Info);
                    }
                }
            }
        }

        private bool BindingConflictDialog(string combi, string conflicts)
            => UnityEditor.EditorUtility.DisplayDialog("Binding Conflict", "The key " + combi
                + " is already assigned to: \n" + conflicts + "\n Do you want to create the conflict?",
                "Create Conflict", "Cancel");
    }
}
#pragma warning restore UDR0001
