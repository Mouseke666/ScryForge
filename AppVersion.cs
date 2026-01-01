using System.Reflection;

namespace ScryForge
{
    public static class AppVersion
    {
        public static string Get()
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Haal InformationalVersion op (dit is wat GitHub Actions instelt)
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            // Als we een InformationalVersion hebben: verwijder alles na een eventuele '+' (commit-hash/metadata)
            if (!string.IsNullOrEmpty(informational))
            {
                var cleanVersion = informational.Split('+')[0]; // Alles na '+' wegknippen

                // Extra veiligheid: alleen teruggeven als het een echte versie lijkt (bevat een punt)
                if (cleanVersion.Contains('.'))
                    return cleanVersion;
            }

            // Fallback: standaard AssemblyVersion (Major.Minor.Patch)
            var version = assembly.GetName().Version;
            if (version != null && version.Build >= 0)
                return $"{version.Major}.{version.Minor}.{version.Build}";

            return "Onbekend";
        }

        /// <summary>
        /// Bijv. "v1.0.25"
        /// </summary>
        public static string GetWithPrefix() => "v" + Get();

        /// <summary>
        /// Bijv. "ScryForge v1.0.25" – handig voor logs of titels
        /// </summary>
        public static string GetFull() => "ScryForge " + GetWithPrefix();
    }
}