using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using MediaBrowser.Common;
using MediaBrowser.Controller.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Jellyfin.Plugin.LocalIntros.Configuration;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.LocalIntros;


[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class LocalIntrosController : ControllerBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="LdapController"/> class.
    /// </summary>
    /// <param name="appHost">The application host to get the LDAP Authentication Provider from.</param>
    public LocalIntrosController(IApplicationHost appHost)
    {
        
    }

    /// <summary>
    /// Tests the server connection and bind settings.
    /// </summary>
    /// <remarks>
    /// Accepts server connection configuration as JSON body.
    /// </remarks>
    /// <response code="200">Server connection was tested.</response>
    /// <response code="400">Body is missing required data.</response>
    /// <param name="body">The request body.</param>
    /// <returns>
    /// An <see cref="OkResult"/> containing the connection results if able to test,
    /// or a <see cref="BadRequestResult"/> if the request body is missing data.
    /// </returns>
    [HttpPost("LoadIntros")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult LoadIntros()
    {
        PopulateIntroLibrary();
        return Ok();
    }


    private static string introsPath => LocalIntrosPlugin.Instance.Configuration.Local;

    private Dictionary<Guid, BaseItem> PopulateIntroLibrary()
    {
        var attrs = System.IO.File.GetAttributes(introsPath);

        bool needsConfigUpdate = false;

        Dictionary<Guid, BaseItem> libraryResults = new Dictionary<Guid, BaseItem>();

        var inLibrary = LocalIntrosPlugin.LibraryManager.GetItemsResult(new InternalItemsQuery
        {
            HasAnyProviderId = new Dictionary<string, string>
            {
                {"prerolls.video", ""}
            }
        }).Items;

        var byPath = inLibrary.ToDictionary(x => x.Path, x => x);

        IDictionary<Guid, bool> isFound = inLibrary.ToDictionary(x => x.Id, x => false);

        var byId = inLibrary.ToDictionary(x => x.Id, x => x);

        IEnumerable<string> filesOnDisk;

        if (attrs.HasFlag(FileAttributes.Directory))
        {
            filesOnDisk = Directory.EnumerateFiles(introsPath);
        }
        else if (System.IO.File.Exists(introsPath))
        {
            filesOnDisk = new List<string> { introsPath };
        }
        else 
        {
            throw new DirectoryNotFoundException($"Directory Not Found: {introsPath}. Please check your configuration.");
        }

        var configDetectedVideos = LocalIntrosPlugin.Instance.Configuration.DetectedLocalVideos.Select(x => x.ItemId).ToHashSet();

        foreach (var file in filesOnDisk)
        {
            if (byPath.ContainsKey(file))
            {
                Console.WriteLine($"Found {file} in library.");
                isFound[byPath[file].Id] = true;
                libraryResults[byPath[file].Id] = byPath[file];
                if (!configDetectedVideos.Contains(byPath[file].Id))
                {
                    LocalIntrosPlugin.Instance.Configuration.DetectedLocalVideos.Add(new IntroVideo{
                        ItemId = byPath[file].Id,
                        Name = byPath[file].Name
                    });
                    needsConfigUpdate = true;
                }
            }
            else 
            {   
                Console.WriteLine($"Adding {file} to library.");
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
                LocalIntrosPlugin.LibraryManager.CreateItem(video, null);
                needsConfigUpdate = true;
                libraryResults[video.Id] = video;
            }
        }
        foreach (var item in isFound.Where(f => !f.Value))
        {
            Console.WriteLine($"Removing {byId[item.Key].Path} from library.");
            LocalIntrosPlugin.LibraryManager.DeleteItem(byId[item.Key], new DeleteOptions());
        }
        if (libraryResults.Count > 0)
        {
            if (inLibrary.Count() == 0)
            {
                LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos.Add(libraryResults.First().Key);
            }
            if (needsConfigUpdate) 
            {
                UpdateOptionsConfig(libraryResults.Values);
            }
        }
        return libraryResults;
    }

    private void UpdateOptionsConfig(IEnumerable<BaseItem> libraryResults)
    {
        // Dictionary so we can use ContainsKey
        
        if (LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos.Count + LocalIntrosPlugin.Instance.Configuration.StudioIntros.Count + LocalIntrosPlugin.Instance.Configuration.TagIntros.Count + LocalIntrosPlugin.Instance.Configuration.GenreIntros.Count == 0)
        {
            LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos.Add(libraryResults.First().Id);
        }
        //And then to the List as we need for saving. (XML can't serialize Dictionaries..)
        LocalIntrosPlugin.Instance.Configuration.DetectedLocalVideos = libraryResults.Select(x => new IntroVideo{
            ItemId = x.Id,
            Name = x.Name
        }).ToList();
        LocalIntrosPlugin.Instance.SaveConfiguration();
    }

}