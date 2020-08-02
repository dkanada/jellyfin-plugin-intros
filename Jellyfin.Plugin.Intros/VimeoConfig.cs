using System.Collections.Generic;

namespace Jellyfin.Plugin.Intros
{
    public class VimeoConfig
    {
        public VimeoRequest request { get; set; }

        public VimeoVideo video { get; set; }
    }

    public class VimeoVideo
    {
        public string title { get; set; }
    }

    public class VimeoRequest
    {
        public VimeoFiles files { get; set; }
    }

    public class VimeoFiles
    {
        public List<VimeoStream> progressive { get; set; }
    }

    public class VimeoStream
    {
        public string url { get; set; }

        public int height { get; set; }
    }
}
