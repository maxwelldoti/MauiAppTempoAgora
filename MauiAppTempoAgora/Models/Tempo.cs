using Newtonsoft.Json; // Pode ser necessário se usar atributos JsonProperty no futuro

namespace MauiAppTempoAgora.Models
{
    /// <summary>
    /// Representa os dados da previsão do tempo obtidos da API.
    /// </summary>
    public class Tempo
    {
        /// <summary>
        /// Longitude geográfica da localização.
        /// </summary>
        public double? lon { get; set; }

        /// <summary>
        /// Latitude geográfica da localização.
        /// </summary>
        public double? lat { get; set; }

        /// <summary>
        /// Temperatura mínima atual ou prevista para o dia (em Celsius).
        /// </summary>
        public double? temp_min { get; set; }

        /// <summary>
        /// Temperatura máxima atual ou prevista para o dia (em Celsius).
        /// </summary>
        public double? temp_max { get; set; }

        /// <summary>
        /// Visibilidade média (em metros).
        /// </summary>
        public int? visibility { get; set; }

        /// <summary>
        /// Velocidade do vento (em m/s, conforme unidade 'metric' da API).
        /// </summary>
        public double? speed { get; set; }

        /// <summary>
        /// Grupo principal da condição do tempo (ex: "Clouds", "Rain", "Clear").
        /// </summary>
        public string? main { get; set; }

        /// <summary>
        /// Descrição textual da condição do tempo (ex: "céu limpo", "nuvens dispersas").
        /// </summary>
        public string? description { get; set; }

        /// <summary>
        /// Hora e data formatada do nascer do sol (fuso horário local).
        /// </summary>
        public string? sunrise { get; set; }

        /// <summary>
        /// Hora e data formatada do pôr do sol (fuso horário local).
        /// </summary>
        public string? sunset { get; set; }
    }
}