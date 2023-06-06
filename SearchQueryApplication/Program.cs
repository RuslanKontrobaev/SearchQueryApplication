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
                // Send GET request to the url and get a response
                HttpResponseMessage response = await client.GetAsync(url);

                // Check succesful response
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ошибка при выполнении запроса: {response.StatusCode}");
                }

                // Resonse in view of stream
                using (var jsonStream = await response.Content.ReadAsStreamAsync())
                {
                    // Decompression response in view of stream
                    using (var decompressedStream = new GZipStream(jsonStream, CompressionMode.Decompress))
                    {
                        // Read the JSON in view of stream
                        using (var jsonDocument = await JsonDocument.ParseAsync(decompressedStream))
                        {
                            // Create options of serialization - formatting with add spaces
                            var options = new JsonSerializerOptions
                            {
                                WriteIndented = true
                            };

                            // Needed to access JSON content
                            var result = jsonDocument.RootElement;
                            // Serialize into string  with options
                            var formattedJson = JsonSerializer.Serialize(result, options);

                            Console.WriteLine(formattedJson);
                            SaveJsonToFile("output.json", formattedJson);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves formatted JSON to a file and displays message about the save operation.
        /// </summary>
        /// <param name="filePath">The file path where JSON will be saved.</param>
        /// <param name="formattedJson">The formatted JSON, which will saved.</param>
        static void SaveJsonToFile(string filePath, string formattedJson)
        {
            File.WriteAllText(filePath, formattedJson);
            Console.WriteLine($"JSON сохранен в файл: {filePath}");
        }
    }
}