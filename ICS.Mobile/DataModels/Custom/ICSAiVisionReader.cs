using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace ICS.Portal.Data.Custom
{
    public class ICSAiVisionReader
    {

        // Process Dataplate Images - capture text using Azure Vision Analytics
        // HTTPS - Client -> Azure -> Client

        // Azure Vision Analytics keys
        private const string azvkey = "f540197e1df5405d88315ddaf4e42a5e";
        private const string azvep = "https://icsdataplaterdr.cognitiveservices.azure.com";

        public string AzureVisionAnalyticsKey { set; get; } = azvkey;
        public string AzureVisionAnalyticsEndpoint { set; get; } = "https://icsdataplaterdr.cognitiveservices.azure.com";
        public bool IsVisionAnalyticsEnabled { set; get; } = true;

        private const int DefaultWaitTime = 1500;
        private HttpClient client = new HttpClient();

        public ICSAiVisionReader(string? AzVisionAnalyticsKey = azvkey, string? AzVisionAnalyticsEndpoint = azvep)
        {

            _ = Initialize(AzVisionAnalyticsKey, AzVisionAnalyticsEndpoint);
        }

        ~ICSAiVisionReader()
        {
            
        }

        public bool Initialize(string? VisionAnalyticsKey = azvkey, string? VisionAnalyticsEndpoint = azvep)
        {
            if (string.IsNullOrEmpty(VisionAnalyticsKey))
                VisionAnalyticsKey = azvkey;
            if (string.IsNullOrEmpty(VisionAnalyticsEndpoint))
                VisionAnalyticsEndpoint = azvep;

            AzureVisionAnalyticsKey = VisionAnalyticsKey;
            AzureVisionAnalyticsEndpoint = VisionAnalyticsEndpoint;

            IsVisionAnalyticsEnabled = true;

            return (IsVisionAnalyticsEnabled);
            
        }


        public async Task<string[]> ReadDataplate(byte[]? MakeImage = null, byte[]? ModelImage = null, byte[]? SerialImage = null, int? msWaitForOCR = 1500, char multiLineDelimeter = '\a')
        {
            if (MakeImage is null && ModelImage is null && SerialImage is null)
            {
                Console.WriteLine("DATAPLATE READER: No images to read");
                return Array.Empty<string>();
            }

            List<string> operationUrls = new List<string>();
            List<string> returnText = new List<string>();

            try
            {
                if (MakeImage is not null)
                {
                    operationUrls.Add(await AnalyzeImageAsync(MakeImage));
                }
                if (ModelImage is not null)
                {
                    operationUrls.Add(await AnalyzeImageAsync(ModelImage));
                }
                if (SerialImage is not null)
                {
                    operationUrls.Add(await AnalyzeImageAsync(SerialImage));
                }

                // Test with filesList of image file paths
                //List<string> imagePaths = new List<string>
                //{
                //    "c:\\temp\\make.png",
                //    "c:\\temp\\model.png",
                //    "c:\\temp\\serial.png"
                //};
                //    // Loop through each image and analyze it
                //    foreach (var imagePath in imagePaths)
                //    {
                //        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
                //        var operationUrl = await AnalyzeImageAsync(imageBytes);
                //        operationUrls.Add(operationUrl);
                //    }


                //await Task.Delay(msWaitForOCR.GetValueOrDefault(DefaultWaitTime) * operationUrls.Count);
                await Task.Delay(msWaitForOCR.GetValueOrDefault(DefaultWaitTime));

                foreach (var operationUrl in operationUrls)
                {
                    var result = await GetAnalysisResultAsync(operationUrl);
                    if (result is not null && result.Length > 0)
                    {
                        StringBuilder textLineBuilder = new StringBuilder();
                        foreach (var line in result)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                if (textLineBuilder.Length > 0)
                                    textLineBuilder.Append(multiLineDelimeter);
                                textLineBuilder.Append(line);
                            }
                        }
                        returnText.Add(textLineBuilder.ToString().Trim());
                    }
                    else
                    {
                        Console.WriteLine($"DATAPLATE READER: No results for {operationUrl}");
                    }
                }

                return returnText.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        private async Task<string> AnalyzeImageAsync(byte[] imageBytes)
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AzureVisionAnalyticsKey);

            using (ByteArrayContent content = new ByteArrayContent(imageBytes))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                HttpResponseMessage response = await client.PostAsync($"{AzureVisionAnalyticsEndpoint}/vision/v3.2/read/analyze", content);
                response.EnsureSuccessStatusCode();

                return response.Headers.GetValues("Operation-Location").FirstOrDefault();
            }
        }

        private async Task<string[]> GetAnalysisResultAsync(string operationUrl)
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AzureVisionAnalyticsKey);
            int quitAfter = 5, quitCount = 0;
            string status;
            string resultJson;
            do
            {
                HttpResponseMessage response = await client.GetAsync(operationUrl);
                response.EnsureSuccessStatusCode();
                resultJson = await response.Content.ReadAsStringAsync();

                using (JsonDocument document = JsonDocument.Parse(resultJson))
                {
                    status = document.RootElement.GetProperty("status").GetString();
                }

                if (status != "succeeded")
                {
                    ++quitCount;
                    if (quitCount >= quitAfter)
                    {
                        Console.WriteLine($"DATAPLATE READER: Operation timed out after {quitAfter} attempts");
                        break;
                    }
                    await Task.Delay(DefaultWaitTime);
                    
                }
            } while (status != "succeeded");

            return ParseResultJson(resultJson);
        }

        private string[] ParseResultJson(string resultJson)
        {
            if (string.IsNullOrEmpty(resultJson))
            {
                return Array.Empty<string>();
            }

            using (JsonDocument document = JsonDocument.Parse(resultJson))
            {
                JsonElement root = document.RootElement;
                JsonElement readResults = root.GetProperty("analyzeResult").GetProperty("readResults");

                var lines = new List<string>();
                foreach (JsonElement readResult in readResults.EnumerateArray())
                {
                    foreach (JsonElement line in readResult.GetProperty("lines").EnumerateArray())
                    {
                        lines.Add(line.GetProperty("text").GetString());
                    }
                }

                return lines.ToArray();
            }
        }
    }
}
