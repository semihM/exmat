using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ExMat.API;
using ExMat.Objects;
using ExMat.VM;
using Newtonsoft.Json;

namespace ExMat.BaseLib
{
    public class ExStdNet
    {
        private static readonly Dictionary<string, string> SearchEngines = new()
        {
            { "Wiki", "https://www.googleapis.com/customsearch/v1/siterestrict?cx=21b0943ef3404ce56" }
        };

        private static readonly Dictionary<string, string> APIFiles = new()
        {
            { "Wiki", "wiki_api.txt" }
        };

        private static string ReadAPIKey(string func)
        {
            return File.ReadAllText(APIFiles[func]).TrimEnd();
        }
        private static string GetSearchLink(string func, string query)
        {
            return SearchEngines[func] + "&key=" + ReadAPIKey(func) + "&q=" + query + "&start=1";
        }

        private static dynamic CreateWebRequest(string link)
        {
            WebRequest request = WebRequest.Create(link);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using StreamReader str = new(response.GetResponseStream());
            string responseText = str.ReadToEnd();
            return JsonConvert.DeserializeObject(responseText);
        }

        public static ExFunctionStatus Wiki(ExVM vm, int nargs)
        {
            if (!File.Exists(APIFiles["Wiki"]))
            {
                return vm.AddToErrorMessage(@"THIS FUNCTION REQUIRES A PERSONAL API KEY. Follow the steps below to make this function work:
    1. Head to https://developers.google.com/custom-search/v1/overview#api_key and create a project and get your API key
    2. Create a file called '" + APIFiles["Wiki"] + @"' in the same directory as 'exmat.exe'
        - Directory for exmat.exe: '" + vm.StartDirectory + @"'
    3. Write your api key into '" + APIFiles["Wiki"] + @"'

    - You can use the following code to create the file(replace APIKEY with your key):
        write_text(""" + string.Format("{0}/{1}", vm.StartDirectory.Replace("\\", "/"), APIFiles["Wiki"]) + @""", ""APIKEY"");");
            }

            List<ExObject> Results = new();

            string query = vm.GetArgument(1).GetString().Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                return vm.CleanReturn(nargs + 2, Results);
            }

            string link = GetSearchLink("Wiki", query);

            try
            {
                dynamic deserializedResponse = CreateWebRequest(link);

                if (deserializedResponse.items != null)
                {
                    foreach (dynamic item in deserializedResponse.items)
                    {
                        Results.Add(new(
                            new List<ExObject>()
                            {
                                new(item.link.ToString()),
                                new(item.title.ToString()),
                                new(item.snippet.ToString())
                            })
                        );
                    }
                }
                return vm.CleanReturn(nargs + 2, Results);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("WIKI SEARCH ERROR: " + e.Message);
            }
        }

        private static readonly List<ExRegFunc> _stdnetfuncs = new()
        {
            new()
            {
                Name = "wiki",
                Function = Wiki,
                nParameterChecks = 2,
                ParameterMask = ".s"
            }
        };
        public static List<ExRegFunc> NetFuncs => _stdnetfuncs;

        public static bool RegisterStdNet(ExVM vm)
        {
            ExAPI.RegisterNativeFunctions(vm, NetFuncs);
            return true;
        }
    }
}
