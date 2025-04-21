// MauiAppTempoAgora/MainPage.xaml.cs
using MauiAppTempoAgora.Models;
using MauiAppTempoAgora.Services;
using System;
using System.Diagnostics;
using System.Net; // Necessário para HttpStatusCode
using Microsoft.Maui.Networking; // Necessário para Connectivity
using Microsoft.Maui.Devices.Sensors; // Necessário para Geolocation, Geocoding, Placemark, etc.
using Microsoft.Maui.ApplicationModel; // Necessário para PermissionException, etc.


namespace MauiAppTempoAgora
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void Button_Clicked_Previsao(object sender, EventArgs e)
        {
            // 1. Verificar Conectividade
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await DisplayAlert("Sem Conexão", "Por favor, verifique sua conexão com a internet e tente novamente.", "OK");
                return;
            }

            // 2. Verificar se a cidade foi preenchida
            if (string.IsNullOrWhiteSpace(txt_cidade.Text))
            {
                lbl_res.Text = "Por favor, preencha o nome da cidade.";
                wv_mapa.Source = null; // Limpa o mapa
                return;
            }

            try
            {
                // 3. Chamar o DataService (que agora retorna uma tupla)
                var (tempoResult, statusCode) = await DataService.GetPrevisao(txt_cidade.Text);

                // 4. Processar o resultado
                if (tempoResult != null && statusCode == HttpStatusCode.OK)
                {
                    // Sucesso! Montar a string de exibição com os dados expandidos
                    string dados_previsao =
                        $"Cidade: {txt_cidade.Text}\n" + // Adiciona nome da cidade para clareza
                        $"Clima: {tempoResult.description ?? "N/A"} ({tempoResult.main ?? "N/A"})\n" + // Descrição e Main
                        $"Temperatura Máx: {tempoResult.temp_max?.ToString("F1") ?? "N/A"} °C\n" + // Formatado com 1 casa decimal
                        $"Temperatura Mín: {tempoResult.temp_min?.ToString("F1") ?? "N/A"} °C\n" + // Formatado com 1 casa decimal
                        $"Velocidade do Vento: {tempoResult.speed?.ToString("F1") ?? "N/A"} m/s\n" + // Velocidade do vento
                        $"Visibilidade: {tempoResult.visibility?.ToString() ?? "N/A"} metros\n" + // Visibilidade
                        $"Nascer do Sol: {tempoResult.sunrise ?? "N/A"}\n" + // Já formatado como string HH:mm:ss
                        $"Pôr do Sol: {tempoResult.sunset ?? "N/A"}\n" +   // Já formatado como string HH:mm:ss
                        $"Latitude: {tempoResult.lat?.ToString() ?? "N/A"}\n" +
                        $"Longitude: {tempoResult.lon?.ToString() ?? "N/A"}";

                    lbl_res.Text = dados_previsao;

                    // Atualizar o WebView (mantendo a lógica original com Replace)
                    if (tempoResult.lat.HasValue && tempoResult.lon.HasValue)
                    {
                        string mapa = $"https://embed.windy.com/embed.html?" +
                                      $"type=map&location=coordinates&metricRain=mm&metricTemp=°C" +
                                      $"&metricWind=km/h&zoom=5&overlay=wind&product=ecmwf&level=surface" +
                                      $"&lat={tempoResult.lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={tempoResult.lon.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                        // Usando InvariantCulture para garantir ponto decimal para a URL

                        wv_mapa.Source = mapa;
                        Debug.WriteLine(mapa);
                    }
                    else
                    {
                        wv_mapa.Source = null; // Limpa se não tiver coords
                    }

                }
                else
                {
                    // Falha - Verificar o StatusCode para erro específico
                    if (statusCode == HttpStatusCode.NotFound) // Erro 404
                    {
                        lbl_res.Text = $"Não foi possível encontrar dados para a cidade: {txt_cidade.Text}. Verifique o nome e tente novamente.";
                    }
                    else if (statusCode == HttpStatusCode.Unauthorized) // Erro 401
                    {
                        lbl_res.Text = "Erro de autenticação com o serviço de previsão do tempo. Verifique a chave da API.";
                    }
                    else if (statusCode == HttpStatusCode.BadRequest && string.IsNullOrWhiteSpace(txt_cidade.Text)) // Tratamento do erro de cidade vazia do DataService
                    {
                        lbl_res.Text = "Por favor, preencha o nome da cidade.";
                    }
                    else
                    {
                        // Outro erro (API fora do ar, problema de rede não capturado antes, etc.)
                        lbl_res.Text = $"Não foi possível obter a previsão. Código de erro: {statusCode?.ToString() ?? "Desconhecido"}";
                    }
                    wv_mapa.Source = null; // Limpa o mapa em caso de erro
                }
            }
            catch (Exception ex)
            {
                // Erro inesperado durante o processo na MainPage
                await DisplayAlert("Erro Inesperado", $"Ocorreu um erro: {ex.Message}", "OK");
                lbl_res.Text = "Ocorreu um erro ao processar a solicitação.";
                wv_mapa.Source = null; // Limpa o mapa
            }
        }

        private async void Button_Clicked_Localizacao(object sender, EventArgs e)
        {
            // 1. Verificar Conectividade (Geocoding precisa de rede)
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await DisplayAlert("Sem Conexão", "É necessária conexão com a internet para obter o nome da cidade a partir da localização.", "OK");
                return;
            }

            try
            {
                GeolocationRequest request = new GeolocationRequest(
                    GeolocationAccuracy.Medium,
                    TimeSpan.FromSeconds(10)
                );

                Location? local = await Geolocation.Default.GetLocationAsync(request);

                if (local != null)
                {
                    string local_disp = $"Latitude: {local.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)} \n" +
                                        $"Longitude: {local.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                    lbl_coords.Text = local_disp;

                    // Pega nome da cidade que está nas coordenadas.
                    await GetCidade(local.Latitude, local.Longitude); // Tornando a chamada awaitable
                }
                else
                {
                    lbl_coords.Text = "Não foi possível obter a localização.";
                    txt_cidade.Text = string.Empty; // Limpa cidade se localização falhar
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                await DisplayAlert("Erro: Dispositivo não Suporta", fnsEx.Message, "OK");
            }
            catch (FeatureNotEnabledException fneEx)
            {
                await DisplayAlert("Erro: Localização Desabilitada", "Por favor, habilite o serviço de localização no seu dispositivo.", "OK"); // Mensagem mais amigável
            }
            catch (PermissionException pEx)
            {
                await DisplayAlert("Erro: Permissão da Localização", "A permissão para acessar a localização foi negada. Verifique as configurações do aplicativo.", "OK"); // Mensagem mais amigável
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro ao Obter Localização", ex.Message, "OK");
            }
        }

        // Marcar como async Task para poder usar await dentro e na chamada
        private async Task GetCidade(double lat, double lon)
        {
            // A verificação de conectividade já foi feita no método chamador (Button_Clicked_Localizacao)
            try
            {
                IEnumerable<Placemark> places = await Geocoding.Default.GetPlacemarksAsync(lat, lon);
                Placemark? place = places?.FirstOrDefault(); // Adicionado '?' para segurança

                if (place != null)
                {
                    // Tenta usar Locality, se não, AdminArea, se não, CountryName
                    txt_cidade.Text = place.Locality ?? place.AdminArea ?? place.CountryName ?? string.Empty;
                    if (string.IsNullOrEmpty(txt_cidade.Text))
                    {
                        await DisplayAlert("Aviso", "Não foi possível determinar o nome da cidade/localidade para estas coordenadas.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Aviso", "Não foi possível encontrar informações de localidade para estas coordenadas.", "OK");
                    txt_cidade.Text = string.Empty; // Limpa se não encontrar
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro: Obtenção do nome da Cidade", $"Não foi possível obter o nome da cidade: {ex.Message}", "OK");
                txt_cidade.Text = string.Empty; // Limpa em caso de erro
            }
        }
    }
}