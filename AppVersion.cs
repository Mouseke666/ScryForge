using System.Reflection;

namespace ScryForge
{
    public static class AppVersion
    {
        public static string Get()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrEmpty(informational))
            {
                var cleanVersion = informational.Split('+')[0];

                if (cleanVersion.Contains('.'))
                {
                    return cleanVersion;
                }
            }

            var version = assembly.GetName().Version;
            if (version != null && version.Build >= 0)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }

            return "Unknown";
        }

        public static string GetWithPrefix() => "v" + Get();
        public static string GetFull() => "ScryForge " + GetWithPrefix();
    }
}