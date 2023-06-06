using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SearchQueryApplication
{
    class Program
    {
        static async Task Main()
        {
            Console.Write("Введите поисковой запрос: ");
            string query = Console.ReadLine();

            const string baseUrl = "https://api.stackexchange.com/2.3/search?";
            const string site = "stackoverflow";
            const string key = "gpBMx)*w8vbrV)naEvIKWw((";
            string paramsUrl = $"order=desc&sort=activity&intitle={query}&site={site}&type=jsontext&key={key}";
            string url = baseUrl + paramsUrl;

            using (HttpClient client = new HttpClient())
            {
                // Send GET request to the url and get a response.
                HttpResponseMessage response = await client.GetAsync(url);

                // Check succesful response.
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ошибка при выполнении запроса: {response.StatusCode}");
                }

                // Resonse in view of stream.
                using (var jsonStream = await response.Content.ReadAsStreamAsync())
                {
                    // Decompression response in view of stream.
                    using (var decompressedStream = new GZipStream(jsonStream, CompressionMode.Decompress))
                    {
                        // Read the JSON in view of stream.
                        using (var jsonDocument = await JsonDocument.ParseAsync(decompressedStream))
                        {
                            // Create options of serialization - formatting with add spaces.
                            var options = new JsonSerializerOptions
                            {
                                WriteIndented = true
                            };

                            // Needed to access JSON content.
                            var result = jsonDocument.RootElement;
                            // Serialize into string  with options.
                            var formattedJson = System.Text.Json.JsonSerializer.Serialize(result, options);

                            Console.WriteLine(formattedJson);

                            SaveJsonToFile("output.json", formattedJson);

                            string html = CreateHtml(query, "output.json");
                            SaveHtmlToFile(html, "output.html");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves formatted JSON to a file and displays message about the save operation.
        /// </summary>
        /// <param name="filePath">The file path where JSON will be saved.</param>
        /// <param name="json">The JSON which needs to be saved.</param>
        static void SaveJsonToFile(string filePath, string json)
        {
            File.WriteAllText(filePath, json);
            Console.WriteLine($"JSON сохранен в файл: {filePath}.");
        }

        /// <summary>
        /// Saves html document to a file and displays message about the save operation.
        /// </summary>
        /// <param name="html">The HTML document which needs to be saved.</param>
        /// <param name="filePath">The file path where HTML will be saved.</param>
        public static void SaveHtmlToFile(string html, string filePath)
        {
            File.WriteAllText(filePath, html);
            Console.WriteLine($"HTML-документ сохранен в файл: {filePath}.");
        }

        /// <summary>
        /// Execute JSON reading and parsing, generate HTML document based on some items on it.
        /// </summary>
        /// <param name="header">The header of HTML document.</param>
        /// <param name="filePath">The JSON file path.</param>
        /// <returns></returns>
        static string CreateHtml(string header, string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);

                // Represent the JSON.
                var jsonObject = JObject.Parse(json);
                // Get properties every item in JSON.
                var items = jsonObject["items"];

                string htmlTable = GenerateHtmlTable(items);
                string htmlDocument = GenerateHtmlDocument(header, htmlTable);

                return htmlDocument;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Файл '{filePath}' не найден.");
                return null;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
                return null;
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"Ошибка при чтении JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate HTML empty document with some content.
        /// </summary>
        /// <param name="header">String which be displayed into Title and Header.</param>
        /// <param name="htmlContent">Some content that needs to be included in HTML document.</param>
        /// <returns>String - HTML with content.</returns>
        public static string GenerateHtmlDocument(string header, string htmlContent)
        {
            string html = "<!DOCTYPE html>";
            html += "<html>";
            html += "<head>";
            html += $"<title>Search query results: {header}</title>";
            html += "</head>";
            html += "<body>";
            html += $"<h1 align=\"center\">Search query results: {header}</h1>";
            html += htmlContent;
            html += "</body>";
            html += "</html>";

            return html;
        }

        /// <summary>
        /// Generate HTML table based on JSON items.
        /// </summary>
        /// <param name="items">Items of JSON which needed to be in table.</param>
        /// <returns>String - HTML table with required items.</returns>
        public static string GenerateHtmlTable(JToken items)
        {
            string html = "<table border=\"1\">";
            html += "<tr>" +
                    "<th>Creation date</th>" +
                    "<th>Title</th>" +
                    "<th>Author</th>" +
                    "<th>Answered</th>" +
                    "<th>Link</th>" +
                    "</tr>";

            foreach (var item in items)
            {
                string creationDate = UnixTimeStampToDateTime((long)item["creation_date"]).ToString();
                string title = item["title"].ToString();
                string author = item["owner"]["display_name"].ToString();
                string answered = item["is_answered"].ToString();
                string link = item["link"].ToString();

                html += $"<tr>" +
                        $"<td>{creationDate}</td><" +
                        $"td>{title}</td>" +
                        $"<td>{author}</td>" +
                        $"<td>{answered}</td>" +
                        $"<td>{link}</td>" +
                        $"</tr>";
            }

            html += "</table>";
            return html;
        }

        /// <summary>
        /// Converts Unix timestamp to DateTime.
        /// </summary>
        /// <param name="unixTimeStamp">The Unix timestamp which needed to be convert.</param>
        /// <returns>The equivalent DateTime.</returns>
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}