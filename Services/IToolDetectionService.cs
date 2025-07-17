using System.Collections.Generic;
using System.Threading.Tasks;
using RvtToNavisConverter.Models;

namespace RvtToNavisConverter.Services
{
    public interface IToolDetectionService
    {
        Task<List<ToolVersion>> DetectRevitServerToolsAsync();
        Task<List<ToolVersion>> DetectNavisworksToolsAsync();
        Task<List<ToolVersion>> DetectAllToolsAsync();
        string ExtractVersionFromPath(string path, ToolType toolType);
    }
}