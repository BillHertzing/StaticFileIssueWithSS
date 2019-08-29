

using System;
using ServiceStack;
using ServiceStack.IO;
using Serilog;
using ServiceStack.Configuration;
using Agent.GUIServices.Shared;

namespace Ace.Agent.GUIServices
{
    public class GUIServices : Service
    {

        public object Any(VerifyGUIRequest request)
        {
            var kind = request.Kind;
            var version = request.Version;
            // ToDo: add the code that returns True/False for the route that includes the kind/version
            return new VerifyGUIResponse { Result = "Blazor" };
        }

        public object Any(ListGUIsRequest request)
        {
            // Get the Plugin's data structure from the SS container
            GUIServicesData gUIServicesData = HostContext.Resolve<GUIServicesData>();
            // Get this PlugIn's AppSettings
            IAppSettings pluginAppSettings = gUIServicesData.PluginAppSettings;
            // Get the GUIs POCO from pluginAppSettings
            if (!pluginAppSettings.Exists(StringConstants.GUISConfigKey)
                || (pluginAppSettings.GetString(StringConstants.GUISConfigKey) == string.Empty))
            {
                throw new Exception(StringConstants.GUISKeyOrValueNotFoundExceptionMessage);
            }
            GUIS gUIS = pluginAppSettings.Get<GUIS>(StringConstants.GUISConfigKey);

            // ToDo: add the code that returns True/False for the route that includes the kind/version
            var hc = HostContext.AppHost.VirtualFileSources;

            
            return new ListGUIsResponse { App_Base = gUIS.GUIs.ToString() };
        }
    }
}
