using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ExMat.API;
using ExMat.Objects;
using ExMat.VM;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ExMat.StdLib
{
    [ExStdLibBase(ExStdLibType.NETWORK)]
    [ExStdLibName("network")]
    [ExStdLibRegister(nameof(Registery))]
    public static class ExStdNet
    {
        #region UTILITY
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

            using StringWriter sw = new();
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
            return sw.ToString();
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

        private static object CreateWebRequest(string link, bool raw = false)
        {
            WebRequest request = WebRequest.Create(new UriBuilder(link).Uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using StreamReader str = new(response.GetResponseStream());
            string responseText = str.ReadToEnd();

            int idx = response.ContentType.IndexOf(';');
            string type = idx == -1 ? response.ContentType : response.ContentType.Substring(0, idx);

            if (raw)
            {
                return responseText;
            }

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

                        using StringWriter sw = new();
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
        #endregion

        #region NET FUNCTIONS
        [ExNativeFuncBase("wiki", ExBaseType.ARRAY, "Get a list of maximum 10 dictionaries of wiki page link, title, and summary of a given subject. Requires an API key to use, call the function for more information.")]
        [ExNativeParamBase(1, "subject", "s", "Subject to search for")]
        public static ExFunctionStatus StdNetWiki(ExVM vm, int nargs)
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
                            new Dictionary<string, ExObject>()
                            {
                                { "link", new(item.link.ToString()) },
                                { "title", new(item.title.ToString()) },
                                { "summary", new(item.snippet.ToString()) }
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

        [ExNativeFuncBase("fetch", ExBaseType.STRING | ExBaseType.DICT | ExBaseType.ARRAY, "Fetch contents of the given url. Response is parsed if it was a json or html response by default, use 'raw' to change it.")]
        [ExNativeParamBase(1, "url", "s", "Url to fetch information from. JSON and HTML files are parsed accordingly unless 'raw' parameter is set to true.")]
        [ExNativeParamBase(2, "raw", ".", "Wheter to return raw response content. Use false to parse json and html responses.", (false))]
        public static ExFunctionStatus StdNetFetch(ExVM vm, int nargs)
        {
            string link = vm.GetArgument(1).GetString().Trim();
            bool raw = nargs == 2 && vm.GetArgument(2).GetBool();

            if (string.IsNullOrWhiteSpace(link))
            {
                return vm.CleanReturn(nargs + 2, new ExObject());
            }

            try
            {
                dynamic deserializedResponse = CreateWebRequest(link, raw);

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

        [ExNativeFuncBase("has_network", ExBaseType.BOOL, "Check if there is any network connection currently 'up'.")]
        public static ExFunctionStatus StdNetHasNetwork(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, NetworkInterface.GetIsNetworkAvailable());
        }

        [ExNativeFuncBase("ip_config", ExBaseType.ARRAY, "Get a list of dictionaries containing adapter and network information")]
        public static ExFunctionStatus StdNetIPConfig(ExVM vm, int nargs)
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            List<ExObject> adapts = new(adapters.Length);

            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                IPInterfaceStatistics statistics = adapter.GetIPStatistics();

                string macbytes = string.Join("-", adapter
                                    .GetPhysicalAddress()
                                    .GetAddressBytes()
                                    .Select(x => x.ToString("X2")));

                string gateway = string.Empty;

                if (adapter.OperationalStatus == OperationalStatus.Up
                    && adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    gateway = string.Join(".", properties.GatewayAddresses
                                .Select(g => g?.Address)
                                .FirstOrDefault(a => a != null)
                                .GetAddressBytes()
                                .Select(x => x.ToString()));
                }

                UnicastIPAddressInformation ipv4 = properties.UnicastAddresses
                            .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);

                Dictionary<string, ExObject> props = new()
                {
                    { "name", new(adapter.Name) },
                    { "description", new(adapter.Description) },
                    { "status", new(adapter.OperationalStatus.ToString()) },
                    { "speed", new(adapter.Speed) },
                    { "bytes_sent", new(statistics.BytesSent) },
                    { "bytes_received", new(statistics.BytesReceived) },

                    { "IPV4", new(ipv4.Address.ToString()) },
                    { "IPV4_mask", new(ipv4.IPv4Mask.ToString()) },
                    { "IPV4_gateway", new(gateway) },

                    { "MAC", new(macbytes) },

                    { "DNS_enabled", new(properties.IsDnsEnabled) },
                    { "dynamic_DNS_enabled", new(properties.IsDynamicDnsEnabled) },
                    { "DNS_suffix", new(properties.DnsSuffix) },
                };

                adapts.Add(new(props));
            }

            return vm.CleanReturn(nargs + 2, adapts);
        }

        #endregion

        // MAIN
        public static ExMat.StdLibRegistery Registery => (ExVM vm) =>
        {
            ExApi.RegisterNativeFunctions(vm, typeof(ExStdNet));

            return true;
        };
    }
}
