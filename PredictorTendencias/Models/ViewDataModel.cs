using System.ComponentModel.DataAnnotations;

namespace PredictorTendencias.Models
{
    public class ViewDataModel
    {
        [Required(ErrorMessage = "Debe ingresar exactamente 20 líneas en el formato YYYY-MM-DD, valor")]
        public string RawInput { get; set; } = string.Empty;

        public string Metodo { get; set; } = string.Empty;
        public string Tendencia { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public double? ValorPredicho { get; set; }
    }
}
