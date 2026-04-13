namespace Biotrackr.Vitals.Svc.Configuration
{
    public class Settings
    {
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
        public double UserHeight { get; set; } = 1.88;
        public int LookbackDays { get; set; } = 1;
    }
}
