
using System;
using System.Reflection;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.VirtualPath;
using Serilog;
using Agent.GUIServices.Shared;

namespace Ace.Agent.GUIServices
{
    public class GUIServicesData
    {
        public GUIServicesData(IAppSettings pluginAppSettings, ConfigurationData configurationData)
        {
            Log.Debug("<GUIServicesData .ctor, pluginAppSettings = {PluginAppSettings}", pluginAppSettings);
            PlugInAppSettings = pluginAppSettings;
            ConfigurationData = configurationData;
            Log.Debug("GUIServicesData .ctor>");
        }

        public IAppSettings PlugInAppSettings { get; set; }
        public ConfigurationData ConfigurationData { get; set; }

    }
}
