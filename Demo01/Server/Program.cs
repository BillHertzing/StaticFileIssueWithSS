using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceStack;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using Serilog.Exceptions;

namespace Server {
    class Program {

        public const string ListenOnURLs = "http://localhost:20500/";

        public const string EnvironmentVariablePrefix = "BlazorDemos_";
        public const string EnvironmentVariableWebHostBuilder = "WebHostBuilder";
        public const string WebHostBuilderStringDefault = "CreateIntegratedIISInProcessWebHostBuilder";
        public const string EnvironmentVariableEnvironment = "Environment";
        public const string EnvironmentDefault = "Production";

        public const string InvalidWebHostBuilderStaticMethodNameExceptionMessage = "The WebHostBuilder specified in the environment variable does not match any static method name returning an IWebHostBuilder";
        public const string InvalidEnvironmentExceptionMessage = "The Environment specified in the environment variable does not match known environment in this program";

        // Helper method to properly combine the prefix with the suffix
        static string EnvironmentVariableFullName(string name) { return EnvironmentVariablePrefix+name; }

        public static async Task Main(string[] args) {

            // To ensure every class uses the same Global Logger, set the LogManager's LogFactory before initializing the hosting environment
            // The Serilog.Log is a static entry to the Serilog logging provider
            // set the LogFactory to Serilog's LogFactory // may be old or obsolete
            // LogManager.LogFactory= new Serilog.LogFactory(); // may be old or obsolete
            // Create a default Serilog configuration in code
            // Create a logger instance for this class
            // Log=LogManager.GetLogger(typeof(Program));
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                //.Enrich.WithHttpRequestId()
                //.Enrich.WithUserName()
                .Enrich.WithExceptionDetails()
                .WriteTo.Seq(serverUrl: "http://localhost:5341")
                //.WriteTo.Udp(remoteAddress:IPAddress.Loopback, remotePort:9999, formatter:default) // I could not get it to write to Sentinel
                .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .WriteTo.Debug()
                // .WriteTo.File(path: "Logs/Demo.Serilog.{Date}.log", fileSizeLimitBytes: 1024, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, retainedFileCountLimit: 31)
                .CreateLogger();

            Log.Debug("Entering Program.Main");

            // determine where this program's entry point's executing assembly resides
            //   then change the working directory to the location where the assembly (and configuration files) are installed to.
            var loadedFromDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(loadedFromDir);

            // Determine the web host builder to use from an EnvironmentVariable
            var webHostBuilderName = Environment.GetEnvironmentVariable(EnvironmentVariableFullName(EnvironmentVariableWebHostBuilder))??WebHostBuilderStringDefault;

            // ToDo: find clever (fast) way to express "Go through the list of static methods returning IHostBuilder", if a (string) cast of the method name matches the WebHostBuilder environment variable string, select the method with the matching name, log that fact,, and call it here...
            // Set the GenericHostBuilder instance based on the name supplied by the environment variable
            IHostBuilder genericHostBuilder;
            if (webHostBuilderName=="IntegratedIISInProcessWebHostBuilder") {
                // Create an IntegratedIISInProcess generic host builder
                Log.Debug("in Program.Main: create genericHostBuilder by calling static method CreateGenericHostHostingIntegratedIISInProcessWebHostBuilder");
                genericHostBuilder=CreateGenericHostHostingIntegratedIISInProcessWebHostBuilder();
            } else if (webHostBuilderName=="KestrelAloneWebHostBuilder") {
                // Create an Kestrel only generic host builder
                Log.Debug("in Program.Main: create genericHostBuilder by calling static method CreateGenericHostHostingKestrelAloneBuilder");
                genericHostBuilder=CreateGenericHostHostingKestrelAloneBuilder();
            } else {
                throw new InvalidDataException(InvalidWebHostBuilderStaticMethodNameExceptionMessage);
            }

            // Determine the environment (Debug, TestingUnit, TestingX, Staging, Production) to use from an EnvironmentVariable
            var env = Environment.GetEnvironmentVariable(EnvironmentVariableFullName(EnvironmentVariableEnvironment))??EnvironmentDefault;
            Log.Debug("in Program.Main: env is {Env}",env);

            // Modify the genericHostBuilder according to the environment
            // ToDo: investigate using an enumeration to support localization
            switch (env) {
                case "Development":
                    // This is where many developer conveniences are configured for Development environment
                    // In the Development environment, modify the WebHostBuilder's settings to use the detailed error pages, and to capture startup errors
                    genericHostBuilder.ConfigureWebHost(webHostBuilder =>
                    webHostBuilder.CaptureStartupErrors(true)
                        .UseSetting("detailedErrors", "true")
                    );
                    break;
                case "Production":
                    break;
                default:
                    throw new InvalidOperationException(InvalidEnvironmentExceptionMessage);
            }

            // Create the generic host genericHost
            Log.Debug("in Program.Main: create genericHost by calling .Build() on the genericHostBuilder");
            var genericHost = genericHostBuilder.Build();

            // Start the genericHost
            Log.Debug("in Program.Main: genericHost created, starting RunAsync at {StartTime}, listening on {ListenOnURLs}", DateTime.Now, ListenOnURLs);
            await genericHost.RunAsync();
            // The followiing code runs on the continuation task after RunAsync returns
            Log.Debug("in Program.Main: Leaving Program.Main");
        }

