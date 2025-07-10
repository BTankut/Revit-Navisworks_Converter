namespace RvtToNavisConverter.Models
{
    public class AppSettings
    {
        public string RevitServerIp { get; set; } = string.Empty;
        public string RevitServerAccelerator { get; set; } = string.Empty;
        public string AcceleratorName { get; set; } = string.Empty;
        public string RevitServerToolPath { get; set; } = string.Empty;
        public string NavisworksToolPath { get; set; } = string.Empty;
        public string DefaultDownloadPath { get; set; } = string.Empty;
        public string DefaultNwcPath { get; set; } = string.Empty;
        public string DefaultNwdPath { get; set; } = string.Empty;
    }
}
