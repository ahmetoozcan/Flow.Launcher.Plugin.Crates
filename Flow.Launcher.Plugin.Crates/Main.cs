using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Crates
{
    public class Crates : IAsyncPlugin, IContextMenu
    {
        private PluginInitContext _context;

        // From https://crates.io/data-access 
        // "A maximum of 1 request per second."
        private static DateTime _lastRequestTime = DateTime.MinValue;

        private readonly string _crateBaseUrl = "https://crates.io/crates";

        // TODO: Setting to change the number of query results
        private readonly string _queryUrl = "https://crates.io/api/v1/crates?page=1&per_page=20&q=";

        public async Task InitAsync(PluginInitContext context)
        {
            _context = context;
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            List<Result> results = new List<Result>();
            string[] args = query.RawQuery.Split(' ');
            string url = _queryUrl;

            // No query args no results 
            if (args.Length < 2){
                return results;
            }

            // Skip the first index as it contains the plugin's ActionKeyword and is not part of the search query
            for (int index = 1; index < args.Length; index++)
            {
                url += args[index];
                if (index != args.Length - 1) 
                    url += "%20";
            }

            // Calculate the time difference between the current request and the last request
            TimeSpan timeSinceLastRequest = DateTime.Now - _lastRequestTime;
            if (timeSinceLastRequest.TotalMilliseconds < 1000)
            {
                await Task.Delay(1000 - (int)timeSinceLastRequest.TotalMilliseconds, token);
            }

            using (var client = new System.Net.Http.HttpClient())
            {
                // From https://crates.io/data-access 
                // "A user-agent header that identifies your application. We strongly suggest 
                // providing a way for us to contact you (whether through a repository, 
                // or an e-mail address, or whatever is appropriate) so that we can 
                // reach out to work with you should there be issues."
                client.DefaultRequestHeaders.Add("User-Agent", "Flow.Launcher.Plugin.Crates/1.0 (contact: ahmetozcan21@yahoo.com, github:https://github.com/ahmetoozcan)");
                
                var response = client.GetAsync(url, token).Result;
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = response.Content.ReadAsStringAsync().Result;
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        JsonElement root = doc.RootElement;
                        JsonElement crates = root.GetProperty("crates");
                        
                        if (!crates.EnumerateArray().Any())
                        {
                            results.Add(new Result
                            {
                                Title = "No crates found",
                                SubTitle = "No crates match your search query.",
                                IcoPath = IconProvider.Crate
                            });
                            return results;
                        }

                        foreach (JsonElement crate in crates.EnumerateArray())
                        {
                            string crateId = crate.GetProperty("id").GetString();
                            string crateName = crate.GetProperty("name").GetString();
                            string crateDescription = crate.GetProperty("description").GetString();
                            string crateUrl = $"{_crateBaseUrl}/{crateId}";


                            CrateContextData crateContextData = new CrateContextData
                            {
                                Downloads = crate.GetProperty("downloads").ToString(),
                                RepositoryUrl = crate.GetProperty("repository").ToString(),
                                LatestVersion = crate.GetProperty("max_version").ToString(),
                                LatestStableVersion = crate.GetProperty("max_stable_version").ToString(),
                                CrateUrl = crateUrl
                            };

                            results.Add(new Result
                            {
                                Title = crateName,
                                SubTitle = crateDescription,
                                IcoPath = IconProvider.Crate,
                                ContextData = crateContextData,
                                Action = e =>
                                {
                                    try
                                    {
                                        _context.API.OpenUrl(crateUrl);
                                        return true;
                                    }
                                    catch (Exception ex)
                                    {
                                        _context.API.LogException("Crates", "Exception occurred while opening URL", ex);
                                        return false;
                                    }
                                }
                            });
                        }
                    }
                }
            }

            // Update the last request time
            _lastRequestTime = DateTime.Now;

            return results;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            List<Result> contextResults = new List<Result>();

            if (selectedResult.ContextData is CrateContextData contextData)
            {
                if (!string.IsNullOrEmpty(contextData.RepositoryUrl))
                {
                    contextResults.Add(new Result
                    {
                        Title = "Crate Repository",
                        SubTitle = contextData.RepositoryUrl,
                        IcoPath = IconProvider.Github,
                        Action = e =>
                        {
                            try
                            {
                                _context.API.OpenUrl(contextData.RepositoryUrl);
                                return true;
                            }
                            catch (Exception ex)
                            {
                                _context.API.LogException("Crates", "Exception occurred while opening repository URL", ex);
                                return false;
                            }
                        }
                    });
                }

                if (!string.IsNullOrEmpty(contextData.Downloads))
                {
                    contextResults.Add(new Result
                    {
                        Title = "Downloads",
                        SubTitle = int.Parse(contextData.Downloads).ToString("N0"),
                        IcoPath = IconProvider.Download
                    });
                }

                if (!string.IsNullOrEmpty(contextData.LatestStableVersion))
                {
                    contextResults.Add(new Result
                    {
                        Title = "Latest Stable Version",
                        SubTitle = contextData.LatestStableVersion,
                        IcoPath = IconProvider.Version,
                        Action = e =>
                        {
                            try
                            {
                                _context.API.OpenUrl($"{contextData.CrateUrl}/{contextData.LatestStableVersion}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                _context.API.LogException("Crates", "Exception occurred while opening latest stable version URL", ex);
                                return false;
                            }
                        }
                    });
                }

                if (!string.IsNullOrEmpty(contextData.LatestVersion))
                {
                    contextResults.Add(new Result
                    {
                        Title = "Latest Version",
                        SubTitle = contextData.LatestVersion,
                        IcoPath = IconProvider.Version,
                        Action = e =>
                        {
                            try
                            {
                                _context.API.OpenUrl($"{contextData.CrateUrl}/{contextData.LatestVersion}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                _context.API.LogException("Crates", "Exception occurred while opening latest version URL", ex);
                                return false;
                            }
                        }
                    });
                }
            }

            return contextResults;
        }

    }
}