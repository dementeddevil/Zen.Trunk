using System.ServiceProcess;

namespace Zen.Trunk.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // Create service object and initialise from startup arguments
            var service = new TrunkStorageEngineService();
            service.Initialize(args);

            // Now we can bootup our service process
            var servicesToRun =
                new ServiceBase[]
                {
                    service
                };
            ServiceBase.Run(servicesToRun);
        }
    }
}
