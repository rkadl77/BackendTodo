using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Restoran.AdminPanel.Controllers;

public class AdminController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AdminController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login()
    {
        // Если уже есть токен, сразу идем в админку
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
            return RedirectToAction("Users");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        // Логирование для отладки
        Console.WriteLine($"=== LOGIN ATTEMPT ===");
        Console.WriteLine($"Email: '{email}'");
        Console.WriteLine($"Password: '{password}'");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Email or password is empty");
            ViewBag.Error = "Введите email и пароль";
            return View();
        }

        var authUrl = _configuration["Services:AuthUrl"];
        Console.WriteLine($"Auth URL: '{authUrl}'");

        var fullUrl = $"{authUrl}/api/auth/login";
        Console.WriteLine($"Full URL: {fullUrl}");

        var client = _httpClientFactory.CreateClient();

        var loginData = new { email, password };
        var jsonData = JsonSerializer.Serialize(loginData);
        Console.WriteLine($"Request JSON: {jsonData}");

        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(fullUrl, content);
            Console.WriteLine($"Response Status Code: {response.StatusCode}");

            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Body: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Неверный email или пароль";
                return View();
            }

            var doc = JsonDocument.Parse(responseBody);

            // Проверяем наличие полей
            if (!doc.RootElement.TryGetProperty("accessToken", out var tokenProp))
            {
                ViewBag.Error = "Ошибка: токен не получен";
                return View();
            }

            var token = tokenProp.GetString();
            var roles = doc.RootElement.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();

            Console.WriteLine($"Token received: {(string.IsNullOrEmpty(token) ? "NO" : "YES")}");
            Console.WriteLine($"Roles: {string.Join(", ", roles)}");

            if (!roles.Contains("Admin"))
            {
                ViewBag.Error = "У вас нет прав администратора. Требуется роль Admin.";
                return View();
            }

            HttpContext.Session.SetString("Token", token);
            Console.WriteLine("Login successful! Redirecting to Users...");

            return RedirectToAction("Users");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            ViewBag.Error = $"Ошибка подключения: {ex.Message}";
            return View();
        }
    }

    public async Task<IActionResult> Users()
    {
        var token = HttpContext.Session.GetString("Token");
        Console.WriteLine($"Users page - Token exists: {!string.IsNullOrEmpty(token)}");

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("No token, redirecting to Login");
            return RedirectToAction("Login");
        }

        var authUrl = _configuration["Services:AuthUrl"];
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync($"{authUrl}/api/admin/users?pageSize=100");
            Console.WriteLine($"Get users response: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to get users, redirecting to Login");
                return RedirectToAction("Login");
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var users = doc.RootElement.GetProperty("items").EnumerateArray().ToList();

            return View(users);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting users: {ex.Message}");
            return RedirectToAction("Login");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Ban(Guid id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        var authUrl = _configuration["Services:AuthUrl"];
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await client.PostAsync($"{authUrl}/api/admin/users/{id}/ban", null);
        return RedirectToAction("Users");
    }

    [HttpPost]
    public async Task<IActionResult> Unban(Guid id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        var authUrl = _configuration["Services:AuthUrl"];
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await client.PostAsync($"{authUrl}/api/admin/users/{id}/unban", null);
        return RedirectToAction("Users");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}