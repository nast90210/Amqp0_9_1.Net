using System.Diagnostics;
using Amqp0_9_1.Clients;

namespace Amqp0_9_1.Utilities
{
    internal static class GlobalExceptionHandler
    {
        private static bool _initialized;

        public static void Initialize(AmqpConnection connection)
        {
            if (_initialized) return;
            _initialized = true;

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Debug.WriteLine(ex, "AppDomain.UnhandledException");
                connection.Dispose();
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Debug.WriteLine(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
                connection.Dispose();
            };
        }
    }
}