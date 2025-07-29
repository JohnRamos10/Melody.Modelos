using System.Text;
using Newtonsoft.Json;

namespace Melody.API.Consumer
{
    public static class Crud<T>
    {
        public static string Endpoint { get; set; }

        public static async Task<List<T>> GetAll()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(Endpoint);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<T>>(json);
            }
            else
            {
                throw new Exception($"Error al obtener datos: {response.ReasonPhrase}");
            }
        }

        public static async Task<T> GetById(int id)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"{Endpoint}/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            else
            {
                throw new Exception($"Error al obtener datos: {response.ReasonPhrase}");
            }
        }

        public static async Task<T> GetByCredentials(string correo, string contraseña)
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetAsync($"{Endpoint}/acceso?correo={Uri.EscapeDataString(correo)}&contraseña={Uri.EscapeDataString(contraseña)}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al autenticar: {ex.Message}");
            }
        }

        public static async Task<List<T>> GetBy(string campo, int id)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"{Endpoint}/{campo}/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<T>>(json);
            }
            else
            {
                throw new Exception($"Error: {response.StatusCode}");
            }
        }

        public static async Task<T> Create(T item)
        {
            using var client = new HttpClient();
            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(Endpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(jsonResponse);
            }
            else
            {
                throw new Exception($"Error al crear datos: {response.ReasonPhrase}");
            }
        }

        public static async Task<bool> Update(int id, T item)
        {
            using var client = new HttpClient();
            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{Endpoint}/{id}", content);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                throw new Exception($"Error al actualizar datos: {response.ReasonPhrase}");
            }
        }

        public static async Task<bool> Delete(int id)
        {
            using var client = new HttpClient();
            var response = await client.DeleteAsync($"{Endpoint}/{id}");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                throw new Exception($"Error al eliminar datos: {response.ReasonPhrase}");
            }
        }
    }
}

