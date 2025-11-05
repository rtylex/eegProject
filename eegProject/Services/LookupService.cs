using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace eegProject.Services
{
    [DataContract]
    internal sealed class LookupData
    {
        [DataMember]
        public List<string> ExperimentTypes { get; set; } = new List<string>();

        [DataMember]
        public List<string> TimeLabels { get; set; } = new List<string>();

        public LookupData Clone()
        {
            return new LookupData
            {
                ExperimentTypes = new List<string>(ExperimentTypes ?? new List<string>()),
                TimeLabels = new List<string>(TimeLabels ?? new List<string>())
            };
        }

        public void Normalize()
        {
            ExperimentTypes = (ExperimentTypes ?? new List<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList();

            TimeLabels = (TimeLabels ?? new List<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    internal sealed class LookupService
    {
        private readonly string _filePath;
        private readonly object _syncRoot = new object();

        public LookupService()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var configDirectory = Path.Combine(baseDirectory, "config");
            _filePath = Path.Combine(configDirectory, "lookup_store.json");
        }

        public Task<LookupData> GetAsync()
        {
            return Task.Run(() => LoadInternal());
        }

        public Task SaveAsync(LookupData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return Task.Run(() => SaveInternal(data));
        }

        private LookupData LoadInternal()
        {
            lock (_syncRoot)
            {
                EnsureDirectory();

                if (!File.Exists(_filePath))
                {
                    var defaults = CreateDefaults();
                    SaveLocked(defaults);
                    return defaults.Clone();
                }

                try
                {
                    using (var stream = File.OpenRead(_filePath))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(LookupData));
                        var data = serializer.ReadObject(stream) as LookupData;
                        if (data == null)
                        {
                            var defaults = CreateDefaults();
                            SaveLocked(defaults);
                            return defaults.Clone();
                        }

                        data.Normalize();
                        return data.Clone();
                    }
                }
                catch
                {
                    var defaults = CreateDefaults();
                    SaveLocked(defaults);
                    return defaults.Clone();
                }
            }
        }

        private void SaveInternal(LookupData data)
        {
            lock (_syncRoot)
            {
                EnsureDirectory();
                data.Normalize();
                SaveLocked(data);
            }
        }

        private void SaveLocked(LookupData data)
        {
            var serializer = new DataContractJsonSerializer(typeof(LookupData));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, data);
                var json = Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(_filePath, json, Encoding.UTF8);
            }
        }

        private void EnsureDirectory()
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static LookupData CreateDefaults()
        {
            return new LookupData
            {
                ExperimentTypes = new List<string> { "Egitim", "Rahatlama", "Muzik" },
                TimeLabels = new List<string> { "Bazal", "30dk", "1saat", "2saat" }
            };
        }
    }
}
