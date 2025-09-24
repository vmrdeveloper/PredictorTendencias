using Microsoft.AspNetCore.Mvc;
using PredictorTendencias.Models;
using System.Diagnostics;
using System.Globalization;

namespace PredictorTendencias.Controllers
{
    public class HomeController : Controller
    {
        private static PredictionMode _selectedMode = PredictionMode.SMA;

        private enum PredictionMode
        {
            SMA = 0,
            LinearRegression = 1,
            ROC = 2
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ViewDataModel());
        }

        [HttpPost]
        public IActionResult Calcular(ViewDataModel model)
        {
            var lines = model.RawInput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != 20)
            {
                ModelState.AddModelError("", "Debe ingresar exactamente 20 valores.");
                return View("Index", model);
            }

            var precios = new List<double>();
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length != 2 ||
                    !double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                {
                    ModelState.AddModelError("", $"Formato inválido en la línea: {line}");
                    return View("Index", model);
                }
                precios.Add(val);
            }

            // Selecciona el modo de predicción
            switch (_selectedMode)
            {
                case PredictionMode.SMA:
                    CalcularSMA(model, precios);
                    break;
                case PredictionMode.LinearRegression:
                    CalcularRegresion(model, precios);
                    break;
                case PredictionMode.ROC:
                    CalcularROC(model, precios);
                    break;
            }

            return View("Result", model);
        }

        private void CalcularSMA(ViewDataModel model, List<double> precios)
        {
            double smaCorta = precios.TakeLast(5).Average();
            double smaLarga = precios.Average();
            model.Metodo = "SMA Crossover";
            model.Tendencia = smaCorta > smaLarga ? "Alcista" : "Bajista";
            model.Detalle = $"SMA Corta={smaCorta:F2}, SMA Larga={smaLarga:F2}";
        }

        private void CalcularRegresion(ViewDataModel model, List<double> precios)
        {
            int n = precios.Count;
            double[] x = Enumerable.Range(1, n).Select(i => (double)i).ToArray();
            double[] y = precios.ToArray();

            double sumX = x.Sum();
            double sumY = y.Sum();
            double sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
            double sumX2 = x.Sum(xi => xi * xi);

            double denom = (n * sumX2 - Math.Pow(sumX, 2));
            double m = denom == 0 ? 0 : (n * sumXY - sumX * sumY) / denom;
            double b = (sumY - m * sumX) / n;

            double yPred = m * (n + 1) + b;

            model.Metodo = "Regresión Lineal";
            model.Tendencia = m > 0 ? "Alcista" : "Bajista";
            model.Detalle = $"Pendiente={m:F4}, Intercepto={b:F2}";
            model.ValorPredicho = yPred;
        }


        private void CalcularROC(ViewDataModel model, List<double> precios)
        {
            const int periodo = 5;
            var resultados = new List<string>();

            for (int i = periodo; i < precios.Count; i++)
            {
                double vActual = precios[i];
                double vAnterior = precios[i - periodo];
                double roc = (vActual / vAnterior - 1) * 100;
                resultados.Add($"t={i}, Precio={vActual:F2}, ROC({periodo})={roc:F2}%");
            }

            model.Metodo = "Momentum (ROC)";
            model.Tendencia = precios.Last() > precios.First() ? "Alcista" : "Bajista";
            model.Detalle = string.Join("\n", resultados);
        }

        // Pantalla de Modos
        [HttpGet]
        public IActionResult Modes()
        {
            ViewBag.SelectedMode = (int)_selectedMode;  // 👈 aquí el cambio
            return View();
        }


        [HttpPost]
        public IActionResult SetMode(int mode)
        {
            if (Enum.IsDefined(typeof(PredictionMode), mode))
                _selectedMode = (PredictionMode)mode;

            return RedirectToAction("Modes");
        }

    }
}
