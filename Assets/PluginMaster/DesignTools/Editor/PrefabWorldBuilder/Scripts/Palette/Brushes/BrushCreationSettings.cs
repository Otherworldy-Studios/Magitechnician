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
    public class BrushCreationSettings
    {
        [SerializeField] private bool _includeSubfolders = true;
        [SerializeField] private bool _addLabelsToDroppedPrefabs = false;
        [SerializeField] private string _labelsCSV = null;
        private string[] _labels = null;
        [SerializeField] private BrushSettings _defaultBrushSettings = new BrushSettings();
        [SerializeField] private ThumbnailSettings _defaultThumbnailSettings = new ThumbnailSettings();

        public bool includeSubfolders
        {
            get => _includeSubfolders;
            set
            {
                if (_includeSubfolders == value) return;
                _includeSubfolders = value;
            }
        }

        public bool addLabelsToDroppedPrefabs
        {
            get => _addLabelsToDroppedPrefabs;
            set
            {
                if (_addLabelsToDroppedPrefabs == value) return;
                _addLabelsToDroppedPrefabs = value;
            }
        }

        private void SplitCSV() => _labels = _labelsCSV.Replace(", ", ",").Split(',');

        public string[] labels
        {
            get
            {
                if (_labels == null || (_labels.Length == 0 && _labelsCSV != null && _labelsCSV != string.Empty))
                    SplitCSV();
                return _labels;
            }
        }

        public string labelsCSV
        {
            get => _labelsCSV;
            set
            {
                if (_labelsCSV == value) return;
                if (value == string.Empty)
                {
                    _labelsCSV = string.Empty;
                    _labels = new string[0];
                    return;
                }
                var trimmed = System.Text.RegularExpressions.Regex.Replace(value.Trim(), "[( *, +)]+", ", ");
                if (trimmed.Last() == ' ') trimmed = trimmed.Substring(0, trimmed.Length - 2);
                if (trimmed.First() == ',') trimmed = trimmed.Substring(1);
                if (_labelsCSV == trimmed) return;
                _labelsCSV = trimmed;
                SplitCSV();
            }
        }

        public BrushSettings defaultBrushSettings => _defaultBrushSettings;
        public void FactoryResetDefaultBrushSettings() => _defaultBrushSettings = new BrushSettings();

        public ThumbnailSettings defaultThumbnailSettings => _defaultThumbnailSettings;
        public void FactoryResetDefaultThumbnailSettings() => _defaultThumbnailSettings = new ThumbnailSettings();

        public BrushCreationSettings Clone()
        {
            var clone = new BrushCreationSettings();
            clone.Copy(this);
            return clone;
        }

        public void Copy(BrushCreationSettings other)
        {
            _includeSubfolders = other._includeSubfolders;
            _addLabelsToDroppedPrefabs = other._addLabelsToDroppedPrefabs;
            _labelsCSV = other._labelsCSV;
            if (other._labels != null)
            {
                _labels = new string[other._labels.Length];
                System.Array.Copy(other._labels, _labels, other._labels.Length);
            }
            _defaultBrushSettings.Copy(other._defaultBrushSettings);
            _defaultThumbnailSettings.Copy(other._defaultThumbnailSettings);
        }

        public override int GetHashCode()
        {
            int hashCode = 917907199;
            hashCode = hashCode * -1521134295 + _includeSubfolders.GetHashCode();
            hashCode = hashCode * -1521134295 + _addLabelsToDroppedPrefabs.GetHashCode();
            hashCode = hashCode * -1521134295 + _defaultBrushSettings.GetHashCode();
            hashCode = hashCode * -1521134295 + _defaultThumbnailSettings.GetHashCode();
            return hashCode;
        }
    }
}
#pragma warning restore UDR0001
