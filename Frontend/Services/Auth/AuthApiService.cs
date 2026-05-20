using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frontend.Models.Auth;
using System.Net.Http.Json;

namespace Frontend.Services.Auth
{
    public class AuthApiService
    {
        private readonly HttpClient _http;

        public AuthApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task Register(RegisterRequest request)
        {
            await _http.PostAsJsonAsync(
                "api/auth/register",
                request);
        }
    }
}