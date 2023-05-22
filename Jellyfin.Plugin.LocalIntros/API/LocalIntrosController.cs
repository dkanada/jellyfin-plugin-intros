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
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.LocalIntros;


[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class LocalIntrosController : ControllerBase
{
    private readonly ILogger<LocalIntrosController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LdapController"/> class.
    /// </summary>
    /// <param name="appHost">The application host to get the LDAP Authentication Provider from.</param>
    public LocalIntrosController(IApplicationHost appHost, ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<LocalIntrosController>();
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
        logger.LogDebug("Loading Intros");
        PopulateIntroLibrary();
        return Ok();
    }
    
    [HttpPost("ClearIntros")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ClearIntros()
    {
        logger.LogInformation("Clearing Intros");
        LocalIntrosPlugin.LibraryManager.GetItemsResult(new InternalItemsQuery
        {
            HasAnyProviderId = new Dictionary<string, string>
            {
                {"prerolls.video", ""}
            }
        }).Items.ToList().ForEach(x => 
        {
            logger.LogInformation($"Removing {x.Path} from library.");
            LocalIntrosPlugin.LibraryManager.DeleteItem(x, new DeleteOptions());
        });
        return Ok();
    }


    private static string introsPath => LocalIntrosPlugin.Instance.Configuration.Local;

    private Dictionary<Guid, BaseItem> PopulateIntroLibrary()
    {
        logger.LogTrace($"Retrieving attributes of {introsPath}");
        var attrs = System.IO.File.GetAttributes(introsPath);

        bool needsConfigUpdate = false;

        Dictionary<Guid, BaseItem> libraryResults = new Dictionary<Guid, BaseItem>();

        logger.LogTrace($"Retrieving existing items from library.");
        var inLibrary = LocalIntrosPlugin.LibraryManager.GetItemsResult(new InternalItemsQuery
        {
            HasAnyProviderId = new Dictionary<string, string>
            {
                {"prerolls.video", ""}
            }
        }).Items;
        logger.LogInformation($"Found {inLibrary.Count()} items in library.");

        logger.LogTrace($"Creating dictionaries for comparison. (path => item, id => isFound, id => item)");
        var byPath = inLibrary.ToDictionary(x => x.Path, x => x);
        var isFound = inLibrary.ToDictionary(x => x.Id, x => false);
        var byId = inLibrary.ToDictionary(x => x.Id, x => x);

        IEnumerable<string> filesOnDisk;

        if (attrs.HasFlag(FileAttributes.Directory))
        {
            logger.LogInformation($"Retrieving files from directory at {introsPath}");
            filesOnDisk = Directory.EnumerateFiles(introsPath);
        }
        else if (System.IO.File.Exists(introsPath))
        {
            logger.LogInformation($"Retrieving file at {introsPath}");
            filesOnDisk = new FancyList<string> { introsPath };
        }
        else 
        {
            throw new DirectoryNotFoundException($"Directory Not Found: {introsPath}. Please check your configuration.");
        }

        logger.LogTrace($"Retrieving item IDs in configuration file");
        var configDetectedVideos = LocalIntrosPlugin.Instance.Configuration.DetectedLocalVideos.Select(x => x.ItemId).ToHashSet();

        logger.LogTrace($"Comparing files on disk to items in library.");
        foreach (var file in filesOnDisk)
        {
            if (byPath.ContainsKey(file))
            {
                logger.LogTrace($"Found {file} in library, marking as found and adding to results.");
                isFound[byPath[file].Id] = true;
                libraryResults[byPath[file].Id] = byPath[file];
                if (!configDetectedVideos.Contains(byPath[file].Id) && !needsConfigUpdate)
                {
                    logger.LogInformation("Flagging for config update.");
                    needsConfigUpdate = true;
                }
            }
            else 
            {
                logger.LogInformation($"Adding {file} to library and adding to results.");
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
                if (!needsConfigUpdate)
                {
                    logger.LogInformation("Flagging for config update.");
                    needsConfigUpdate = true;
                }
                libraryResults[video.Id] = video;
            }
        }
        foreach (var item in isFound.Where(f => !f.Value))
        {
            logger.LogWarning($"Removing {byId[item.Key].Path} from library.");
            LocalIntrosPlugin.LibraryManager.DeleteItem(byId[item.Key], new DeleteOptions());
        }
        if (libraryResults.Count > 0)
        {
            if (inLibrary.Count() == 0)
            {
                logger.LogInformation($"No existing items in library, erasing configuration.");
                LocalIntrosPlugin.Instance.Configuration.CurrentDateIntros = new List<CurrentDateRangeIntro>();
                LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos = new List<Guid>();
                LocalIntrosPlugin.Instance.Configuration.DetectedLocalVideos = new List<IntroVideo>();
                LocalIntrosPlugin.Instance.Configuration.GenreIntros = new List<GenreIntro>();
                LocalIntrosPlugin.Instance.Configuration.StudioIntros = new List<StudioIntro>();
                LocalIntrosPlugin.Instance.Configuration.TagIntros = new List<TagIntro>();
                
                UpdateOptionsConfig(libraryResults.Values);
            }
            if (needsConfigUpdate) 
            {
                logger.LogInformation($"Updating configuration file.");
                UpdateOptionsConfig(libraryResults.Values);
            }
        }
        if (libraryResults.Count == 0)
        {
            logger.LogWarning($"No videos found in {introsPath}, updating configuration file.");
            UpdateOptionsConfig(libraryResults.Values);
        }
        return libraryResults;
    }
    
    private void CleanList(ICollection<Guid> listToClean, HashSet<Guid> existingItems)
    {
        listToClean.Where(x => !existingItems.Contains(x)).ToList().ForEach(x => listToClean.Remove(x));
    }
    
    private void CleanList<TIntro>(List<TIntro> listToClean, HashSet<Guid> existingItems)
        where TIntro : ISpecialIntro
    {
        listToClean.Where(x => !existingItems.Contains(x.IntroId)).ToList().ForEach(x => listToClean.Remove(x));
    }

    private void UpdateOptionsConfig(IEnumerable<BaseItem> libraryResults)
    {
        // Dictionary so we can use ContainsKey
        logger.LogTrace($"Adding detected videos to configuration.");
        LocalIntrosPlugin.Instance.Configuration.DetectedLocalVideos = libraryResults.Select(x => new IntroVideo{
            ItemId = x.Id,
            Name = x.Name
        }).ToList();
        
        var validIds = LocalIntrosPlugin.Instance.Configuration.DetectedLocalVideos.Select(x => x.ItemId).ToHashSet();
        
        CleanList(LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos, validIds);
        CleanList(LocalIntrosPlugin.Instance.Configuration.StudioIntros, validIds);
        CleanList(LocalIntrosPlugin.Instance.Configuration.TagIntros, validIds);
        CleanList(LocalIntrosPlugin.Instance.Configuration.GenreIntros, validIds);
        CleanList(LocalIntrosPlugin.Instance.Configuration.CurrentDateIntros, validIds);
        
        logger.LogTrace($"Checking to see if there are any configured videos...");
        if (LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos.Count + LocalIntrosPlugin.Instance.Configuration.StudioIntros.Count + LocalIntrosPlugin.Instance.Configuration.TagIntros.Count + LocalIntrosPlugin.Instance.Configuration.GenreIntros.Count == 0)
        {
            logger.LogInformation($"No configured videos found, adding first video to default.");
            LocalIntrosPlugin.Instance.Configuration.DefaultLocalVideos.Add(libraryResults.First().Id);
        }
        
        //And then to the List as we need for saving. (XML can't serialize Dictionaries..)
        LocalIntrosPlugin.Instance.SaveConfiguration();
    }

}