// MauiAppTempoAgora/Services/DataService.cs
using MauiAppTempoAgora.Models;
using Newtonsoft.Json.Linq; // Ou System.Text.Json se preferir refatorar
using System.Net; // Necessário para HttpStatusCode
using System.Net.Http;
using System.Threading.Tasks;
using System; // Necessário para DateTimeOffset, DateTime, Uri
using System.Diagnostics; // Para Debug.WriteLine

namespace MauiAppTempoAgora.Services
{
    public class DataService
    {
        // Retorna uma Tupla: os dados do Tempo (se sucesso) e o StatusCode da resposta HTTP
        public static async Task<(Tempo? TempoData, HttpStatusCode? StatusCode)> GetPrevisao(string cidade)
        {
            Tempo? t = null;
            HttpStatusCode? statusCode = null;

            // ATENÇÃO: Mantenha sua chave segura, não hardcoded assim em produção!
            // Considere usar segredos de configuração ou Azure Key Vault.
            string chave = "fa33d1905d1b7c040ef59f4ab163faa5";

            // Verifica se a cidade não está vazia ou nula
            if (string.IsNullOrWhiteSpace(cidade))
            {
                Debug.WriteLine("Nome da cidade está vazio ou contém apenas espaços.");
                // Retorna BadRequest para cidade vazia, MainPage tratará a mensagem
                return (null, HttpStatusCode.BadRequest);
            }

            // Codifica o nome da cidade para ser seguro na URL e adiciona idioma pt-br
            string url = $"https://api.openweathermap.org/data/2.5/weather?" +
                         $"q={Uri.EscapeDataString(cidade)}&units=metric&appid={chave}&lang=pt_br";

            Debug.WriteLine($"URL da API sendo chamada: {url}"); // Log para depuração

            using (HttpClient client = new HttpClient())
            {
                // Definir um timeout razoável para a requisição
                client.Timeout = TimeSpan.FromSeconds(15);

                try
                {
                    HttpResponseMessage resp = await client.GetAsync(url);
                    statusCode = resp.StatusCode; // Guarda o status code

                    Debug.WriteLine($"Resposta da API recebida com status: {statusCode}"); // Log

                    if (resp.IsSuccessStatusCode) // Sucesso (ex: 200 OK)
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        Debug.WriteLine($"JSON recebido: {json}"); // Log (cuidado com dados sensíveis em produção)

                        var rascunho = JObject.Parse(json); // Usando Newtonsoft.Json.Linq como no original

                        // *** CORREÇÃO DO TIMESTAMP APLICADA ***
                        long sunriseTimestamp = (long)rascunho["sys"]["sunrise"];
                        DateTimeOffset sunriseDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(sunriseTimestamp);
                        // Correto: Obter o DateTime local a partir do DateTimeOffset
                        DateTime sunriseLocal = sunriseDateTimeOffset.LocalDateTime;

                        long sunsetTimestamp = (long)rascunho["sys"]["sunset"];
                        DateTimeOffset sunsetDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(sunsetTimestamp);
                        // Correto: Obter o DateTime local a partir do DateTimeOffset
                        DateTime sunsetLocal = sunsetDateTimeOffset.LocalDateTime;
                        // *** FIM DA CORREÇÃO ***

                        t = new()
                        {
                            // Usando safe navigation (?) e conversão explícita para maior segurança
                            lat = (double?)rascunho?["coord"]?["lat"],
                            lon = (double?)rascunho?["coord"]?["lon"],
                            description = (string?)rascunho?["weather"]?[0]?["description"],
                            main = (string?)rascunho?["weather"]?[0]?["main"],
                            temp_min = (double?)rascunho?["main"]?["temp_min"],
                            temp_max = (double?)rascunho?["main"]?["temp_max"],
                            speed = (double?)rascunho?["wind"]?["speed"],
                            visibility = (int?)rascunho?["visibility"],
                            // Atribuindo a partir das variáveis DateTime corrigidas e formatando
                            sunrise = sunriseLocal.ToString("HH:mm:ss"), // Formato explícito de hora
                            sunset = sunsetLocal.ToString("HH:mm:ss"),   // Formato explícito de hora
                        };

                        Debug.WriteLine("Objeto Tempo populado com sucesso."); // Log
                    }
                    else
                    {
                        Debug.WriteLine($"Falha na chamada da API. Status: {statusCode}. Conteúdo: {await resp.Content.ReadAsStringAsync()}"); // Log do erro
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    // Erro de rede, DNS, timeout, etc.
                    Debug.WriteLine($"Erro de HTTP no DataService: {httpEx.Message}");
                    statusCode = HttpStatusCode.ServiceUnavailable; // Exemplo: indicar que o serviço não pôde ser contatado
                }
                catch (Newtonsoft.Json.JsonException jsonEx)
                {
                    // Erro ao fazer o parse do JSON recebido
                    Debug.WriteLine($"Erro ao parsear JSON no DataService: {jsonEx.Message}");
                    statusCode = HttpStatusCode.InternalServerError; // Indica um problema ao processar a resposta
                }
                catch (Exception ex)
                {
                    // Outro erro inesperado
                    Debug.WriteLine($"Erro inesperado no DataService: {ex.GetType().Name} - {ex.Message}");
                    // Mantém statusCode como null ou define um genérico como InternalServerError
                    if (statusCode == null) statusCode = HttpStatusCode.InternalServerError;
                }
            } // HttpClient é डिस्पोज्ड (liberado) aqui

            // Retorna a tupla com o objeto Tempo (pode ser null) e o StatusCode (pode ser null se exceção ocorreu antes da chamada)
            return (t, statusCode);
        }
    }
}