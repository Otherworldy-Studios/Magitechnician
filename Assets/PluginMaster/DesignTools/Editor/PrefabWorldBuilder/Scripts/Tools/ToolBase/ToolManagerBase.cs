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
    public static class ToolProfile
    {
        public const string DEFAULT = "Default";
    }

    public interface IToolController
    {
        string selectedProfileName { get; set; }
        string[] profileNames { get; }
        void SaveProfile();
        void SaveProfileAs(string name);
        void DeleteProfile();
        void Revert();
        void FactoryReset();
    }

    [System.Serializable]
    public class ToolControllerBase<TOOL_SETTINGS, TOOL_MANAGER> : IToolController, ISerializationCallbackReceiver
        where TOOL_SETTINGS : IToolSettings, new()
        where TOOL_MANAGER : ToolControllerBase<TOOL_SETTINGS, TOOL_MANAGER>, new()
    {

        protected static TOOL_MANAGER _instance = null;
        private static System.Collections.Generic.Dictionary<string, TOOL_SETTINGS> _staticProfiles
            = new System.Collections.Generic.Dictionary<string, TOOL_SETTINGS>
        { { ToolProfile.DEFAULT, new TOOL_SETTINGS() } };
        [SerializeField] private string[] _profileKeys = { ToolProfile.DEFAULT };
        [SerializeField] private TOOL_SETTINGS[] _profileValues = { new TOOL_SETTINGS() };
        private static string _staticSelectedProfileName = ToolProfile.DEFAULT;
        [SerializeField] private string _selectedProfileName = _staticSelectedProfileName;
        private static TOOL_SETTINGS _staticUnsavedProfile = new TOOL_SETTINGS();
        [SerializeField] private TOOL_SETTINGS _unsavedProfile = _staticUnsavedProfile;

        protected ToolControllerBase() { }

        public static TOOL_MANAGER instance
        {
            get
            {
                if (_instance == null) _instance = new TOOL_MANAGER();
                return _instance;
            }
        }
        public static TOOL_SETTINGS settings => _staticUnsavedProfile;

        private void UpdateUnsaved()
        {
            var tool = ToolController.GetToolFromSettings(settings);
            if (ToolController.current == ToolController.Tool.NONE || tool != ToolController.current) return;
            if (!_staticProfiles.ContainsKey(_staticSelectedProfileName))
                _staticSelectedProfileName = ToolProfile.DEFAULT;
            _staticUnsavedProfile.Copy(_staticProfiles[_staticSelectedProfileName]);
        }

        public string selectedProfileName
        {
            get => _staticSelectedProfileName;
            set
            {
                if (_staticSelectedProfileName == value) return;
                _staticSelectedProfileName = value;
                _selectedProfileName = value;
                UpdateUnsaved();
                _staticUnsavedProfile.DataChanged();
            }
        }

        public string[] profileNames => _staticProfiles.Keys.ToArray();
        public void SaveProfile()
        {
            _staticProfiles[_staticSelectedProfileName].Copy(_staticUnsavedProfile);
            PWBCore.staticData.SaveAndUpdateVersion();
        }
        public void SaveProfileAs(string name)
        {
            if (!_staticProfiles.ContainsKey(name))
            {
                var newProfile = new TOOL_SETTINGS();
                newProfile.Copy(_unsavedProfile);
                _staticProfiles.Add(name, newProfile);
            }
            else _staticProfiles[name].Copy(_staticUnsavedProfile);
            _staticSelectedProfileName = name;
            UpdateUnsaved();
            _staticUnsavedProfile.DataChanged();
            PWBCore.staticData.SaveAndUpdateVersion();
        }
        public void DeleteProfile()
        {
            if (_staticSelectedProfileName == ToolProfile.DEFAULT) return;
            _staticProfiles.Remove(_staticSelectedProfileName);
            _staticSelectedProfileName = ToolProfile.DEFAULT;
            _staticUnsavedProfile.Copy(_staticProfiles[ToolProfile.DEFAULT]);
            _staticUnsavedProfile.DataChanged();
            PWBCore.staticData.SaveAndUpdateVersion();
        }
        public void Revert()
        {
            UpdateUnsaved();
            _staticUnsavedProfile.DataChanged();
            PWBCore.staticData.SaveAndUpdateVersion();
        }

        public void FactoryReset()
        {
            if (_staticUnsavedProfile == null) _staticUnsavedProfile = new TOOL_SETTINGS();
            else
            {
                var newSettings = new TOOL_SETTINGS();
                _staticUnsavedProfile.Copy(newSettings);
            }
            _staticUnsavedProfile.DataChanged();
            PWBCore.staticData.SaveAndUpdateVersion();
        }

        public void CopyToolSettings(TOOL_SETTINGS value) => _staticUnsavedProfile.Copy(value);
        public virtual void OnBeforeSerialize()
        {
            _selectedProfileName = _staticSelectedProfileName;
            _profileKeys = _staticProfiles.Keys.ToArray();
            _profileValues = _staticProfiles.Values.ToArray();
        }

        public virtual void OnAfterDeserialize()
        {
            _staticSelectedProfileName = _selectedProfileName;
            if (_profileKeys.Length > 1)
            {
                _staticProfiles.Clear();
                for (int i = 0; i < _profileKeys.Length; ++i) _staticProfiles.Add(_profileKeys[i], _profileValues[i]);
            }
        }
    }

    public interface IPersistentToolController
    {
        bool applyBrushToExisting { get; set; }
        IPersistentData[] GetItems();
        void ToggleItemsVisibility();
        void DeletePersistentItem(long itemId, bool deleteObjects, bool registerUndo = true);
        void DeselectAllItems();
        IPersistentData Duplicate(long itemId);
        string GetToolName();
    }

    [System.Serializable]
    public class PersistentToolControllerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA, SCENE_DATA, TOOL_MANAGER>
        : ToolControllerBase<TOOL_SETTINGS, TOOL_MANAGER>, IPersistentToolController
        where TOOL_NAME : IToolName, new()
        where TOOL_SETTINGS : IToolSettings, new()
        where CONTROL_POINT : ControlPoint, new()
        where TOOL_DATA : PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>, new()
        where TOOL_MANAGER : ToolControllerBase<TOOL_SETTINGS, TOOL_MANAGER>, new()
        where SCENE_DATA : SceneData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA>, new()
    {

        private static System.Collections.Generic.List<SCENE_DATA> _staticSceneItems = null;
        [SerializeField] private System.Collections.Generic.List<SCENE_DATA> _sceneItems = _staticSceneItems;

        private static bool _staticShowPreexistingElements = true;
        [SerializeField] private bool _showPreexistingElements = _staticShowPreexistingElements;

        private static bool _staticApplyBrushToExisting = false;
        [SerializeField] private bool _applyBrushToExisting = _staticApplyBrushToExisting;

        private static IPersistentData.Visibility _itemsVisibility = IPersistentData.Visibility.SHOW_ALL;

        protected PersistentToolControllerBase() { }
        public new static TOOL_MANAGER instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TOOL_MANAGER();
                return _instance;
            }
        }

        public void AddPersistentItem(string sceneGUID, TOOL_DATA data)
        {
            if (_staticSceneItems == null)
                _staticSceneItems = new System.Collections.Generic.List<SCENE_DATA>();
            var sceneItem = _staticSceneItems.Find(i => i.sceneGUID == sceneGUID);
            if (sceneItem == null)
            {
                sceneItem = new SCENE_DATA();
                sceneItem.sceneGUID = sceneGUID;
                _staticSceneItems.Add(sceneItem);
            }
            if (sceneItem.items != null)
            {
                var item = sceneItem.items.Find(i => i.id == data.id);
                if (item != null) return;
            }
            sceneItem.AddItem(data);
            PWBCore.staticData.SaveAndUpdateVersion();
        }

        public string GetToolName() => (new TOOL_NAME()).value;
        public TOOL_DATA[] GetPersistentItems()
        {
            var items = new System.Collections.Generic.List<TOOL_DATA>();

            if (_staticSceneItems == null) return items.ToArray();

            if (PWBIO.isInPrefabMode)
            {
                var guid = UnityEditor.AssetDatabase.AssetPathToGUID(PWBIO.prefabStage.assetPath);
                var data = _staticSceneItems.Find(item => item.sceneGUID == guid);
                if (data == null) _staticSceneItems.Remove(data);
                else items.AddRange(data.items);
                return items.ToArray();
            }

            var openedSceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < openedSceneCount; ++i)
            {
                string sceneGUID = UnityEditor.AssetDatabase.AssetPathToGUID
                    (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path);
                var data = _staticSceneItems.Find(item => item.sceneGUID == sceneGUID);
                if (data == null)
                {
                    _staticSceneItems.Remove(data);
                    continue;
                }
                items.AddRange(data.items);
            }
            return items.ToArray();
        }

        public IPersistentData[] GetItems() => GetPersistentItems();

        public void DeselectAllItems()
        {
            var items = GetItems();
            foreach (var item in items)
            {
                item.isSelected = false;
                item.ClearSelection();
            }
        }

        public IPersistentData Duplicate(long itemId)
        {
            var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            var sceneGUID = UnityEditor.AssetDatabase.AssetPathToGUID(scenePath);
            var sceneItem = _staticSceneItems.Find(i => i.sceneGUID == sceneGUID);

            var source = GetItem(itemId);
            var clone = new TOOL_DATA();
            clone.Duplicate(source);

            sceneItem.AddItem(clone);
            PWBCore.staticData.SaveAndUpdateVersion();
            return clone;
        }
        public void ToggleItemsVisibility()
        {
            switch (_itemsVisibility)
            {
                case IPersistentData.Visibility.SHOW_ALL: _itemsVisibility = IPersistentData.Visibility.SHOW_OBJECTS; break;
                case IPersistentData.Visibility.SHOW_OBJECTS: _itemsVisibility = IPersistentData.Visibility.HIDE_ALL; break;
                case IPersistentData.Visibility.HIDE_ALL: _itemsVisibility = IPersistentData.Visibility.SHOW_ALL; break;
            }
            var items = GetItems();
            foreach (var item in items) item.visibility = _itemsVisibility;
        }

        public bool ReplaceObject(GameObject target, GameObject obj)
        {
            var items = GetPersistentItems();
            foreach (var item in items)
                if (item.ReplaceObject(target, obj)) return true;
            return false;
        }

        public void RemovePersistentItem(long itemId)
        {
            foreach (var item in _staticSceneItems) item.RemoveItemData(itemId);
            PWBCore.staticData.SaveAndUpdateVersion();
        }
        public void DeletePersistentItem(long itemId, bool deleteObjects, bool registerUndo = true)
        {
            if (registerUndo) ToolProperties.RegisterUndo("Delete Item");
            var parents = new System.Collections.Generic.HashSet<GameObject>();
            foreach (var item in _staticSceneItems)
            {
                var itemParents = item.GetParents(itemId);
                foreach (var parent in itemParents)
                    if (!parents.Contains(parent)) parents.Add(parent);
                item.DeleteItemData(itemId, deleteObjects);
            }

            foreach (var parent in parents)
            {
                var components = parent.GetComponentsInChildren<Component>();
                if (components.Length == 1)
                {
                    if (registerUndo) UnityEditor.Undo.DestroyObjectImmediate(parent);
                    else Object.DestroyImmediate(parent);
                }
            }
            PWBCore.staticData.SaveAndUpdateVersion();
        }
        public TOOL_DATA GetItem(long itemId)
        {
            var items = GetPersistentItems();
            foreach (var item in items)
                if (item.id == itemId) return item;
            return null;
        }

        public TOOL_DATA GetItem(string hexItemId)
        {
            var splittedId = hexItemId.Split('_');
            if (splittedId.Length != 2) return null;
            var provider = new System.Globalization.CultureInfo("en-US");
            if (long.TryParse(splittedId[1], System.Globalization.NumberStyles.AllowHexSpecifier, provider, out long itemId))
                return GetItem(itemId);
            return null;
        }

        public bool showPreexistingElements
        {
            get => _staticShowPreexistingElements;
            set
            {
                if (_staticShowPreexistingElements == value) return;
                _staticShowPreexistingElements = value;
                _showPreexistingElements = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        public bool applyBrushToExisting
        {
            get => _staticApplyBrushToExisting;
            set
            {
                if (_staticApplyBrushToExisting == value) return;
                _staticApplyBrushToExisting = value;
                _applyBrushToExisting = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            _sceneItems = _staticSceneItems;
            _showPreexistingElements = _staticShowPreexistingElements;
            _applyBrushToExisting = _staticApplyBrushToExisting;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            _staticSceneItems = _sceneItems;
            _staticShowPreexistingElements = _showPreexistingElements;
            _staticApplyBrushToExisting = _applyBrushToExisting;
        }
    }
}
#pragma warning restore UDR0001
