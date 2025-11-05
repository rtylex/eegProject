using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace eegProject.Services
{
    internal sealed class MindwaveStreamService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public MindwaveStreamService(string host = "127.0.0.1", int port = 13854)
        {
            _host = host;
            _port = port;
        }

        public async Task StartAsync(Func<MindwaveSample, Task> onSample, Action<string> statusReporter, CancellationToken cancellationToken)
        {
            if (onSample == null)
            {
                throw new ArgumentNullException(nameof(onSample));
            }

            using (var client = new TcpClient())
            {
                await client.ConnectAsync(_host, _port).ConfigureAwait(false);
                using (var stream = client.GetStream())
                using (var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                {
                    await writer.WriteLineAsync("{\"enableRawOutput\": false, \"format\": \"Json\"}").ConfigureAwait(false);
                    statusReporter?.Invoke("Baglandi");

                    var hasReportedData = false;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null)
                        {
                            break;
                        }

                        line = line.Trim();
                        if (line.Length == 0)
                        {
                            continue;
                        }

                        MindwaveSample sample;
                        try
                        {
                            sample = ParseLine(line);
                        }
                        catch
                        {
                            statusReporter?.Invoke("Veri parse hatasi");
                            continue;
                        }

                        if (sample == null)
                        {
                            continue;
                        }

                        if (sample.PoorSignalLevel.HasValue && sample.PoorSignalLevel.Value > 80)
                        {
                            statusReporter?.Invoke("Sinyal zayif");
                            continue;
                        }

                        if (!hasReportedData)
                        {
                            statusReporter?.Invoke("Akis aktif");
                            hasReportedData = true;
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        await onSample(sample).ConfigureAwait(false);
                    }

                    statusReporter?.Invoke("Akis sonlandi");
                }
            }
        }

        private MindwaveSample ParseLine(string line)
        {
            var root = _serializer.Deserialize<Dictionary<string, object>>(line);
            if (root == null)
            {
                return null;
            }

            if (!root.TryGetValue("eegPower", out var powerObj))
            {
                return null;
            }

            var powerDict = AsDictionary(powerObj);
            if (powerDict == null)
            {
                return null;
            }

            var sample = new MindwaveSample
            {
                Delta = GetDouble(powerDict, "delta"),
                Theta = GetDouble(powerDict, "theta"),
                LowAlpha = GetDouble(powerDict, "lowAlpha"),
                HighAlpha = GetDouble(powerDict, "highAlpha"),
                LowBeta = GetDouble(powerDict, "lowBeta"),
                HighBeta = GetDouble(powerDict, "highBeta"),
                LowGamma = GetDouble(powerDict, "lowGamma"),
                HighGamma = GetDouble(powerDict, "highGamma"),
                BlinkStrength = GetInt(root, "blinkStrength"),
                PoorSignalLevel = GetInt(root, "poorSignalLevel")
            };

            return sample.HasAnyPower ? sample : null;
        }

        private static IDictionary<string, object> AsDictionary(object value)
        {
            return value as IDictionary<string, object>;
        }

        private static double? GetDouble(IDictionary<string, object> dictionary, string key)
        {
            if (dictionary == null || !dictionary.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            switch (value)
            {
                case double d:
                    return d;
                case float f:
                    return f;
                case int i:
                    return i;
                case long l:
                    return l;
                case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result):
                    return result;
                default:
                    return null;
            }
        }

        private static int? GetInt(IDictionary<string, object> dictionary, string key)
        {
            if (dictionary == null || !dictionary.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            switch (value)
            {
                case int i:
                    return i;
                case long l:
                    return (int)l;
                case double d:
                    return (int)d;
                case string s when int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result):
                    return result;
                default:
                    return null;
            }
        }
    }

    internal sealed class MindwaveSample
    {
        public double? Delta { get; set; }
        public double? Theta { get; set; }
        public double? LowAlpha { get; set; }
        public double? HighAlpha { get; set; }
        public double? LowBeta { get; set; }
        public double? HighBeta { get; set; }
        public double? LowGamma { get; set; }
        public double? HighGamma { get; set; }
        public int? BlinkStrength { get; set; }
        public int? PoorSignalLevel { get; set; }

        public bool HasAnyPower => Delta.HasValue || Theta.HasValue || LowAlpha.HasValue || HighAlpha.HasValue || LowBeta.HasValue || HighBeta.HasValue || LowGamma.HasValue || HighGamma.HasValue;
    }
}
