using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using IoTEdgeRaspberryPiCamera.Services.Camera;
using IoTEdgeRaspberryPiCamera.Services.Cloud;
using IoTEdgeRaspberryPiCamera.Services.Scheduler;
using Microsoft.Extensions.Logging;

namespace IoTEdgeRaspberryPiCamera
{
    internal class Program
    {
        public static IContainer Container { get; set; }

        private static void Main(string[] args)
        {
            Console.WriteLine("  _____        _    _____                               \n" +
                              " |  __ \\      (_)  / ____|                              \n" +
                              " | |__) |_ __  _  | |     __ _ _ __ ___   ___ _ __ __ _ \n" +
                              " |  _  /| '_ \\| | | |    / _` | '_ ` _ \\ / _ \\ '__/ _` |\n" +
                              " | | \\ \\| |_) | | | |___| (_| | | | | | |  __/ | | (_| |\n" +
                              " |_|  \\_\\ .__/|_|  \\_____\\__,_|_| |_| |_|\\___|_|  \\__,_|\n" +
                              "        | |                                             \n" +
                              "        |_|                                             \n");
            Console.WriteLine("Azure IoT Edge RaspberryPi Camera Module");
            Console.WriteLine($"Version: {typeof(Program).Assembly.GetName().Version}");

            SetupIoC();

            var runner = Container.Resolve<Runner>();
            runner.Run();
            
            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
            runner.Dispose();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        private static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        private static void SetupIoC()
        {
            var builder = new ContainerBuilder();

            var loggerFactory = LoggerFactory.Create(loggerBuilder =>
            {
                var logLevelEnv = Environment.GetEnvironmentVariable("LogLevel");

#if DEBUG
                var defaultLogLevel = LogLevel.Debug;
#else
                var defaultLogLevel = LogLevel.Information;
#endif
                switch (logLevelEnv?.ToLower())
                {
                    case "none":
                        defaultLogLevel = LogLevel.None;
                        break;
                    case "debug":
                        defaultLogLevel = LogLevel.Debug;
                        break;
                    case "information":
                        defaultLogLevel = LogLevel.Information;
                        break;
                }

                loggerBuilder
                    .SetMinimumLevel(defaultLogLevel)
                    .AddSimpleConsole(c =>
                    {
                        c.IncludeScopes = true;
                        c.SingleLine = true;
                        c.TimestampFormat = "[yyyy-MM-ddTHH:mm:ssZ] ";
                        c.UseUtcTimestamp = true;
                    })
                    .AddDebug();
            });

            var logger = loggerFactory.CreateLogger("Raspberry Pi Camera Module");
            builder.RegisterInstance(logger)
                .As<ILogger>()
                .SingleInstance();

            builder.RegisterType<SchedulerProvider>()
                .As<ISchedulerProvider>()
                .SingleInstance();

            var storageConnectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING") ?? "";
            builder.RegisterType<BlobService>()
                .As<IBlobService>()
                .WithParameter("connectionString", storageConnectionString)
                .SingleInstance();
            
            builder.RegisterType<AzureIoTEdgeService>()
                .As<ICloudService>()
                .SingleInstance();

            builder.RegisterType<RpiCameraModule>()
                .As<ICameraService>()
                .SingleInstance();

            builder.RegisterType<Runner>()
                .SingleInstance();

            Container = builder.Build();
        }
    }
}
