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
using System.Linq;

namespace PluginMaster
{
    public class FilterByFolderWindow : UnityEditor.EditorWindow
    {
        private class FolderNode
        {
            public string path { get; set; }
            public string name { get; set; }
            public int prefabCount { get; set; }
            public bool isExpanded { get; set; } = true;
            public bool isVisible { get; set; } = true;
            public System.Collections.Generic.List<FolderNode> subfolders { get; set; }
                = new System.Collections.Generic.List<FolderNode>();
            public FolderNode parent { get; set; } = null;
        }

        private System.Collections.Generic.List<FolderNode> _rootFolders = new System.Collections.Generic.List<FolderNode>();
        private System.Collections.Generic.HashSet<string> _prefabPaths = new System.Collections.Generic.HashSet<string>();

        private GUISkin _skin = null;
        private GUIStyle _itemBtnStyle = null;
        private GUIContent _showIcon = null;
        private GUIContent _hideIcon = null;
        private GUIContent _showIconLight = null;
        private GUIContent _hideIconLight = null;
        private GUIContent _expandedIcon = null;
        private GUIContent _collapsedIcon = null;
        public static void ShowWindow()
        {
            var window = GetWindow<FilterByFolderWindow>("Filter By Folder");
            window.Show();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Domain reload", "UDR0004:Domain Reload Analyzer")]
        private void OnEnable()
        {
            LoadStyles();
            LoadIcons();
            LoadFolderHierarchy();
            PaletteManager.OnPaletteChanged -= OnPaletteChanged;
            PaletteManager.OnPaletteChanged += OnPaletteChanged;
            PaletteData.OnPaletteSaved -= OnPaletteChanged;
            PaletteData.OnPaletteSaved += OnPaletteChanged;
        }

        private void OnPaletteChanged()
        {
            LoadFolderHierarchy();
            Repaint();
        }

        private void LoadIcons()
        {
            _showIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/ShowAll"));
            _hideIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/HideAll"));
            _showIconLight = new GUIContent(Resources.Load<Texture2D>("Sprites/LightTheme/ShowAll"));
            _hideIconLight = new GUIContent(Resources.Load<Texture2D>("Sprites/LightTheme/HideAll"));
            _expandedIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Expanded"));
            _collapsedIcon = new GUIContent(Resources.Load<Texture2D>("Sprites/Collapsed"));
        }

        private void LoadStyles()
        {
            _skin = Resources.Load<GUISkin>("PWBSkin");
            _itemBtnStyle = _skin.GetStyle("EyeButton");
        }

        private GUIContent showIcon => UnityEditor.EditorGUIUtility.isProSkin ? _showIcon : _showIconLight;
        private GUIContent hideIcon => UnityEditor.EditorGUIUtility.isProSkin ? _hideIcon : _hideIconLight;


        private void LoadFolderHierarchy()
        {
            _rootFolders.Clear();
            _prefabPaths.Clear();
            var brushes = PaletteManager.selectedPalette.brushes;            
            foreach (var brush in brushes)
            {
                var items = brush.items;
                foreach (var item in items)
                {
                    if (item.prefab == null) continue;
                    if (_prefabPaths.Contains(item.prefabPath)) continue;
                    _prefabPaths.Add(item.prefabPath);
                }
            }
            string[] rootFolders = UnityEditor.AssetDatabase.GetSubFolders("Assets");
            var hiddenFolders = PrefabPalette.GetHiddenFolders();
            foreach (var folder in rootFolders)
            {
                var prefabCount = _prefabPaths.Count(prefabPath => prefabPath.StartsWith(folder));
                var rootNode = new FolderNode { path = folder, name = System.IO.Path.GetFileName(folder) };
                rootNode.prefabCount = prefabCount;
                if (prefabCount <= 0) continue;
                PopulateFolder(rootNode, hiddenFolders, parent: null);
                _rootFolders.Add(rootNode);
            }
        }

        private void PopulateFolder(FolderNode folderNode, string[] hiddenFolders, FolderNode parent)
        {
            folderNode.parent = parent;
            if (hiddenFolders.Any(f => folderNode.path == f)) folderNode.isVisible = false;
            string[] subfolders = UnityEditor.AssetDatabase.GetSubFolders(folderNode.path);
            foreach (var subfolder in subfolders)
            {
                var prefabCount = _prefabPaths.Count(prefabPath => prefabPath.StartsWith(subfolder));
                var subfolderNode = new FolderNode
                {
                    path = subfolder,
                    name = System.IO.Path.GetFileName(subfolder),
                    prefabCount = prefabCount,
                    parent = folderNode
                };
                if (prefabCount <= 0) continue;
                if (hiddenFolders.Any(f => subfolderNode.path == f)) subfolderNode.isVisible = false;
                PopulateFolder(subfolderNode, hiddenFolders, folderNode);
                folderNode.subfolders.Add(subfolderNode);
            }
        }

        private Vector2 _scrollPosition = Vector2.zero;

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(UnityEditor.EditorStyles.helpBox))
            {
                using (var scrollView = new UnityEditor.EditorGUILayout.ScrollViewScope(_scrollPosition,
                    alwaysShowHorizontal: false, alwaysShowVertical: false,
                    GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, background: GUIStyle.none))
                {
                    _scrollPosition = scrollView.scrollPosition;
                    foreach (var folder in _rootFolders) DrawFolder(folder, 0);
                }
            }
            DrawShowHideAllButtons();
        }

