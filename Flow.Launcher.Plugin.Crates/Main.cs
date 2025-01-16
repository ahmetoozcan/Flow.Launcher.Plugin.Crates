using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Crates
{
    public class Crates : IPlugin, IContextMenu
    {
        private PluginInitContext _context;

        private readonly string crateBaseUrl = "https://crates.io/crates";

        // TODO: Setting to change the number of query results
        private readonly string queryUrl = "https://crates.io/api/v1/crates?page=1&per_page=20&q=";

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            string[] args = query.RawQuery.Split(' ');
            string url = queryUrl;

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

            using (var client = new System.Net.Http.HttpClient())
            {
                // Add User-Agent header
                client.DefaultRequestHeaders.Add("User-Agent", "Flow.Launcher.Plugin.Crates/1.0 (contact: ahmetozcan21@yahoo.com, github:https://github.com/ahmetoozcan)");

                var response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = response.Content.ReadAsStringAsync().Result;
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        JsonElement root = doc.RootElement;
                        JsonElement crates = root.GetProperty("crates");
            
                        foreach (JsonElement crate in crates.EnumerateArray())
                        {
                            string crateId = crate.GetProperty("id").GetString();
                            string crateName = crate.GetProperty("name").GetString();
                            string crateDescription = crate.GetProperty("description").GetString();
                            string crateUrl = $"{crateBaseUrl}/{crateId}";


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