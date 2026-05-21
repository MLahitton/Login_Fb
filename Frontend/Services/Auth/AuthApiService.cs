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
            /*var response = await _http.PostAsJsonAsync(
                "api/auth/register",
                request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }*/
            await Task.Delay(1000);
            Console.WriteLine("Registro simulado");
        }
        public async Task Login(LoginRequest request)
        {
            var response = await _http.PostAsJsonAsync(
                "api/auth/login",
                request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }
        }
        public async Task VerifyCode(VerifyCodeRequest request)
        {
            // CUANDO YA EXISTA EL BACKEND
            /*
            var response = await _http.PostAsJsonAsync(
                "api/auth/verify-code",
                request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }
            */

            // SIMULACIÓN TEMPORAL
            await Task.Delay(1000);

            Console.WriteLine($"Código confirmado: {request.Code}");
        }
    }
}