

using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.VirtualPath;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using ServiceStack.Caching;
using Serilog;
using Microsoft.Extensions.Hosting;
using Agent.GUIServices.Shared;

namespace Ace.Agent.GUIServices
{
    public class GUIServicesPlugin : IPlugin, IPreInitPlugin, IPostInitPlugin
    {

        public Microsoft.Extensions.Hosting.IHostEnvironment HostEnvironment { get; set; }

        public IAppSettings PlugInAppSettings { get; set; }
        public ConfigurationData ConfigurationData { get; set; }

        /// <summary>
        /// Register this plugin with the appHost
        /// </summary>
        /// <param name="appHost">The ServiceStack Host</param>
        public void Register(IAppHost appHost)
        {
            if (null == appHost) { throw new ArgumentNullException(nameof(appHost)); }

            appHost.RegisterService<GUIServices>();
        }

        /// Configure its PlugInAppSettings and ConfigurationData
        public void BeforePluginsLoaded(IAppHost appHost)
        {

            // Get the IHostEnvironment type object from the ServiceStack Container
            HostEnvironment = appHost.GetContainer().Resolve<Microsoft.Extensions.Hosting.IHostEnvironment>();
            // Determine the environment this PlugIn has been activated in
            string envName = HostEnvironment.EnvironmentName;

            // Populate this PlugIn's AppSettings Configuration Settings and place it in the appSettingsDictionary
            // ToDo: figure out how to place /resolve text files from relative to the location of the PlugIn assembly
            var pluginAppSettingsBuilder = new MultiAppSettingsBuilder();
            // ToDo: command line settings have the highest priority 
            // Environment variables have 2nd highest priority
            pluginAppSettingsBuilder.AddEnvironmentalVariables();
            // third priority are Non-Production Configuration settings in a text file
            if (!this.HostEnvironment.IsProduction())
            {
                var settingsTextFileName = Ace.Agent.GUIServices.StringConstants.PluginSettingsTextFileName + '.' + envName + StringConstants.PluginSettingsTextFileSuffix;
                // ToDo: ensure it exists and the ensure we have permission to read it
                // ToDo: Security: There is something called a Time-To-Check / Time-To-Use vulnerability, ensure the way we check then access the text file does not open the program to this vulnerability
                pluginAppSettingsBuilder.AddTextFile(settingsTextFileName);
            }
            // next in priority are Production Configuration settings in a text file
            pluginAppSettingsBuilder.AddTextFile(StringConstants.PluginSettingsTextFileName + StringConstants.PluginSettingsTextFileSuffix);
            // BuiltIn (compiled in) have the lowest priority
            pluginAppSettingsBuilder.AddDictionarySettings(DefaultConfiguration.Production);

            // Create the appSettings for this PlugIn from the builder
            PlugInAppSettings = pluginAppSettingsBuilder.Build();
            // Populate the ConfigurationData property with an empty configuration data instance
            // populate the GUIS property of the ConfigurationData with a new GUIS having an empty list of GUIs
            ConfigurationData = new ConfigurationData();
            ConfigurationData.GUIS = new GUIS() { GUIs = new List<GUI>() };
            // Get the GUIMaps POCO from PlugInAppSettings
            // Ensure the PlugInAppSettings has a non-empty ConfigKey for the GUIMaps
            if (!PlugInAppSettings.Exists(StringConstants.GUIMapsConfigKey)
                || (PlugInAppSettings.GetString(StringConstants.GUIMapsConfigKey) == string.Empty))
            {
                throw new Exception(StringConstants.GUISKeyOrValueNotFoundExceptionMessage);
            }
            // ToDo: Security: this is a place where character strings from external sources are processed, check carefully
            //  any typo in the text file can make the conversion to a POCO break
            GUIMaps gUIMaps;
            try
            {
                gUIMaps = PlugInAppSettings.Get<GUIMaps>(StringConstants.GUIMapsConfigKey);
            }
            catch (Exception)
            {
                //ToDo: Better exception handling
                throw;
            }
            // ToDo: Throw exception if the IEnumerable count = 0
            char[] invalidChars = Path.GetInvalidPathChars();

            // Loop over each GUI defined in pluginAppSettings GUIs, create a virtualFileMapping
            foreach (GUIMap gUIMap in gUIMaps._GUIMaps)
            {
                if (gUIMap.RelativeToContentRootPath.Any(x => invalidChars.Contains(x)))
                {
                    throw new Exception(StringConstants.RelativeRootPathValueContainsIlegalCharacterExceptionMessage);
                }
                // The GenericHost's ContentRootPath is found on the HostEnvironment injected during the .ctor
                var physicalPath = Path.Combine(this.HostEnvironment.ContentRootPath, gUIMap.RelativeToContentRootPath);
                //Log.Debug("in GUIServicesPlugin.Configure, physicalPath = {PhysicalPath}, ContentRootPath = {ContentRootPath} relativeRootPathValue = {RelativeToContentRootPath}", physicalPath, this.HostEnvironment.ContentRootPath, gUIMap.RelativeToContentRootPath);
                //Log.Debug("in GUIServicesPlugin.Configure, index.html exists in physicalpath = {0}", File.Exists(Path.Combine(physicalPath, "index.html")));
                //Log.Debug("in GUIServicesPlugin.Configure, virtualRootPath = {GUIMapVirtualRootPath}", gUIMap.VirtualRootPath);
                // Map the virtualRootPath to the physicalpath of the root of the GUI
                // Wrap in a try catch block in case the physicalRootPath does not exists
                // ToDo: test for failure condition instead of letting it throw an exception
                try
                {
                    appHost.AddVirtualFileSources
                        .Add(new FileSystemMapping(gUIMap.VirtualRootPath, physicalPath));
                }
                catch (Exception e)
                {
                    // ToDo: research how best to log an exception with Serilog, ServiceStack and/or MS
                    // ToDo: USe stringconstant for exception message
                    Log.Debug(e, "in GUIServicesPlugin.Configure, Adding a new VirtualFileSource failed with : {Message}", e.Message);
                    // ToDo wrap in an Aggregate when doing loop
                    throw;
                    // ToDo: figure out how to log this and fallback to something useful
                }
                // ToDo: Get Version and Description from assembly metadata
                ConfigurationData.GUIS.GUIs.Add(new GUI() { Version = "0.0.1" , Description = "A sample" , GUIMap = gUIMap });
            }

            // ToDo: add support for probing for additional GUIS at runtime, add them to ConfigurationData.GUIS.GUIs

            // Map the root path "/" to the default GUI
            // ToDo: Security: this is a place where character strings from external sources are processed, check carefully
            // Get the DefaultGUIVirtualRootPath from PlugInAppSettings
            // Ensure the PlugInAppSettings has a non-empty ConfigKey for the DefaultGUIVirtualRootPath
            if (!PlugInAppSettings.Exists(StringConstants.DefaultGUIVirtualRootPathConfigKey)
                || (PlugInAppSettings.GetString(StringConstants.DefaultGUIVirtualRootPathConfigKey) == string.Empty))
            {
                throw new Exception(StringConstants.DefaultGUIVirtualRootPathKeyOrValueNotFoundExceptionMessage);
            }
            // Ensure the value of the defaultGUIVirtualRootPath matches the value of the VirtualRootPath of one of the GUIMaps
            string defaultGUIVirtualRootPath =  PlugInAppSettings.Get<string>(StringConstants.DefaultGUIVirtualRootPathConfigKey);
            GUI defaultGUI;
            try { 
                defaultGUI = ConfigurationData.GUIS.GUIs.Single(gUI => gUI.GUIMap.VirtualRootPath.Matches(defaultGUIVirtualRootPath));
            } catch {
                throw new Exception(StringConstants.DefaultGUIVirtualRootPathDoesNotMatchExactlyOneGUIVirtualRootPathExceptionMessage);
            }

            // Set the default redirect 
            appHost.Config.DefaultRedirectPath = String.Format(StringConstants.DefaultRedirectPathTemplate,defaultGUI.GUIMap.VirtualRootPath);

            // Blazor requires the delivery of static files ending in certain file suffixes.
            // SS disallows some of these by default, so here we tell SS to allow certain file suffixes
            appHost.Config.AllowFileExtensions.Add("dll");
            appHost.Config.AllowFileExtensions.Add("json");
            appHost.Config.AllowFileExtensions.Add("pdb");

            // Blazor requires CORS support, enable the ServiceStack CORS feature
            appHost.Plugins.Add(new CorsFeature(
               allowedMethods: "GET, POST, PUT, DELETE, OPTIONS",
               allowedOrigins: "*",
               allowCredentials: true,
               allowedHeaders: "content-type, Authorization, Accept"));

            // create the PlugIn's data object  
            GUIServicesData gUIServicesData = new GUIServicesData(PlugInAppSettings, ConfigurationData);

            // Pass the Plugin's data structure to the container so it will be available to every other module and services
            appHost.GetContainer().Register<GUIServicesData>(d => gUIServicesData);

            // ToDo: enable the mechanisms that monitors each plugin-specific data sensor, and start them running
            // ToDo: Need to figure out if / how a PlugIn can add a PlugIn to the parent AppHost, if it detects there is a dependency
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {

        }
    }
}
