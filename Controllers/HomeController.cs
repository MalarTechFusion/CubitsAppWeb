using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public HomeController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return RedirectToAction("EmailEntry");
    }

    public IActionResult EmailEntry()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendOtp(string email)
    {
        var baseUrl = _configuration["ExternalApi:BaseUrl"];
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync($"{baseUrl}/Auth/send-otp", new { Email = email });

        if (response.IsSuccessStatusCode)
        {
            ViewBag.Email = email;
            return RedirectToAction("OtpEntry", new { email });
        }

        return View("Error");
    }

    public IActionResult OtpEntry(string email)
    {
        ViewBag.Email = email;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp(string email, string otp)
    {
        var baseUrl = _configuration["ExternalApi:BaseUrl"];
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync($"{baseUrl}/Auth/verify-otp", new { Email = email, Otp = otp });

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("FileUpload");
        }

        return View("Error");
    }

    public async Task<IActionResult> FileUpload()
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _configuration["ExternalApi:BaseUrl"];
        var response = await client.GetAsync($"{baseUrl}list-files");

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<FileModel>>(jsonString);
            ViewBag.Files = files;
        }
        else
        {
            ViewBag.Files = new List<FileModel>();
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return RedirectToAction("FileUpload");
        }

        var client = _httpClientFactory.CreateClient();
        using var stream = file.OpenReadStream(); 
        var baseUrl = _configuration["ExternalApi:BaseUrl"];

        using var content = new StreamContent(stream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

        var response = await client.PostAsync($"{baseUrl}/File/upload", content);

        return RedirectToAction("FileUpload");
    }

    public async Task<IActionResult> DownloadFile(string name)
    {
        var client = _httpClientFactory.CreateClient();
        var baseUrl = _configuration["ExternalApi:BaseUrl"];

        var response = await client.GetAsync($"{baseUrl}/File/download/{name}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsByteArrayAsync();
            var jsonString = await response.Content.ReadAsStringAsync();
            var file = JsonSerializer.Deserialize<FileModel>(jsonString);

            return File(content, file.ContentType, file.FileName);
        }

        return View("Error");
    }
}

public class FileModel
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
}
