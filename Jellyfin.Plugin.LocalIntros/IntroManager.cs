using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.LocalIntros.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.LocalIntros
{
    public class IntroManager
    {
        private readonly CookieContainer _cookieContainer = new CookieContainer();

        private readonly Random _random = new Random();

        public IEnumerable<IntroInfo> Get()
        {
            // only relevant on first installation
            // if (Plugin.Instance.Configuration.Id == Guid.Empty)
            // {
            //     Console.WriteLine("No intro ID found, generating one now.");
            //     Cache(Plugin.DefaultIntro);
            // }

            if (Plugin.Instance.Configuration.Local != string.Empty)
            {
                Console.WriteLine("Local Config Detected, retrieving local intros.");
                yield return Local(Plugin.Instance.Configuration.Local);
            }
            else 
            {
                yield break;
            }
            
        }

        private IntroInfo Local(string path)
        {
            var options = new List<string>();
            var location = File.GetAttributes(path);

            var libraryResults = PopulateIntroLibrary(path).ToArray();

            if (libraryResults.Length == 0)
            {
                throw new Exception("No intros found in library");
            }
            else 
            {
                UpdateOptionsConfig(libraryResults);
            }

            var enabledItems = libraryResults.Where(b => Plugin.Instance.Configuration.EnabledLocalVideos.Contains(b.Id)).ToList();

            var selectedItem = enabledItems[_random.Next(enabledItems.Count)];
            return new IntroInfo{
                Path = selectedItem.Path,
                ItemId = selectedItem.Id
            };
        }

        private void UpdateOptionsConfig(IEnumerable<BaseItem> libraryResults)
        {
            Dictionary<Guid, string> options = libraryResults.ToDictionary(x => x.Id, x => x.Name);
            Plugin.Instance.Configuration.DetectedLocalVideos = options.Select(x => new LocalVideo{
                ItemId = x.Key,
                Name = x.Value
            }).ToList();
            if (Plugin.Instance.Configuration.EnabledLocalVideos.Count == 0 || !Plugin.Instance.Configuration.EnabledLocalVideos.Any(x => options.ContainsKey(x)))
            {
                Plugin.Instance.Configuration.EnabledLocalVideos = options.Keys.ToList();
            }
            Plugin.Instance.SaveConfiguration();
        }

        private IEnumerable<BaseItem> PopulateIntroLibrary(string path)
        {
            var attrs = File.GetAttributes(path);
            if (attrs.HasFlag(FileAttributes.Directory))
            {
                var inLibrary = Plugin.LibraryManager.GetItemsResult(new InternalItemsQuery
                {
                    HasAnyProviderId = new Dictionary<string, string>
                    {
                        {"prerolls.video", ""}
                    }
                }).Items;

                var byPath = inLibrary.ToDictionary(x => x.Path, x => x);

                IDictionary<Guid, bool> isFound = inLibrary.ToDictionary(x => x.Id, x => false);

                var byId = inLibrary.ToDictionary(x => x.Id, x => x);

                var filesOnDisk = Directory.EnumerateFiles(path);

                foreach (var file in filesOnDisk)
                {
                    if (byPath.ContainsKey(file))
                    {
                        yield return byPath[file];
                        isFound[byPath[file].Id] = true;
                    }
                    else 
                    {
                        var video = new Video
                        {
                            Id = Guid.NewGuid(),
                            Path = file,
                            ProviderIds = new Dictionary<string, string>
                            {
                                {"prerolls.video", file}
                            },
                            Name = Path.GetFileNameWithoutExtension(file)
                                .Replace("jellyfin", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("pre-roll", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("_", " ", StringComparison.InvariantCultureIgnoreCase)
                                .Replace("-", " ", StringComparison.InvariantCultureIgnoreCase)
                                .Trim()
                        };
                        Plugin.LibraryManager.CreateItem(video, null);
                        yield return video;
                    }
                }
                foreach (var item in isFound.Where(f => !f.Value))
                {
                    Plugin.LibraryManager.DeleteItem(byId[item.Key], new DeleteOptions());
                }
            }
            else if (File.Exists(path))
            {
                var inLibrary = Plugin.LibraryManager.GetItemsResult(new InternalItemsQuery
                {
                    HasAnyProviderId = new Dictionary<string, string>
                    {
                        {"prerolls.video", ""}
                    }
                }).Items;

                var byPath = inLibrary.ToDictionary(x => x.Path, x => x);

                IDictionary<Guid, bool> isFound = inLibrary.ToDictionary(x => x.Id, x => false);

                var byId = inLibrary.ToDictionary(x => x.Id, x => x);

                if (byPath.ContainsKey(path))
                {
                    yield return byPath[path];
                    isFound[byPath[path].Id] = true;
                }
                else 
                {
                    var video = new Video
                    {
                        Id = Guid.NewGuid(),
                        Path = path,
                        ProviderIds = new Dictionary<string, string>
                        {
                            {"prerolls.video", path}
                        },
                        Name = Path.GetFileNameWithoutExtension(path)
                                .Replace("jellyfin", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("pre-roll", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                                .Replace("_", " ", StringComparison.InvariantCultureIgnoreCase)
                                .Replace("-", " ", StringComparison.InvariantCultureIgnoreCase)
                                .Trim()
                    };
                    Plugin.LibraryManager.CreateItem(video, null);
                    yield return video;
                }
                foreach (var item in isFound.Where(f => !f.Value))
                {
                    Plugin.LibraryManager.DeleteItem(byId[item.Key], new DeleteOptions());
                }
            }
            else 
            {
                throw new DirectoryNotFoundException($"Directory Not Found: {path}. Please check your configuration.");
            }
        }
   }
}
