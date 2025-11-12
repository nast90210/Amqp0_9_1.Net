using System.Reflection;

namespace Amqp0_9_1.Methods.Connection.Properties
{
    internal sealed class ConnectionStartOkProperties
    {
        private string Product { get; }
        private string Version { get; }
        private string Platform { get; }
        private string Copyright { get; }

        public ConnectionStartOkProperties()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            Product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ??
                        assembly.GetName().Name;

            Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
                        assembly.GetName().Version?.ToString() ??
                        "0.0.0";

            Platform = $"dotnet/{Environment.Version}";

            Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ??
                        "Copyright";
        }

        internal Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { nameof(Product), Product },
                { nameof(Version), Version },
                { nameof(Platform), Platform },
                { nameof(Copyright), Copyright }
            };
        }
    }
}
