using System;

namespace RvtToNavisConverter.Models
{
    public class ToolVersion
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public ToolType Type { get; set; }
        public DateTime? DetectedAt { get; set; }

        public string DisplayName => $"{Name} {Version}";
        
        public override string ToString() => DisplayName;
    }

    public enum ToolType
    {
        RevitServerTool,
        NavisworksFileToolsTaskRunner
    }
}