        // This Builder pattern creates a GenericHostBuilder populated instructions to build an Integrated IIS InProcess WebHost
        public static IHostBuilder CreateGenericHostHostingIntegratedIISInProcessWebHostBuilder() =>
            new HostBuilder()
                .ConfigureWebHostDefaults(webHostBuilder => {
                    // The method UseIISIntegration instructs the HostBuilder to use IISIntegration which IS desired
                    webHostBuilder.UseIISIntegration()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                    // Specify the class to use when starting the WebHost
                    .UseStartup<Startup>()
                    // Use hard-coded URLs for this demo to listen on
                    .UseUrls(ListenOnURLs);
                });

        // This Builder pattern creates a GenericHostBuilder populated with instructions to build a Kestrel WebHost with no IIS integration
        public static IHostBuilder CreateGenericHostHostingKestrelAloneBuilder() =>
            // CreateDefaultBuilder includes IISIntegration which is NOT desired, so
            // The Kestrel WebHost must be manually configured into the Generic Host
            new HostBuilder()
                .ConfigureWebHostDefaults(webHostBuilder => {
                    webHostBuilder.UseKestrel()
                    // This (older) post has great info and examples on setting up the Kestrel options
                    //https://github.com/aspnet/KestrelHttpServer/issues/1334
                    // In V30P5, all SS interfaces return an error that "synchronous writes are disallowed", see following issue
                    //  https://github.com/aspnet/AspNetCore/issues/8302
                    // Workaround is to configure the default web server to AllowSynchronousIO=true
                    // ToDo: see if this is fixed in a release after V30P5
                    // Configure Kestrel
                    .ConfigureKestrel((context, options) => {
                        options.AllowSynchronousIO=true;
                    })
				    .UseContentRoot(Directory.GetCurrentDirectory())
                	// Specify the class to use when starting the WebHost
                    .UseStartup<Startup>()
                    // Use hard-coded URLs for this demo to listen on
                    .UseUrls(ListenOnURLs);
                });
    }

    public class Startup {
        public IConfiguration Configuration { get; }

        // This class gets created by the runtime when .Build is called on the webHostBuilder. The .ctor populates this class' Configuration property .
        public Startup(IConfiguration configuration) {
            Log.Debug("entering Program.Startup.ctor");
            Log.Debug("populating the Program.Startup.Configuration property by Constructior Injection");
            Configuration=configuration;
            Log.Debug("leaving Program.Startup.ctor");
        }

        // This method gets called by the runtime after this class' .ctor is finished. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            Log.Debug("Entering Program.Startup.ConfigureServices");
            Log.Debug("in Program.Startup.ConfigureServices: no service(s) have been injected in this Demo");
            Log.Debug("Leaving Program.Startup.ConfigureServices");
        }

        // This method gets called by the runtime when .Run or .RunAsync is called on the webHost instance. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            Log.Debug("Entering Program.Startup.Configure");
            Log.Debug("in Program.Startup.Configure: Create the SSApp");

            app.UseServiceStack(new SSAppHost() {
                AppSettings=new NetCoreAppSettings(Configuration) // Use the Configuration injected in the Startup .ctor
            });
            Log.Debug("in Program.Startup.Configure: SSApp creation is finished");
            Log.Debug("in Program.Startup.Configure: Provide the terminal middleware handler delegate to the IApplicationBuilder via .Run");
            // The supplied lambda becomes the final handler in the HTTP pipeline
            app.Run(async (context) => {
                Log.Debug("Last HTTP Pipeline handler");
                context.Response.StatusCode=404;
                await Task.FromResult(0);
            });

            Log.Debug("Leaving Program.Startup.Configure");
        }
    }
}
