

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
    public class GUIServicesPlugin : IPlugin, IPreInitPlugin
    {

        public Microsoft.Extensions.Hosting.IHostEnvironment HostEnvironment { get; set; }

        public void Configure(IAppHost appHost)
        {
            Log.Debug("<GUIServicesPlugin.Configure, appHost = {AppHost}", appHost);

            // Load the local property of IHostEnvironment type
            HostEnvironment = appHost.GetContainer().Resolve<Microsoft.Extensions.Hosting.IHostEnvironment>();
            // Determine the environment this PlugIn has been activated in
            string envName = HostEnvironment.EnvironmentName;

            // Populate this PlugIn's AppSettings Configuration Settings and place it in the appSettingsDictionary

            // Location of the Configuration text files are at the ContentRoot. ToDo: figure out how to place them / resolve them relative to the location of the PlugIn assembly

            var pluginAppSettingsBuilder = new MultiAppSettingsBuilder();
            // ToDo: command line settings
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
            var pluginAppSettings = pluginAppSettingsBuilder.Build();

            // Get the GUIs POCO from pluginAppSettings
            if (!pluginAppSettings.Exists(StringConstants.GUISConfigKey)
                || (pluginAppSettings.GetString(StringConstants.GUISConfigKey) == string.Empty))
            {
                throw new Exception(StringConstants.GUISKeyOrValueNotFoundExceptionMessage);
            }
            GUIS gUIS = pluginAppSettings.Get<GUIS>(StringConstants.GUISConfigKey);

            // ToDo: Security: this is a place where character strings from external sources are processed, check carefully
            char[] invalidChars = Path.GetInvalidPathChars();

            // Loop over each GUI defined in pluginAppSettings GUIs, create a virtualFileMapping
            // ToDo: Throw exception if the IEnumerable count = 0
            foreach (GUIMapping gUIMapping in gUIS.GUIs )
            {
                if (gUIMapping.RelativeToContentRootPath.Any(x => invalidChars.Contains(x)))
                {
                    throw new Exception(StringConstants.RelativeRootPathValueContainsIlegalCharacterExceptionMessage);
                }
                // The GenericHost's ContentRootPath is found on the HostEnvironment injected during the .ctor
                var physicalPath = Path.Combine(this.HostEnvironment.ContentRootPath, gUIMapping.RelativeToContentRootPath);
                Log.Debug("in GUIServicesPlugin.Configure, physicalPath = {PhysicalPath}, ContentRootPath = {ContentRootPath} relativeRootPathValue = {RelativeToContentRootPath}",physicalPath, this.HostEnvironment.ContentRootPath, gUIMapping.RelativeToContentRootPath);
                Log.Debug("in GUIServicesPlugin.Configure, index.html exists in physicalpath = {0}",File.Exists(Path.Combine(physicalPath, "index.html")));
                Log.Debug("in GUIServicesPlugin.Configure, virtualRootPath = {GUIMappingVirtualRootPath}", gUIMapping.VirtualRootPath);
                // Map the virtualRootPath to the physicalpath of the root of the GUI
                // Wrap in a try catch block in case the physicalRootPath does not exists
                // ToDo: test for failure condition instead of letting it throw an exception
                try
                {
                    appHost.AddVirtualFileSources
                        .Add(new FileSystemMapping(gUIMapping.VirtualRootPath, physicalPath));
                }
                catch (Exception e)
                {
                    // ToDo: research how best to log an exception with Serilog, ServiceStack and/or MS
                    Log.Debug(e, "in GUIServicesPlugin.Configure, Adding a new VirtualFileSource failed with : {Message}", e.Message);
                    // ToDo wrap in an Aggregate when doing loop
                    throw;
                    // ToDo: figure out how to log this and fallback to something useful
                }
                //ToDo: If this is the default GUI, map the URL '/' to this physical path
            }

            // Blazor requires the delivery of static files ending in certain file suffixes.
            // SS disallows many of these by default, so here we tell SS to allow certain file suffixes
            appHost.Config.AllowFileExtensions.Add("dll");
            appHost.Config.AllowFileExtensions.Add("json");
            appHost.Config.AllowFileExtensions.Add("pdb");

            // per conversation with Myth at SS, the default behavior of a web server, for URI "/" is to return the content of wwwroot/index.html
            // ToDo: better understanding how this is configured to ensure it behaves as expected in all WebMost models
            // appHost.Config.DefaultRedirectPath="/index.html";

            // Remove the ServiceStack metadata feature, as it overrides the default behavior expected when a request for a root resource arrives, and the root resource is not found
            appHost.Config.EnableFeatures = Feature.All.Remove(Feature.Metadata);

            // Need to figure out if / how a PlugIn can add a PlugIn to the parent AppHost, if it detects there is a dependency

            // Blazor requires CORS support, enable the ServiceStack CORS feature
            appHost.Plugins.Add(new CorsFeature(
               allowedMethods: "GET, POST, PUT, DELETE, OPTIONS",
               allowedOrigins: "*",
               allowCredentials: true,
               allowedHeaders: "content-type, Authorization, Accept"));

            // create the plugIn's data object
            GUIServicesData gUIServicesData = new GUIServicesData(pluginAppSettings);

            // Pass the Plugin's data structure to the container so it will be available to every other module and services
            appHost.GetContainer().Register<GUIServicesData>(d => gUIServicesData);

            // ToDo: enable the mechanisms that monitors each plugin-specific data sensor, and start them running

        }

        /// <summary>
        /// Register this plugin with the appHost
        /// Configure its observableDataStructures and event handlers
        /// </summary>
        /// <param name="appHost">The ASP.Net Host</param>
        public void Register(IAppHost appHost)
        {
            // ToDo: Create static string for exception message
            if (null == appHost) { throw new ArgumentNullException("appHost"); }
            appHost.RegisterService<GUIServices>();
            this.Configure(appHost);
        }

        public void BeforePluginsLoaded(IAppHost appHost)
        {
        }
    }
}
