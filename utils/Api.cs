using AlanTelegramBotApp.Models;
using DotNetEnv;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace AlanTelegramBotApp.utils
{
    public class Api
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly string ApiUrl = "https://localhost:7256/api/";

        public static async Task CreateUserAsync(User user)
        {
            var json = JsonConvert.SerializeObject(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(ApiUrl + "Users", content);

            response.EnsureSuccessStatusCode(); // Ensure success status code

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response from backend server: " + responseBody);
        }

        public static async Task UpdateUserAsync(User user)
        {
            var json = JsonConvert.SerializeObject(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync(ApiUrl + $"Users/{user.TelegramId}", content);

            response.EnsureSuccessStatusCode(); // Ensure success status code

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response from backend server: " + responseBody);
        }

        public static async Task<string> GetUserAsync(long TelegramId)
        {
            var response = await HttpClient.GetAsync(ApiUrl + $"Users/{TelegramId}");

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return null;
            }

            response.EnsureSuccessStatusCode(); // Ensure success status code

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response from backend server: " + responseBody);

            return responseBody;
        }

        public static async Task CreatePatientAsync(Patient patient)
        {
            var json = JsonConvert.SerializeObject(patient);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(ApiUrl + "Patients", content);

            response.EnsureSuccessStatusCode(); // Ensure success status code

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response from backend server: " + responseBody);
        }

        public static async Task<int> GetNextAvailablePatientIdAsync()
        {
            var response = await HttpClient.GetAsync(ApiUrl + "Patients/MaxPatientId");

            response.EnsureSuccessStatusCode(); // Ensure success status code

            string responseBody = await response.Content.ReadAsStringAsync();
            int maxPatientId = Convert.ToInt32(responseBody);

            return maxPatientId + 1;
        }
        
        public static async Task<string> GetPatientsAsync()
        {
            var response = await HttpClient.GetAsync(ApiUrl + "Patients/ByUsers");

            response.EnsureSuccessStatusCode(); // Ensure success status code

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response from backend server: " + responseBody);

            return responseBody;
        }

        public static async Task<string> GetAppointmentAsync(long telegramId)
        {
            var response = await HttpClient.GetAsync(ApiUrl + $"Appointments/telegram/{telegramId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode(); // Ensure success status code

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response from backend server: " + responseBody);
            
            var messages = JArray.Parse(responseBody);
            var appointmentDate = messages[0]["appointmentDate"].ToString();

            return appointmentDate;
        }

        public static async Task<string> GetTelegramMessagesAsync()
        {
            var response = await HttpClient.GetAsync(ApiUrl + "TelegramMessages");

            response.EnsureSuccessStatusCode(); // Ensure success status code

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response from backend server: " + responseBody);

            // Deserialize the JSON response
            var messages = JArray.Parse(responseBody);
            var content = messages[0]["content"].ToString();

            return content;
        }
    }
}