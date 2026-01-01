using System.Reflection;

namespace ScryForge
{
    public static class AppVersion
    {
        public static string Get()
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Eerst InformationalVersion proberen (dit is wat we via GitHub Actions instellen: bijv. "1.0.23")
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrEmpty(informational))
                return informational;

            // Fallback naar de standaard AssemblyVersion
            var version = assembly.GetName().Version;
            if (version != null)
                return $"{version.Major}.{version.Minor}.{version.Build}";

            return "Onbekend";
        }

        // Optioneel: met 'v' ervoor, als je dat mooier vindt
        public static string GetWithPrefix() => "v" + Get();
    }
}