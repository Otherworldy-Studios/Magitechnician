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
    public partial class PWBData
    {
        public bool VersionUpdate()
        {
            string currentText = null;
            PWBDataVersion dataVersion = null;

            bool ReadTextAndGetVersion()
            {
                currentText = ReadDataText();
                if (currentText == null) return false;
                try
                {
                    dataVersion = JsonUtility.FromJson<PWBDataVersion>(currentText);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
                if (dataVersion == null)
                {
                    DeleteFile();
                    return false;
                }
                return true;
            }

            if (!ReadTextAndGetVersion()) return false;
            var updated = false;
            if (dataVersion.IsOlderThan("2.9"))
            {
                var v2_8_data = JsonUtility.FromJson<V2_8_PWBData>(currentText);
                if (v2_8_data._paletteManager._paletteData.Length > 0) PaletteManager.ClearPaletteList();
                foreach (var paletteData in v2_8_data._paletteManager._paletteData)
                {
                    paletteData.version = VERSION;
                    PaletteManager.AddPalette(paletteData, save: true);
                }
                var textAssets = Resources.LoadAll<TextAsset>(FILE_NAME);
                for (int i = 0; i < textAssets.Length; ++i)
                {
                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(textAssets[i]);
                    UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                }
                PWBCore.staticData.Save(updateVersion: false);
                PrefabPalette.RepaintWindow();
                updated = true;
            }
            if (updated) if (!ReadTextAndGetVersion()) return false;
            if (dataVersion.IsOlderThan("4.1"))
            {
                var v4_0_data = JsonUtility.FromJson<V4_0_PWBData>(currentText);
                var v4_0_lineSceneItems = v4_0_data._lineManager._sceneItems;
                foreach (var v4_0_sceneItem in v4_0_lineSceneItems)
                {
                    var v4_0_items = v4_0_sceneItem._items;
                    foreach (var v4_0_item in v4_0_items)
                    {
                        var data = LineManager.instance.GetItem(v4_0_item._id);
                        if (data == null) continue;
                        var v4_0_poses = v4_0_item._objectPoses;
                        data.RemoveAllPoses();
                        foreach (var v4_0_pose in v4_0_poses)
                        {
                            var pose = new ObjectPose(v4_0_pose._position, v4_0_pose._localRotation, v4_0_pose._localScale);
                            data.AddPose(v4_0_pose._id, pose);
                        }
                    }
                }
                var v4_0_shapeSceneItems = v4_0_data._shapeManager._sceneItems;
                foreach (var v4_0_sceneItem in v4_0_shapeSceneItems)
                {
                    var v4_0_items = v4_0_sceneItem._items;
                    foreach (var v4_0_item in v4_0_items)
                    {
                        var data = ShapeManager.instance.GetItem(v4_0_item._id);
                        if (data == null) continue;
                        var v4_0_poses = v4_0_item._objectPoses;
                        data.RemoveAllPoses();
                        foreach (var v4_0_pose in v4_0_poses)
                        {
                            var pose = new ObjectPose(v4_0_pose._position, v4_0_pose._localRotation, v4_0_pose._localScale);
                            data.AddPose(v4_0_pose._id, pose);
                        }
                    }
                }
                var v4_0_tilingSceneItems = v4_0_data._tilingManager._sceneItems;
                foreach (var v4_0_sceneItem in v4_0_tilingSceneItems)
                {
                    var v4_0_items = v4_0_sceneItem._items;
                    foreach (var v4_0_item in v4_0_items)
                    {
                        var data = TilingManager.instance.GetItem(v4_0_item._id);
                        if (data == null) continue;
                        var v4_0_poses = v4_0_item._objectPoses;
                        data.RemoveAllPoses();
                        foreach (var v4_0_pose in v4_0_poses)
                        {
                            var pose = new ObjectPose(v4_0_pose._position, v4_0_pose._localRotation, v4_0_pose._localScale);
                            data.AddPose(v4_0_pose._id, pose);
                        }
                    }
                }
                PWBCore.staticData.Save(updateVersion: false);
                updated = true;
            }
            if (dataVersion.IsOlderThan(VERSION)) updated = true;
            if (updated)
            {
                PWBCore.refreshDatabase = true;
                PaletteManager.instance.LoadPaletteFiles(deleteUnusedThumbnails: true);
            }

            return updated;
        }
    }

    #region VERSION
    [System.Serializable]
    public class PWBDataVersion
    {
        [SerializeField] public string _version;
        public bool IsOlderThan(string value) => IsOlderThan(value, _version);

        public static bool IsOlderThan(string value, string referenceValue)
        {
            var intArray = GetIntArray(referenceValue);
            var otherIntArray = GetIntArray(value);
            var minLength = Mathf.Min(intArray.Length, otherIntArray.Length);
            for (int i = 0; i < minLength; ++i)
            {
                if (intArray[i] < otherIntArray[i]) return true;
                else if (intArray[i] > otherIntArray[i]) return false;
            }
            return false;
        }
        private static int[] GetIntArray(string value)
        {
            var stringArray = value.Split('.');
            if (stringArray.Length == 0) return new int[] { 1, 0 };
            var intArray = new int[stringArray.Length];
            for (int i = 0; i < intArray.Length; ++i) intArray[i] = int.Parse(stringArray[i]);
            return intArray;
        }
    }
    #endregion

    #region DATA 2.8
    [System.Serializable]
    public class V2_8_PaletteManager
    {
        [SerializeField] public PaletteData[] _paletteData;
    }
    [System.Serializable]
    public class V2_8_PWBData
    {
        [SerializeField] public V2_8_PaletteManager _paletteManager;
    }
    #endregion

    #region DATA 4.0
    [System.Serializable]
    public struct V4_0_ObjectPose
    {
        [SerializeField] public ObjectId _id;
        [SerializeField] public Vector3 _position;
        [SerializeField] public Quaternion _localRotation;
        [SerializeField] public Vector3 _localScale;
    }
    [System.Serializable]
    public struct V4_0_ToolData
    {
        [SerializeField] public long _id;
        [SerializeField] public V4_0_ObjectPose[] _objectPoses;
    }
    [System.Serializable]
    public struct V4_0_SceneData
    {
        [SerializeField] public V4_0_ToolData[] _items;
    }
    [System.Serializable]
    public struct V4_0_ToolController
    {
        [SerializeField] public V4_0_SceneData[] _sceneItems;
    }
    [System.Serializable]
    public struct V4_0_PWBData
    {
        [SerializeField] public PinManager pinManager;
        [SerializeField] public V4_0_ToolController _lineManager;
        [SerializeField] public V4_0_ToolController _shapeManager;
        [SerializeField] public V4_0_ToolController _tilingManager;
    }
    #endregion
}
#pragma warning restore UDR0001
