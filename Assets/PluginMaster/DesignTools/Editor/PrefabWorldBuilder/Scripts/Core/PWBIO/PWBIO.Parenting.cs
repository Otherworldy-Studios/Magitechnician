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
using UnityEngine;

namespace PluginMaster
{
    public static partial class PWBIO
    {
        private const string PWB_OBJ_NAME = "Prefab World Builder";

        private static Transform _autoParent = null;
        private static System.Collections.Generic.Dictionary<string, Transform> _subParents
            = new System.Collections.Generic.Dictionary<string, Transform>();

        public static void ResetAutoParent() => _autoParent = null;

        private const string NO_PALETTE_NAME = "<#PALETTE@>";
        private const string NO_TOOL_NAME = "<#TOOL@>";
        private const string NO_OBJ_ID = "<#ID@>";
        private const string NO_BRUSH_NAME = "<#BRUSH@>";
        private const string NO_PREFAB_NAME = "<#PREFAB@>";
        private const string PARENT_KEY_SEPARATOR = "<#@>";
        public static Transform GetParent(IPaintToolSettings settings, string prefabName,
            bool create, Transform surface, string toolObjectId = "")
        {
            IToolParentingSettings parentingSettings = PWBCore.staticData.globalParentingSettings;
            if (settings.overwriteParentingSettings) parentingSettings = settings.GetParentingSettings();
            if (!create) return parentingSettings.parent;

            var isPersistent = ToolController.IsCurrentToolPersistent();
            if (parentingSettings.autoCreateParent)
            {
                if (isInPrefabMode)
                {
                    var root = prefabStage.prefabContentsRoot;
                    var pwbObj = root.transform.Find(PWB_OBJ_NAME);
                    if (pwbObj == null)
                    {
                        _autoParent = new GameObject(PWB_OBJ_NAME).transform;
                        _autoParent.SetParent(root.transform);
                    }
                    else _autoParent = pwbObj.transform;
                }
                else
                {
                    var pwbObj = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                        .GetRootGameObjects().FirstOrDefault(o => o.name == PWB_OBJ_NAME);
                    if (pwbObj == null) _autoParent = new GameObject(PWB_OBJ_NAME).transform;
                    else _autoParent = pwbObj.transform;
                }
            }
            else if (parentingSettings.setLastSelectedAsParent) _autoParent = UnityEditor.Selection.activeTransform;
            else if (parentingSettings.setSurfaceAsParent) _autoParent = surface;
            else _autoParent = parentingSettings.parent;

            if (!parentingSettings.createSubparentPerPalette && !parentingSettings.createSubparentPerTool && !isPersistent
                && !parentingSettings.createSubparentPerBrush && !parentingSettings.createSubparentPerPrefab)
                return _autoParent;
#if UNITY_6000_3_OR_NEWER
            var autoParentId = _autoParent == null ? EntityId.None : _autoParent.gameObject.GetEntityId();
#else
            var autoParentId = _autoParent == null ? -1 : _autoParent.gameObject.GetInstanceID();
#endif
#if UNITY_6000_3_OR_NEWER
            string GetSubParentKey(EntityId parentId,
#else
            string GetSubParentKey(int parentId,
#endif
                string palette = NO_PALETTE_NAME, string tool = NO_TOOL_NAME, string id = NO_OBJ_ID,
                string brush = NO_BRUSH_NAME, string prefab = NO_PREFAB_NAME)
                => parentId.ToString() + PARENT_KEY_SEPARATOR + palette + PARENT_KEY_SEPARATOR
                + tool + PARENT_KEY_SEPARATOR + id + PARENT_KEY_SEPARATOR + brush
                + PARENT_KEY_SEPARATOR + prefab;

            var paletteName = parentingSettings.createSubparentPerPalette && PaletteManager.selectedPalette != null
                ? PaletteManager.selectedPalette.name : NO_PALETTE_NAME;
            var toolName = parentingSettings.createSubparentPerTool
                ? ToolController.GetToolFromSettings(settings as IToolSettings).ToString() : NO_TOOL_NAME;
            var objId = string.IsNullOrEmpty(toolObjectId) ? NO_OBJ_ID : toolObjectId;
            var brushName = parentingSettings.createSubparentPerBrush && PaletteManager.selectedBrush != null
                ? PaletteManager.selectedBrush.name : NO_BRUSH_NAME;
            var prefabKey = parentingSettings.createSubparentPerPrefab ? prefabName : NO_PREFAB_NAME;

            string subParentKey = GetSubParentKey(autoParentId, paletteName, toolName, objId, brushName, prefabKey);

            create = !(_subParents.ContainsKey(subParentKey));
            if (!create && _subParents[subParentKey] == null) create = true;
            if (!create) return _subParents[subParentKey];

            Transform CreateSubParent(string key, string name, Transform transformParent)
            {
                Transform subParentTransform = null;
                var subParentIsEmpty = true;
                if (transformParent != null)
                {
                    subParentTransform = transformParent.Find(name);
                    if (subParentTransform != null)
                        subParentIsEmpty = subParentTransform.GetComponents<Component>().Length == 1;
                    if (isInPrefabMode && transformParent != prefabStage.prefabContentsRoot.transform
                        && transformParent.parent == null)
                        transformParent.SetParent(prefabStage.prefabContentsRoot.transform);
                }
                else if (isInPrefabMode) transformParent = prefabStage.prefabContentsRoot.transform;

                if (subParentTransform == null || !subParentIsEmpty)
                {
                    var obj = new GameObject(name);
                    var subParent = obj.transform;
                    subParent.SetParent(transformParent);
                    subParent.localPosition = Vector3.zero;
                    subParent.localRotation = Quaternion.identity;
                    subParent.localScale = Vector3.one;
                    if (_subParents.ContainsKey(key)) _subParents[key] = subParent;
                    else _subParents.Add(key, subParent);
                    return subParent;
                }
                return subParentTransform;
            }

            var parent = _autoParent;
            void CreateSubParentIfDoesntExist(string name, string palette = NO_PALETTE_NAME,
                string tool = NO_TOOL_NAME, string id = NO_OBJ_ID, string brush = NO_BRUSH_NAME,
                string prefab = NO_PREFAB_NAME)
            {
                var key = GetSubParentKey(autoParentId, palette, tool, id, brush, prefab);
                var keyExist = _subParents.ContainsKey(key);
                var subParent = keyExist ? _subParents[key] : null;
                if (subParent != null) parent = subParent;
                if (!keyExist || subParent == null) parent = CreateSubParent(key, name, parent);
            }

            var keySplitted = subParentKey.Split(new string[] { PARENT_KEY_SEPARATOR },
                System.StringSplitOptions.None);
            var keyPlaletteName = keySplitted[1];
            var keyToolName = keySplitted[2];
            var keyToolObjId = keySplitted[3];
            var keyBrushName = keySplitted[4];
            var keyPrefabName = keySplitted[5];

            if (keyPlaletteName != NO_PALETTE_NAME)
                CreateSubParentIfDoesntExist(keyPlaletteName, keyPlaletteName);
            if (keyToolName != NO_TOOL_NAME)
                CreateSubParentIfDoesntExist(keyToolName, keyPlaletteName, keyToolName);
            if (keyToolObjId != NO_OBJ_ID)
                CreateSubParentIfDoesntExist(keyToolObjId, keyPlaletteName, keyToolName, keyToolObjId);
            if (keyBrushName != NO_BRUSH_NAME)
                CreateSubParentIfDoesntExist(keyBrushName, keyPlaletteName, keyToolName,
                    keyToolObjId, keyBrushName);
            if (keyPrefabName != NO_PREFAB_NAME)
                CreateSubParentIfDoesntExist(keyPrefabName, keyPlaletteName,
                    keyToolName, keyToolObjId, keyBrushName, keyPrefabName);
            return parent;
        }
    }
}
#pragma warning restore UDR0001
