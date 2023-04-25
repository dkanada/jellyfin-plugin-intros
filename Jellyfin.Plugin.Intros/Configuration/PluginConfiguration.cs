using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Intros.Configuration
{
    public class IntroPluginConfiguration : BasePluginConfiguration
    {
        public string Local { get; set; } = string.Empty;

        public List<LocalVideo> DetectedLocalVideos { get; set; } = new List<LocalVideo>();

        public List<Guid> EnabledLocalVideos { get; set; } = new List<Guid>();

    }

    public class LocalVideo
    {
        public string Name { get; set; }

        public Guid ItemId { get; set; }
    }
}
