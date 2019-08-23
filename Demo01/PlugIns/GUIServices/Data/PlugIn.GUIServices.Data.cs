
using System;
using System.Reflection;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.VirtualPath;
using Serilog;

namespace Ace.Agent.GUIServices
{
    public class GUIServicesData
    {

        public GUIServicesData() : this(new MultiAppSettingsBuilder().Build())
        {
            Log.Debug("<GUIServicesData parameterless .ctor");
            Log.Debug("GUIServicesData parameterless .ctor>");
        }

        public GUIServicesData(IAppSettings pluginAppSettings)
        {
            Log.Debug("<GUIServicesData .ctor, pluginAppSettings = {PluginAppSettings}", pluginAppSettings);
            PluginAppSettings = pluginAppSettings;
            Log.Debug("GUIServicesData .ctor>");
        }

        //ToDo: constructors with event handlers

        public IAppSettings PluginAppSettings { get; set; }
    }
}
