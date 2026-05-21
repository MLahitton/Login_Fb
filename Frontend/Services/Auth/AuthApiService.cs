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
        public async Task ForgotPassword(ForgotPasswordRequest request)
        {
            await Task.Delay(1000);

            Console.WriteLine("Correo de recuperación enviado");
        }

        public async Task ResetPassword(ResetPasswordRequest request)
        {
            await Task.Delay(1000);

            Console.WriteLine("Contraseña actualizada");
        }
         public async Task<DashboardStats> GetStats()
        {
            await Task.Delay(500);

            return new DashboardStats
            {
                ActiveProjects = 24,
                PendingTasks = 18,
                ActiveClients = 36,
                Revenue = 24680
            };
        }

        public async Task<List<ActivityItem>> GetActivities()
        {
            await Task.Delay(500);

            return new List<ActivityItem>
            {
                new ActivityItem
                {
                    Title = "Nuevo proyecto creado",
                    Description = "Rediseño Web"
                },

                new ActivityItem
                {
                    Title = "Tarea completada",
                    Description = "Investigación de mercado"
                },

                new ActivityItem
                {
                    Title = "Cliente actualizado",
                    Description = "Central Café"
                },

                new ActivityItem
                {
                    Title = "Reporte generado",
                    Description = "Reporte mensual"
                }
            };
        }

        public async Task<List<QuickAction>> GetQuickActions()
        {
            await Task.Delay(500);

            return new List<QuickAction>
            {
                new QuickAction
                {
                    Name = "Nuevo proyecto"
                },

                new QuickAction
                {
                    Name = "Nueva tarea"
                },

                new QuickAction
                {
                    Name = "Agregar cliente"
                },

                new QuickAction
                {
                    Name = "Generar reporte"
                }
            };
       
        }    
    }
}