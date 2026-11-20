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
    [System.Serializable]
    public class SceneData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA>
        where TOOL_NAME : IToolName, new()
        where TOOL_SETTINGS : IToolSettings, new()
        where CONTROL_POINT : ControlPoint, new()
        where TOOL_DATA : PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>, new()
    {
        [SerializeField] private string _sceneGUID = null;
        [SerializeField] private System.Collections.Generic.List<TOOL_DATA> _items = null;

        public string sceneGUID { get => _sceneGUID; set => _sceneGUID = value; }
        public System.Collections.Generic.List<TOOL_DATA> items => _items;

        public SceneData() { }
        public SceneData(string sceneGUID) => _sceneGUID = sceneGUID;

        public void AddItem(TOOL_DATA data)
        {
            if (_items == null) _items = new System.Collections.Generic.List<TOOL_DATA>();
            _items.Add(data);
        }

        public void RemoveItemData(long itemId) => _items.RemoveAll(i => i.id == itemId);

        public void DeleteItemData(long itemId, bool deleteObjects)
        {
            var item = GetItem(itemId);
            if (item == null) return;
            if (deleteObjects) item.DestroyGameObjects();
            RemoveItemData(itemId);
        }
        public TOOL_DATA GetItem(long itemId) => _items.Find(i => i.id == itemId);

        public GameObject[] GetParents(long itemId)
        {
            var parents = new System.Collections.Generic.HashSet<GameObject>();
            var item = GetItem(itemId);
            if (item == null) return parents.ToArray();
            var objs = item.objects;
            foreach (var obj in objs)
            {
                if (obj == null) continue;
                if (obj.transform.parent == null) continue;
                var parent = obj.transform.parent.gameObject;
                if (parents.Contains(parent)) continue;
                parents.Add(parent);
                do
                {
                    if (parent.transform.parent == null) parent = null;
                    else
                    {
                        parent = parent.transform.parent.gameObject;
                        if (!parents.Contains(parent)) parents.Add(parent);
                    }
                }
                while (parent != null);
            }
            return parents.ToArray();
        }
    }
}
#pragma warning restore UDR0001
