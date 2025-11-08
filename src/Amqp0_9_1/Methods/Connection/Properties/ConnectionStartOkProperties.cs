using System.Reflection;

namespace Amqp0_9_1.Methods.Connection.Properties
{
    internal sealed class ConnectionStartOkProperties
    {
        private readonly string product;
        private readonly string version;
        private readonly string platform;
        private readonly string copyright;

        public ConnectionStartOkProperties()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ??
                        assembly.GetName().Name;

            version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
                        assembly.GetName().Version?.ToString() ??
                        "0.0.0";

            platform = $"dotnet/{Environment.Version}";

            copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ??
                        "Copyright";
        }

        internal Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { nameof(product), product },
                { nameof(version), version },
                { nameof(platform), platform },
                { nameof(copyright), copyright }
            };
        }
    }
}
