using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.LocalIntros
{
    public class IntroProvider : IIntroProvider
    {
        public string Name { get; } = "Intros";

        public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            var introManager = new IntroManager();
            return Task.FromResult(introManager.Get(item, user));
        }
    }
}
