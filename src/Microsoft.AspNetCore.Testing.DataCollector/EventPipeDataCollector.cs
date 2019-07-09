using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Microsoft.AspNetCore.Testing.Diagnostics
{
    [DataCollectorFriendlyName("EventPipe")]
    [DataCollectorTypeUri("aspnet://Diagnostics/EventPipe")]
    public class EventPipeDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        private SessionConfiguration _sessionConfiguration;
        private DataCollectionSink _dataSink;
        private DataCollectionLogger _logger;
        private string _traceDirectory;

        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            // Load config
            _sessionConfiguration = new SessionConfiguration(
                (uint)ReadIntAttribute(configurationElement, "circularBufferSizeMB", 1024),
                EventPipeSerializationFormat.NetTrace,
                ReadProviders(configurationElement));
            _dataSink = dataSink;
            _logger = logger;

            var traceExt = _sessionConfiguration.Format == EventPipeSerializationFormat.NetTrace ? "nettrace" : "netperf";
            _traceDirectory = Path.Combine(Path.GetTempPath(), "vstest_eventpipe", $"{environmentContext.SessionDataCollectionContext.SessionId.Id:N}_{Guid.NewGuid():N}");
            if (!Directory.Exists(_traceDirectory))
            {
                Directory.CreateDirectory(_traceDirectory);
            }

            events.SessionEnd += Events_SessionEnd;
        }

        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            yield return new KeyValuePair<string, string>("COMPLUS_EnableEventPipe", "1");
            yield return new KeyValuePair<string, string>("COMPLUS_EventPipeCircularMB", _sessionConfiguration.CircularBufferSizeInMB.ToString());
            yield return new KeyValuePair<string, string>("COMPLUS_EventPipeNetTraceFormat", _sessionConfiguration.Format == EventPipeSerializationFormat.NetTrace ? "1" : "0");
            yield return new KeyValuePair<string, string>("COMPLUS_EventPipeOutputPath", _traceDirectory);

            var configString = new StringBuilder();
            foreach (var provider in _sessionConfiguration.Providers)
            {
                configString.Append(provider.ToString());
                configString.Append(",");
            }

            // Remove trailing comma
            configString.Length -= 1;

            yield return new KeyValuePair<string, string>("COMPLUS_EventPipeConfig", configString.ToString());
        }

        private void Events_SessionEnd(object sender, SessionEndEventArgs e)
        {
            try
            {
                foreach(var file in Directory.GetFiles(_traceDirectory))
                {
                    _logger.LogWarning(e.Context, $"Saving event pipe session: {file}");
                    _dataSink.SendFileAsync(e.Context, file, deleteFile: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(e.Context, "Error during SessionEnd", ex);
            }
        }

        private IReadOnlyCollection<Provider> ReadProviders(XmlElement configurationElement)
        {
            var providersList = configurationElement["Providers"];
            if (providersList == null)
            {
                return Array.Empty<Provider>();
            }
            else
            {
                var providers = new List<Provider>();
                var profileElements = providersList.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "Profile");
                foreach(var profileElement in profileElements)
                {
                    var profileName = ReadRequiredAttribute(profileElement, "name");
                    LoadProfile(profileName, providers);
                }

                var providerElements = providersList.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "Provider");
                foreach (var providerElement in providerElements)
                {
                    providers.Add(ReadProvider(providerElement));
                }
                return providers;
            }
        }

        private void LoadProfile(string profileName, List<Provider> providers)
        {
            switch(profileName.ToLowerInvariant())
            {
                // Source: https://github.com/dotnet/coreclr/blob/961e8d7bd1bdd058ee5f8f34937e9f3f9d80b65b/src/System.Private.CoreLib/src/System/Diagnostics/Eventing/EventPipeController.cs#L30
                case "default":
                    providers.Add(new Provider("Microsoft-Windows-DotNETRuntime", 0x4c14fccbd, EventLevel.Verbose, null));
                    providers.Add(new Provider("Microsoft-Windows-DotNETRuntimePrivate", 0x4002000b, EventLevel.Verbose, null));
                    providers.Add(new Provider("Microsoft-DotNETCore-SampleProfiler", 0x0, EventLevel.Verbose, null));
                    break;
            }
        }

        private Provider ReadProvider(XmlElement providerElement)
        {
            return new Provider(name: ReadRequiredAttribute(providerElement, "name"));
        }

        private string ReadRequiredAttribute(XmlElement element, string attribute)
        {
            var value = element.GetAttribute(attribute);
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"Missing required attribute '{attribute}'.");
            }
            return value;
        }

        private int ReadIntAttribute(XmlElement element, string attribute, int defaultValue)
        {
            var valStr = element.GetAttribute(attribute);
            if (string.IsNullOrEmpty(valStr))
            {
                return defaultValue;
            }
            return int.Parse(valStr);
        }
    }
}