        private void DrawShowHideAllButtons()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Show All", UnityEditor.EditorStyles.miniButtonLeft, GUILayout.Width(70)))
                    foreach (var folder in _rootFolders) SetVisibility(folder, isVisible: true);
                GUILayout.Space(10);
                if (GUILayout.Button("Hide All", UnityEditor.EditorStyles.miniButtonRight, GUILayout.Width(70)))
                    foreach (var folder in _rootFolders) SetVisibility(folder, isVisible: false);
            }
        }

        private void DrawFolder(FolderNode folderNode, int indentLevel)
        {
            if (folderNode == null) return;

            using (new GUILayout.HorizontalScope())
            {
                for (int i = 0; i < indentLevel; i++) UnityEditor.EditorGUILayout.Space(14, expand: false);

                GUIContent expandCollapseIcon = folderNode.isExpanded ? _expandedIcon : _collapsedIcon;
                using (new UnityEditor.EditorGUI.DisabledGroupScope(folderNode.subfolders.Count == 0))
                {
                    if (GUILayout.Button(expandCollapseIcon, _itemBtnStyle) && folderNode.subfolders.Count > 0)
                        folderNode.isExpanded = !folderNode.isExpanded;
                }
                GUILayout.Space(-4);
                GUIContent eyeContent = folderNode.isVisible ? showIcon : hideIcon;
                if (GUILayout.Button(eyeContent, _itemBtnStyle))
                {
                    if (!folderNode.isVisible) MakeParentsVisible(folderNode);
                    SetVisibility(folderNode, !folderNode.isVisible);
                }

                UnityEditor.EditorGUILayout.LabelField(folderNode.name,
                    GUILayout.Width(UnityEditor.EditorStyles.label.CalcSize(new GUIContent(folderNode.name)).x));

                GUILayout.FlexibleSpace();

                var labelStyle = new GUIStyle(UnityEditor.EditorStyles.label);
                labelStyle.alignment = TextAnchor.UpperRight;
                var prefabCountText = folderNode.prefabCount.ToString();
                UnityEditor.EditorGUILayout.LabelField(prefabCountText, labelStyle,
                    GUILayout.Width(UnityEditor.EditorStyles.label.CalcSize(new GUIContent(prefabCountText)).x));
                GUILayout.Space(4);
            }
            if (folderNode.isExpanded)
                foreach (var subfolder in folderNode.subfolders) DrawFolder(subfolder, indentLevel + 1);
        }

        private void SetVisibility(FolderNode folderNode, bool isVisible)
        {
            folderNode.isVisible = isVisible;
            foreach (var subfolder in folderNode.subfolders) SetVisibility(subfolder, isVisible);
            var hiddenFolders = new System.Collections.Generic.HashSet<string>();
            void AddHiddenFolders(FolderNode folderNode)
            {
                if (!folderNode.isVisible) hiddenFolders.Add(folderNode.path);
                foreach (var subfolder in folderNode.subfolders)
                {
                    if (!subfolder.isVisible) hiddenFolders.Add(subfolder.path);
                    AddHiddenFolders(subfolder);
                }
            }
            foreach (var rootFolder in _rootFolders) AddHiddenFolders(rootFolder);
            PrefabPalette.SetHiddenFolders(hiddenFolders.ToArray());
        }

        private void MakeParentsVisible(FolderNode folderNode)
        {
            var parent = folderNode.parent;
            while (parent != null)
            {
                parent.isVisible = true;
                parent = parent.parent;
            }
        }
    }
}
#pragma warning restore UDR0001
