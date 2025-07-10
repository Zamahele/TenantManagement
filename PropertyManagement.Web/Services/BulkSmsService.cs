using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class BulkSmsService : ISmsService
{
    private readonly string _username;
    private readonly string _password;
    private readonly HttpClient _httpClient;

    public BulkSmsService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _username = config["BulkSMS:Username"];
        _password = config["BulkSMS:Password"];
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task SendAsync(string phoneNumber, string message)
    {
        var url = "https://api.bulksms.com/v1/messages";
        var payload = new
        {
            to = new[] { phoneNumber },
            body = message
        };
        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var byteArray = Encoding.ASCII.GetBytes($"{_username}:{_password}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}