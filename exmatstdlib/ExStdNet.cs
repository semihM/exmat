using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ExMat.API;
using ExMat.Objects;
using ExMat.VM;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        /// <summary>
        /// Converts HTML to plain text / strips tags.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns></returns>
        public static string ConvertToPlainText(string html)
        {
            HtmlDocument doc = new();
            doc.LoadHtml(html);

            StringWriter sw = new();
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
            return sw.ToString();
        }

        /// <summary>
        /// Count the words.
        /// The content has to be converted to plain text before (using ConvertToPlainText).
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <returns></returns>
        public static int CountWords(string plainText)
        {
            return !string.IsNullOrEmpty(plainText) ? plainText.Split(' ', '\n').Length : 0;
        }

        public static string Cut(string text, int length)
        {
            if (!string.IsNullOrEmpty(text) && text.Length > length)
            {
                text = text.Substring(0, length - 4) + " ...";
            }
            return text;
        }

        private static void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }

        private static void ConvertTo(HtmlNode node, TextWriter outText)
        {
            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                    {
                        break;
                    }

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                    {
                        break;
                    }

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html) + ' ');
                    }
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
                        case "p":
                            // treat paragraphs as crlf
                            outText.Write("\r\n");
                            break;
                        case "br":
                            outText.Write("\r\n");
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }
                    break;
            }
        }

        private static object CreateWebRequest(string link)
        {
            WebRequest request = WebRequest.Create(new UriBuilder(link).Uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using StreamReader str = new(response.GetResponseStream());
            string responseText = str.ReadToEnd();

            int idx = response.ContentType.IndexOf(';');
            string type = idx == -1 ? response.ContentType : response.ContentType.Substring(0, idx);

            switch (type)
            {
                case "application/json":
                    {
                        return JsonConvert.DeserializeObject(responseText);
                    }
                case "text/html":
                    {
                        HtmlDocument doc = new();
                        doc.LoadHtml(responseText);

                        StringWriter sw = new();
                        ConvertTo(doc.DocumentNode, sw);
                        sw.Flush();
                        return sw.ToString();
                    }
                default:
                    {
                        return null;
                    }
            }
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

        public static ExFunctionStatus Fetch(ExVM vm, int nargs)
        {
            string link = vm.GetArgument(1).GetString().Trim();

            if (string.IsNullOrWhiteSpace(link))
            {
                return vm.CleanReturn(nargs + 2, new ExObject());
            }

            try
            {
                dynamic deserializedResponse = CreateWebRequest(link);

                if (deserializedResponse is string res)
                {
                    return vm.CleanReturn(nargs + 2, res);
                }
                else if (deserializedResponse is JObject obj && obj.Count > 0)
                {
                    return vm.CleanReturn(nargs + 2, ExStdIO.GetJsonContent(obj));
                }
                return vm.CleanReturn(nargs + 2, new ExObject());
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("FETCH ERROR: " + e.Message);
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
            },

            new()
            {
                Name = "fetch",
                Function = Fetch,
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
