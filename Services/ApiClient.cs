using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Auth;

namespace StockPortalApp.Services
{
    public static class ApiClient
    {
        private static readonly HttpClient _httpClient;
        private static string? _authToken;
        private static readonly JsonSerializerOptions _jsonOptions;

        static ApiClient()
        {
            var baseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "http://localhost:5000/api/v1/";
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
            };
        }

        public static string? AuthToken
        {
            get => _authToken;
            set
            {
                _authToken = value;
                if (!string.IsNullOrEmpty(value))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);
                }
                else
                {
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }
            }
        }

        public static bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);
        public static string CurrentUser { get; set; } = string.Empty;
        public static string CurrentRole { get; set; } = string.Empty;

        public static async Task<LoginResponseDto> LoginAsync(string idOrMobile, string password)
        {
            var payload = new LoginRequestDto { IdOrMobile = idOrMobile, Password = password };
            var response = await PostAsync<LoginResponseDto>("auth/login", payload);

            if (response.Success && response.Data != null)
            {
                AuthToken = response.Data.Token;
                CurrentUser = response.Data.FullName ?? idOrMobile;
                CurrentRole = response.Data.Role ?? "Member";
                return response.Data;
            }

            return new LoginResponseDto
            {
                Success = false,
                ErrorMessage = response.Message ?? "Invalid credentials."
            };
        }

        public static void Logout()
        {
            AuthToken = null;
            CurrentUser = string.Empty;
            CurrentRole = string.Empty;
        }

        private static void LogCall(string method, string endpoint, int? statusCode = null, string? error = null)
        {
            var msg = statusCode.HasValue
                ? $"[ApiClient] <-- {method} {endpoint} => {statusCode}"
                : error != null
                    ? $"[ApiClient] <-- {method} {endpoint} FAILED: {error}"
                    : $"[ApiClient] --> {method} {endpoint}";

            Debug.WriteLine(msg);
            Console.WriteLine(msg);
        }

        public static async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                LogCall("GET", endpoint);
                var response = await _httpClient.GetAsync(endpoint);
                var content = await response.Content.ReadAsStringAsync();
                LogCall("GET", endpoint, (int)response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                    return result ?? ApiResponse<T>.Fail("Empty response from server.");
                }

                var errResult = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                return errResult ?? ApiResponse<T>.Fail($"Server error ({response.StatusCode}).");
            }
            catch (Exception ex)
            {
                LogCall("GET", endpoint, error: ex.Message);
                return ApiResponse<T>.Fail($"Network error: {ex.Message}");
            }
        }

        public static async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                LogCall("POST", endpoint);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, httpContent);
                var content = await response.Content.ReadAsStringAsync();
                LogCall("POST", endpoint, (int)response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                    return result ?? ApiResponse<T>.Fail("Empty response from server.");
                }

                var errResult = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                return errResult ?? ApiResponse<T>.Fail($"Server error ({response.StatusCode}).");
            }
            catch (Exception ex)
            {
                LogCall("POST", endpoint, error: ex.Message);
                return ApiResponse<T>.Fail($"Network error: {ex.Message}");
            }
        }

        public static async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                LogCall("PUT", endpoint);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(endpoint, httpContent);
                var content = await response.Content.ReadAsStringAsync();
                LogCall("PUT", endpoint, (int)response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                    return result ?? ApiResponse<T>.Fail("Empty response from server.");
                }

                var errResult = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                return errResult ?? ApiResponse<T>.Fail($"Server error ({response.StatusCode}).");
            }
            catch (Exception ex)
            {
                LogCall("PUT", endpoint, error: ex.Message);
                return ApiResponse<T>.Fail($"Network error: {ex.Message}");
            }
        }

        public static async Task<ApiResponse<T>> PatchAsync<T>(string endpoint, object data)
        {
            try
            {
                LogCall("PATCH", endpoint);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint) { Content = httpContent };
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                LogCall("PATCH", endpoint, (int)response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                    return result ?? ApiResponse<T>.Fail("Empty response from server.");
                }

                var errResult = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                return errResult ?? ApiResponse<T>.Fail($"Server error ({response.StatusCode}).");
            }
            catch (Exception ex)
            {
                LogCall("PATCH", endpoint, error: ex.Message);
                return ApiResponse<T>.Fail($"Network error: {ex.Message}");
            }
        }

        public static async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
        {
            try
            {
                LogCall("DELETE", endpoint);
                var response = await _httpClient.DeleteAsync(endpoint);
                var content = await response.Content.ReadAsStringAsync();
                LogCall("DELETE", endpoint, (int)response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                    return result ?? ApiResponse<T>.Fail("Empty response from server.");
                }

                return ApiResponse<T>.Fail($"Server error ({response.StatusCode}).");
            }
            catch (Exception ex)
            {
                LogCall("DELETE", endpoint, error: ex.Message);
                return ApiResponse<T>.Fail($"Network error: {ex.Message}");
            }
        }

        public static async Task<bool> DownloadFileAsync(string endpoint, string destinationFilePath)
        {
            try
            {
                LogCall("GET (Download)", endpoint);
                var response = await _httpClient.GetAsync(endpoint);
                LogCall("GET (Download)", endpoint, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(destinationFilePath, fileBytes);
                return true;
            }
            catch (Exception ex)
            {
                LogCall("GET (Download)", endpoint, error: ex.Message);
                return false;
            }
        }
    }
}
