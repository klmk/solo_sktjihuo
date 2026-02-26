using System.Collections.Generic;

namespace HardwareHook.Core.Configuration
{
    public class ConfigurationLoadResult
    {
        public bool Success { get; set; }

        public HardwareConfig? Config { get; set; }

        public string? ErrorMessage { get; set; }

        public List<string> ValidationErrors { get; } = new List<string>();
    }
}

