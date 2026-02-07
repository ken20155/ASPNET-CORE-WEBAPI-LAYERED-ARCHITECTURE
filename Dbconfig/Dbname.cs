namespace WebApiInterviewStatus.Dbconfig
{
    public static class Dbname
    {
        // Static properties (can be assigned at runtime)
        public static string LogDb { get; private set; } = null!;
        public static string MainDb { get; private set; } = null!;
        public static string SysDb { get; private set; } = null!;

        // Initialize once at startup
        public static void Initialize(IConfiguration configuration)
        {
            LogDb = configuration["DatabaseNames:LogDb"] ?? throw new InvalidOperationException("LogDb not configured.");
            MainDb = configuration["DatabaseNames:MainDb"] ?? throw new InvalidOperationException("MainDb not configured.");
            SysDb = configuration["DatabaseNames:SysDb"] ?? throw new InvalidOperationException("SysDb not configured.");
        }
    }
}
