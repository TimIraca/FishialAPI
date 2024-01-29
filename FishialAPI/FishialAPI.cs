using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace FishialAPI
{
    public class DirectUploadHeaders
    {
        [JsonProperty("Content-MD5")]
        public string ContentMD5 { get; set; }

        [JsonProperty("Content-Disposition")]
        public string ContentDisposition { get; set; }
    }
    public class DirectUpload
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("headers")]
        public DirectUploadHeaders Headers { get; set; }
    }
    public class AuthResponse
    {
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("direct-upload")]
        public DirectUpload DirectUpload { get; set; }

        [JsonProperty("signed-id")]
        public string SignedId { get; set; }
    }
    public class FishialAPI
    {
        private string publickey = "your key here";
        private string privatekey = "your key here";
        private static readonly HttpClient httpClient = new HttpClient();
        public event EventHandler<string> Message;

        private void Notify(string message)
        {
            Message?.Invoke(this, message);
        }
        internal async Task<AuthResponse> Request_URL(string accessToken, ImageMetadata metadata)
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.fishial.ai/v1/recognition/upload"))
            {
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

                var jsonPayload = new
                {
                    blob = new
                    {
                        filename = metadata.FileName,
                        content_type = metadata.MimeType,
                        byte_size = metadata.ByteSize,
                        checksum = metadata.Checksum
                    }
                };

                string jsonContent = JsonConvert.SerializeObject(jsonPayload);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    AuthResponse apiResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);

                    Notify("Success: " + responseContent);

                    if (apiResponse.DirectUpload != null)
                    {
                        Notify("Direct Upload URL: " + apiResponse.DirectUpload.Url);
                        Notify("Signed ID: " + apiResponse.SignedId);

                        if (apiResponse.DirectUpload.Headers != null)
                        {
                            Notify("Content-MD5: " + apiResponse.DirectUpload.Headers.ContentMD5);
                            Notify("Content-Disposition: " + apiResponse.DirectUpload.Headers.ContentDisposition);
                            return apiResponse;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    Notify("Error: " + response.StatusCode + " - " + response.ReasonPhrase);
                    return null;
                }
            }
        }
        internal async Task<AuthResponse> Request_accesstoken()
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api-users.fishial.ai/v1/auth/token"))
            {
                var jsonContent = $"{{\"client_id\": \"{publickey}\", \"client_secret\": \"{privatekey}\"}}";
                request.Content = new StringContent(jsonContent);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    AuthResponse authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);

                    Notify("Token Type: " + authResponse.TokenType);
                    Notify("Access Token: " + authResponse.AccessToken);
                    return authResponse;
                }
                else
                {
                    Notify("Error: " + response.StatusCode + " - " + response.ReasonPhrase);
                    return null;
                }
            }
        }
        internal async Task UploadImage(string imagePath, DirectUpload directUpload)
        {

            using (var request = new HttpRequestMessage(new HttpMethod("PUT"), directUpload.Url))
            {
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                request.Content = new ByteArrayContent(imageBytes);

                request.Content.Headers.TryAddWithoutValidation("Content-Disposition", directUpload.Headers.ContentDisposition);
                request.Content.Headers.TryAddWithoutValidation("Content-MD5", directUpload.Headers.ContentMD5);

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Notify("Image upload successful");
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Notify($"Image upload failed. Status: {response.StatusCode}, Body: {responseBody}");
                }
            }
        }
        internal async Task Recognize_Fish(AuthResponse authResponse, string FileName)
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://api.fishial.ai/v1/recognition/image?q=" + authResponse.SignedId))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {authResponse.AccessToken}");

                var response = await httpClient.SendAsync(request);


                string responseContent = await response.Content.ReadAsStringAsync();
                AuthResponse apiResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);

                if (response.IsSuccessStatusCode)
                {
                    Notify($"Recognize successful: {responseContent}");
                    FishResult fishResult = new();
                    fishResult.Message += (sender, message) => Notify(message);
                    await fishResult.FishResults(responseContent);
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Notify($"recognize failed. Status: {response.StatusCode}, Body: {responseBody}");
                }
            }
        }

        internal async Task ProcessNewImage(string imagePath)
        {
            var metadata = new ImageMetadata(imagePath);

            Notify("File Name: " + metadata.FileName);
            Notify("MIME Type: " + metadata.MimeType);
            Notify("Byte Size: " + metadata.ByteSize);
            Notify("MD5 Checksum: " + metadata.Checksum);

            var authResponse = await Request_accesstoken();
            if (authResponse != null)
            {
                var apiResponse = await Request_URL(authResponse.AccessToken, metadata);
                if (apiResponse.DirectUpload != null)
                {
                    await UploadImage(imagePath, apiResponse.DirectUpload);
                    await Recognize_Fish(apiResponse, metadata.FileName);
                }
                else
                {
                    Notify("Failed to obtain direct upload details.");
                }
            }
            else
            {
                Notify("Failed to obtain access token.");
            }
        }
    }
}
