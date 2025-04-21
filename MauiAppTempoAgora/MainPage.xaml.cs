// Conteúdo do MainPage.xaml.cs (sem alterações desde a última versão)
using MauiAppTempoAgora.Models;
using MauiAppTempoAgora.Services;
using System;
using System.Diagnostics;
using System.Net;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;

namespace MauiAppTempoAgora
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            lbl_res.Text = "Digite uma cidade ou use sua localização.";
        }

        // --- Handlers Botões ---
        private async void Button_Clicked_Previsao(object sender, EventArgs e)
        {
            if (loadingIndicator.IsRunning) return;
            HideKeyboard();
            if (!await CheckConnectivity("buscar a previsão")) return;

            string cidade = txt_cidade.Text;
            if (string.IsNullOrWhiteSpace(cidade))
            {
                await DisplayAlert("Entrada Inválida", "Por favor, preencha o nome da cidade.", "OK");
                return;
            }

            SetLoading(true, "Buscando previsão...");
            Tempo? tempoResult = null;
            HttpStatusCode? statusCode = null;
            try
            {
                (tempoResult, statusCode) = await DataService.GetPrevisao(cidade);
                ProcessWeatherResult(tempoResult, statusCode, cidade);
            }
            catch (Exception ex) { await HandleGeneralError("Erro ao buscar previsão", ex); }
            finally { SetLoading(false); }
        }

        private async void Button_Clicked_Localizacao(object sender, EventArgs e)
        {
            if (loadingIndicator.IsRunning) return;
            if (!await CheckConnectivity("obter a localização")) return;

            SetLoading(true, "Obtendo localização...");
            Location? local = null;
            try
            {
                local = await GetDeviceLocation();
                if (local != null)
                {
                    string local_disp = $"Lat: {local.Latitude.ToString(CultureInfo.InvariantCulture)}, Lon: {local.Longitude.ToString(CultureInfo.InvariantCulture)}";
                    lbl_coords.Text = local_disp;
                    SetLoading(true, "Buscando nome da cidade...");
                    await GetCidade(local.Latitude, local.Longitude);
                }
                else
                {
                    lbl_coords.Text = "Não foi possível obter a localização.";
                    txt_cidade.Text = string.Empty;
                }
            }
            catch (Exception ex) { await HandleGeneralError("Erro ao obter localização", ex); } // Erros específicos são tratados em GetDeviceLocation/GetCidade
            finally { SetLoading(false); }
        }

        private void ClearCity_Clicked(object sender, EventArgs e)
        {
            txt_cidade.Text = string.Empty;
            lbl_res.Text = "Digite uma cidade ou use sua localização.";
            lbl_coords.Text = string.Empty;
            wv_mapa.Source = null;
            HideKeyboard();
        }

        // --- Métodos Auxiliares ---
        private async Task<bool> CheckConnectivity(string action)
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await DisplayAlert("Sem Conexão", $"Verifique sua conexão com a internet para {action}.", "OK");
                return false;
            }
            return true;
        }

        private void SetLoading(bool isLoading, string? statusText = null)
        {
            loadingIndicator.IsRunning = isLoading;
            loadingIndicator.IsVisible = isLoading;
            btnBuscarPrevisao.IsEnabled = !isLoading;
            // btnLocalizacao.IsEnabled = !isLoading; // Se tiver nomeado o botão localização
            btnClearCity.IsEnabled = !isLoading;
            // Aqui você poderia atualizar um Label de status com statusText
        }

        private void ProcessWeatherResult(Tempo? tempoResult, HttpStatusCode? statusCode, string cidade)
        {
            if (tempoResult != null && statusCode == HttpStatusCode.OK)
            {
                string dados_previsao =
                    $"📍 Cidade: {cidade}\n" +
                    $"🌦️ Clima: {tempoResult.description ?? "N/A"} ({tempoResult.main ?? "N/A"})\n" +
                    $"🌡️ Temp Máx/Mín: {tempoResult.temp_max?.ToString("F1", CultureInfo.InvariantCulture) ?? "N/A"}°C / {tempoResult.temp_min?.ToString("F1", CultureInfo.InvariantCulture) ?? "N/A"}°C\n" +
                    $"🌬️ Vento: {tempoResult.speed?.ToString("F1", CultureInfo.InvariantCulture) ?? "N/A"} m/s\n" +
                    $"👁️ Visibilidade: {tempoResult.visibility?.ToString() ?? "N/A"} metros\n" +
                    $"☀️ Nascer do Sol: {tempoResult.sunrise ?? "N/A"}\n" +
                    $"🌙 Pôr do Sol: {tempoResult.sunset ?? "N/A"}\n" +
                    $"🌍 Coords: Lat {tempoResult.lat?.ToString(CultureInfo.InvariantCulture) ?? "N/A"}, Lon {tempoResult.lon?.ToString(CultureInfo.InvariantCulture) ?? "N/A"}";

                lbl_res.Text = dados_previsao;

                if (tempoResult.lat.HasValue && tempoResult.lon.HasValue)
                {
                    string latLon = $"lat={tempoResult.lat.Value.ToString(CultureInfo.InvariantCulture)}&lon={tempoResult.lon.Value.ToString(CultureInfo.InvariantCulture)}";
                    string mapa = $"https://embed.windy.com/embed.html?type=map&location=coordinates&metricRain=mm&metricTemp=°C&metricWind=km/h&zoom=9&overlay=wind&product=ecmwf&level=surface&{latLon}";
                    Debug.WriteLine($"URL do Mapa: {mapa}");
                    wv_mapa.Source = mapa;
                }
                else { wv_mapa.Source = null; }
            }
            else { ProcessWeatherError(statusCode, cidade); }
        }

        private void ProcessWeatherError(HttpStatusCode? statusCode, string cidade)
        {
            string errorMsg;
            switch (statusCode)
            {
                case HttpStatusCode.NotFound: errorMsg = $"Cidade não encontrada: {cidade}."; break;
                case HttpStatusCode.Unauthorized: errorMsg = "Erro de autenticação (API Key)."; break;
                case HttpStatusCode.BadRequest: errorMsg = "Requisição inválida (verifique a cidade)."; break;
                case HttpStatusCode.ServiceUnavailable: errorMsg = "Serviço indisponível ou sem rede."; break;
                default: errorMsg = $"Erro ao obter previsão ({statusCode?.ToString() ?? "Desconhecido"})."; break;
            }
            lbl_res.Text = errorMsg;
            wv_mapa.Source = null;
            Debug.WriteLine($"Erro ao processar previsão: {errorMsg}");
            // Considerar DisplayAlert para erros mais críticos se necessário
        }


        private async Task<Location?> GetDeviceLocation()
        {
            Location? location = null;
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted) { await DisplayAlert("Permissão Necessária", "Permissão de localização é necessária.", "OK"); return null; }
                }

                location = await Geolocation.Default.GetLastKnownLocationAsync();
                if (location == null || location.Timestamp < DateTimeOffset.UtcNow.AddMinutes(-5))
                {
                    GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    location = await Geolocation.Default.GetLocationAsync(request);
                }
            }
            catch (FeatureNotSupportedException) { await DisplayAlert("Erro", "Localização não suportada neste dispositivo.", "OK"); }
            catch (FeatureNotEnabledException) { await DisplayAlert("Erro", "GPS desabilitado. Por favor, habilite.", "OK"); }
            catch (PermissionException) { await DisplayAlert("Erro", "Permissão de localização negada.", "OK"); }
            catch (Exception ex) { await DisplayAlert("Erro Localização", $"Erro inesperado: {ex.Message}", "OK"); }
            return location;
        }

        private async Task GetCidade(double lat, double lon)
        {
            try
            {
                IEnumerable<Placemark> places = await Geocoding.Default.GetPlacemarksAsync(lat, lon);
                Placemark? place = places?.FirstOrDefault();
                if (place != null)
                {
                    txt_cidade.Text = place.Locality ?? place.SubAdminArea ?? place.AdminArea ?? place.CountryName ?? string.Empty;
                    if (string.IsNullOrEmpty(txt_cidade.Text)) { await DisplayAlert("Aviso", "Não foi possível determinar o nome da localidade.", "OK"); }
                }
                else { await DisplayAlert("Aviso", "Não foi possível encontrar info de localidade.", "OK"); txt_cidade.Text = string.Empty; }
            }
            catch (Exception ex) { await DisplayAlert("Erro Geocoding", $"Não foi possível obter nome da cidade: {ex.Message}", "OK"); txt_cidade.Text = string.Empty; }
        }

        private void HideKeyboard()
        {
            if (txt_cidade.IsFocused) { txt_cidade.Unfocus(); }
            // O método Unfocus() é a forma mais correta em MAUI.
        }

        // --- Handlers WebView ---
        private void WebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url != null && e.Url.StartsWith("https://embed.windy.com")) { SetLoading(true, "Carregando mapa..."); }
        }

        private async void WebView_Navigated(object sender, WebNavigatedEventArgs e)
        {
            SetLoading(false);
            if (e.Result == WebNavigationResult.Failure)
            { await DisplayAlert("Erro no Mapa", $"Falha ao carregar mapa: {e.Result}", "OK"); lbl_res.Text += "\n(Falha ao carregar mapa)"; }
            else if (e.Result == WebNavigationResult.Timeout)
            { await DisplayAlert("Erro no Mapa", "Timeout ao carregar mapa.", "OK"); lbl_res.Text += "\n(Timeout ao carregar mapa)"; }
        }

        // --- Tratamento Genérico de Erro ---
        private async Task HandleGeneralError(string context, Exception ex)
        {
            Debug.WriteLine($"Erro inesperado em {context}: {ex}");
            await DisplayAlert("Erro Inesperado", $"Ocorreu um erro inesperado. Por favor, tente novamente.", "OK");
            // Poderia limpar a UI ou tomar outra ação aqui
            lbl_res.Text = "Ocorreu um erro.";
            wv_mapa.Source = null;
        }
    }
}