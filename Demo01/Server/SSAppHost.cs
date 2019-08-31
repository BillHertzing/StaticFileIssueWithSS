// Define the Container being used when configuring the SSApp
using Funq;
using System;
using Serilog;
using ServiceStack;
using ServiceStack.Text;
// Required to serve the Blazor static files
using ServiceStack.VirtualPath;
// Added to support the use of Dictionary in this Demo 
using System.Collections.Generic;
using Ace.Agent.GUIServices;

namespace Server
{

    public class SSAppHost : AppHostBase
    {

        public const string CouldNotCreateServiceStackVirtualFileMappingExceptionMessage = "Could not create ServiceStack Virtual File Mapping: ";

        public SSAppHost() : base("SSServer", typeof(SSAppHost).Assembly)
        {
            Log.Debug("Entering SSAppHost Ctor");
            Log.Debug("Leaving SSAppHost Ctor");
        }

        public override void Configure(Container container)
        {
            Log.Debug("Entering SSAppHost.Configure method");

            // Everythoing having to do with the delivery of static files should be handled within the GUIServices PlugIn
            /* Add the GUI plugin */
            // Create the list of PlugIns to load
            var plugInList = new List<IPlugin>() {
               new GUIServicesPlugin(),
            };

            // Load each plugin here. 
            foreach (var pl in plugInList)
            {
                Plugins.Add(pl);
            }

            Log.Debug("Leaving SSAppHost.Configure");
        }
    }


}
