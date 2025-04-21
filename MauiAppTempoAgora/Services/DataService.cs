using MauiAppTempoAgora.Models;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Diagnostics;

namespace MauiAppTempoAgora.Services
{
    public class DataService
    {
        public static async Task<(Tempo? TempoData, HttpStatusCode? StatusCode)> GetPrevisao(string cidade)
        {
            Tempo? t = null;
            HttpStatusCode? statusCode = null;

            // ATENÇÃO: Chave da API hardcoded. Considere externalizar.
            string chave = "fa33d1905d1b7c040ef59f4ab163faa5";

            if (string.IsNullOrWhiteSpace(cidade))
            {
                Debug.WriteLine("Nome da cidade está vazio ou contém apenas espaços.");
                return (null, HttpStatusCode.BadRequest);
            }

            string url = $"https://api.openweathermap.org/data/2.5/weather?" +
                         $"q={Uri.EscapeDataString(cidade)}&units=metric&appid={chave}&lang=pt_br";

            Debug.WriteLine($"URL da API sendo chamada: {url}");

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);

                try
                {
                    HttpResponseMessage resp = await client.GetAsync(url);
                    statusCode = resp.StatusCode;
                    Debug.WriteLine($"Resposta da API recebida com status: {statusCode}");

                    if (resp.IsSuccessStatusCode)
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        Debug.WriteLine($"JSON recebido: {json}");
                        var rascunho = JObject.Parse(json);

                        // Parse mais seguro usando operadores ?. e ?[]
                        long? sunriseTimestamp = (long?)rascunho?["sys"]?["sunrise"];
                        long? sunsetTimestamp = (long?)rascunho?["sys"]?["sunset"];

                        DateTime? sunriseLocal = null;
                        if (sunriseTimestamp.HasValue)
                        {
                            sunriseLocal = DateTimeOffset.FromUnixTimeSeconds(sunriseTimestamp.Value).LocalDateTime;
                        }

                        DateTime? sunsetLocal = null;
                        if (sunsetTimestamp.HasValue)
                        {
                            sunsetLocal = DateTimeOffset.FromUnixTimeSeconds(sunsetTimestamp.Value).LocalDateTime;
                        }

                        t = new()
                        {
                            lat = (double?)rascunho?["coord"]?["lat"],
                            lon = (double?)rascunho?["coord"]?["lon"],
                            description = (string?)rascunho?["weather"]?[0]?["description"],
                            main = (string?)rascunho?["weather"]?[0]?["main"],
                            temp_min = (double?)rascunho?["main"]?["temp_min"],
                            temp_max = (double?)rascunho?["main"]?["temp_max"],
                            speed = (double?)rascunho?["wind"]?["speed"],
                            visibility = (int?)rascunho?["visibility"],
                            // Usa formato "g" para data e hora curtas
                            sunrise = sunriseLocal?.ToString("g"),
                            sunset = sunsetLocal?.ToString("g"),
                        };
                        Debug.WriteLine("Objeto Tempo populado com sucesso.");
                    }
                    else
                    {
                        Debug.WriteLine($"Falha na chamada da API. Status: {statusCode}. Conteúdo: {await resp.Content.ReadAsStringAsync()}");
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Debug.WriteLine($"Erro de HTTP no DataService: {httpEx.Message}");
                    statusCode = HttpStatusCode.ServiceUnavailable;
                }
                catch (Newtonsoft.Json.JsonException jsonEx)
                {
                    Debug.WriteLine($"Erro ao parsear JSON no DataService: {jsonEx.Message}");
                    statusCode = HttpStatusCode.InternalServerError;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro inesperado no DataService: {ex.GetType().Name} - {ex.Message}");
                    if (statusCode == null) statusCode = HttpStatusCode.InternalServerError;
                }
            }
            return (t, statusCode);
        }
    }
}