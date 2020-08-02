using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Intros
{
    public class IntroProvider : IIntroProvider
    {
        public string Name { get; } = "Intros";

        public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            var introManager = new IntroManager();
            return Task.FromResult(introManager.Get());
        }

        public IEnumerable<string> GetAllIntroFiles()
        {
            // not implemented on server
            return Enumerable.Empty<string>();
        }
    }
}
