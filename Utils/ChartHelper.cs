using SkiaSharp;

namespace GraficasMixing.Utils
{
    public class ChartDataset
    {
        public string Label { get; set; } = "";
        public List<double?> Values { get; set; } = new();
        public SKColor Color { get; set; } = SKColors.Red;
    }

    public static class ChartHelper
    {
        // 🔹 Gráfico de barras
        public static byte[] GenerateBarChart(List<string> labels, List<double> values, string title)
        {
            // 🔹 Si no hay datos, devolvemos una imagen con mensaje
            if (labels == null || labels.Count == 0 || values == null || values.Count == 0)
            {
                int width = 600;
                int height = 300;

                using var bitmap = new SKBitmap(width, height);
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.White);

                using var paintTitle = new SKPaint { Color = SKColors.Black, TextSize = 28, IsAntialias = true };
                canvas.DrawText(title, 20, 40, paintTitle);

                using var paintMsg = new SKPaint { Color = SKColors.Red, TextSize = 24, IsAntialias = true };
                canvas.DrawText("Sin datos disponibles", 20, height / 2, paintMsg);

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }

            // 🔹 Lógica normal si hay datos
            int barCount = labels.Count;
            int chartWidth = 1200;
            int chartHeight = 500;

            int availableWidth = chartWidth - 200;
            int barWidth = Math.Max(20, availableWidth / (barCount * 2));
            int spacing = barWidth;
            int startX = 100;

            using var bitmap2 = new SKBitmap(chartWidth, chartHeight);
            using var canvas2 = new SKCanvas(bitmap2);
            canvas2.Clear(SKColors.White);

            using var paintTitle2 = new SKPaint { Color = SKColors.Black, TextSize = 32, IsAntialias = true };
            canvas2.DrawText(title, 20, 40, paintTitle2);

            int maxHeight = chartHeight - 150;
            double maxValue = values.Count > 0 ? values.Max() : 1;

            using var paintBar = new SKPaint { Color = SKColors.Blue, IsAntialias = true };
            using var paintLabel = new SKPaint { Color = SKColors.Black, TextSize = 18, IsAntialias = true };
            using var paintValue = new SKPaint { Color = SKColors.Black, TextSize = 28, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) };

            for (int i = 0; i < values.Count; i++)
            {
                double ratio = values[i] / maxValue;
                int barHeight = (int)(ratio * maxHeight);

                int x = startX + i * (barWidth + spacing);
                int y = chartHeight - barHeight - 60;

                canvas2.DrawRect(x, y, barWidth, barHeight, paintBar);

                canvas2.DrawText($"{values[i]:0.0}", x, y - 5, paintValue);

                canvas2.Save();
                canvas2.Translate(x + barWidth / 2, chartHeight - 20);
                canvas2.RotateDegrees(-45);
                canvas2.DrawText(labels[i], 0, 0, paintLabel);
                canvas2.Restore();
            }

            using var image2 = SKImage.FromBitmap(bitmap2);
            using var data2 = image2.Encode(SKEncodedImageFormat.Png, 100);
            return data2.ToArray();
        }
        // 🔹 Gráfico de pastel
        public static byte[] GeneratePieChart(List<string> labels, List<double> values, string title)
        {
            int width = 600, height = 400;
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            using var paintTitle = new SKPaint { Color = SKColors.Black, TextSize = 24, IsAntialias = true };
            canvas.DrawText(title, 20, 40, paintTitle);

            double total = values.Sum();
            float centerX = width / 2, centerY = height / 2, radius = 120;
            float startAngle = 0;
            var colors = new[] { SKColors.Blue, SKColors.Red, SKColors.Green, SKColors.Orange };

            using var paintValue = new SKPaint { Color = SKColors.White, TextSize = 25, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) };

            for (int i = 0; i < values.Count; i++)
            {
                float sweepAngle = (float)(360 * (values[i] / total));
                using var paint = new SKPaint { Color = colors[i % colors.Length], IsAntialias = true };
                using var path = new SKPath();
                path.MoveTo(centerX, centerY);
                path.ArcTo(new SKRect(centerX - radius, centerY - radius, centerX + radius, centerY + radius),
                           startAngle, sweepAngle, false);
                path.Close();
                canvas.DrawPath(path, paint);

                // 🔹 Posición del texto en el centro del segmento
                float midAngle = startAngle + sweepAngle / 2;
                float textX = centerX + (float)(Math.Cos(midAngle * Math.PI / 180) * radius * 0.6);
                float textY = centerY + (float)(Math.Sin(midAngle * Math.PI / 180) * radius * 0.6);

                canvas.DrawText($"{values[i]:0.0}%", textX, textY, paintValue);

                startAngle += sweepAngle;
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        // 🔹 Gráfico de línea simple
        public static byte[] GenerateLineChart(List<string> labels, List<double> values, string title)
        {
            int count = labels.Count;
            int spacing = Math.Max(15, 2500 / Math.Max(1, count)); // más separación mínima
            int width = 100 + count * spacing;                     // ancho proporcional sin límite
            int height = 600;
            int baseFont = height / 25;

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            using var paintTitle = new SKPaint { Color = SKColors.Black, TextSize = baseFont * 2, IsAntialias = true };
            canvas.DrawText(title, 60, baseFont * 2, paintTitle);

            int startX = 100;
            int bottomY = height - 100;
            int maxHeight = height - 180;
            double maxValue = values.Count > 0 ? values.Max() : 1;

            using var gridPaint = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 1, IsAntialias = true };
            using var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true };
            using var paintLabel = new SKPaint { Color = SKColors.Black, TextSize = baseFont, IsAntialias = true };

            int gridLines = 10;
            for (int i = 0; i <= gridLines; i++)
            {
                int y = bottomY - (int)(i * (maxHeight / (double)gridLines));
                double val = (maxValue / gridLines) * i;
                canvas.DrawLine(startX, y, width - 80, y, gridPaint);
                canvas.DrawText($"{val:0}", startX - 60, y + 10, paintLabel);
            }

            canvas.DrawLine(startX, bottomY, width - 80, bottomY, axisPaint);
            canvas.DrawLine(startX, bottomY, startX, bottomY - maxHeight, axisPaint);

            using var paintLine = new SKPaint { Color = SKColors.Red, StrokeWidth = 3, IsAntialias = true, Style = SKPaintStyle.Stroke };
            using var paintPoint = new SKPaint { Color = SKColors.DarkBlue, IsAntialias = true };

            SKPath path = new SKPath();
            int interval = Math.Max(10, count / 30);

            for (int i = 0; i < count; i++)
            {
                double ratio = values[i] / maxValue;
                int x = startX + i * spacing;
                int y = bottomY - (int)(ratio * maxHeight);

                if (i == 0) path.MoveTo(x, y);
                else path.LineTo(x, y);

                canvas.DrawCircle(x, y, 5, paintPoint);

                if (i % interval == 0)
                {
                    string hora = DateTime.TryParse(labels[i], out var dt) ? dt.ToString("HH:mm") : labels[i];
                    using var paintVertical = new SKPaint { Color = SKColors.Black, TextSize = baseFont, IsAntialias = true };
                    canvas.Save();
                    canvas.Translate(x, bottomY + 60);
                    canvas.RotateDegrees(-90);
                    canvas.DrawText(hora, 0, 0, paintVertical);
                    canvas.Restore();
                }
            }

            canvas.DrawPath(path, paintLine);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        // 🔹 Gráfico de línea múltiple
        public static byte[] GenerateMultiLineChart(List<string> labels, List<ChartDataset> datasets, string title)
        {
            int count = labels.Count;
            int spacing = Math.Max(15, 2000 / Math.Max(1, count));
            int width = Math.Min(2000, 100 + count * spacing);
            int height = 600;
            int baseFont = height / 25;

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            using var paintTitle = new SKPaint { Color = SKColors.Black, TextSize = baseFont * 2, IsAntialias = true };
            canvas.DrawText(title, 60, baseFont * 2, paintTitle);

            int startX = 100;
            int bottomY = height - 100;
            int maxHeight = height - 180;

            double maxValue = datasets.SelectMany(d => d.Values.Where(v => v.HasValue).Select(v => v.Value)).DefaultIfEmpty(1).Max();

            using var gridPaint = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 1, IsAntialias = true };
            using var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true };
            using var paintLabel = new SKPaint { Color = SKColors.Black, TextSize = baseFont, IsAntialias = true };

            int gridLines = 10;
            for (int i = 0; i <= gridLines; i++)
            {
                int y = bottomY - (int)(i * (maxHeight / (double)gridLines));
                double val = (maxValue / gridLines) * i;
                canvas.DrawLine(startX, y, width - 80, y, gridPaint);
                canvas.DrawText($"{val:0}", startX - 60, y + 10, paintLabel);
            }

            canvas.DrawLine(startX, bottomY, width - 80, bottomY, axisPaint);
            canvas.DrawLine(startX, bottomY, startX, bottomY - maxHeight, axisPaint);

            int interval = Math.Max(10, count / 30);

            foreach (var dataset in datasets)
            {
                using var paintLine = new SKPaint { Color = dataset.Color, StrokeWidth = 3, IsAntialias = true, Style = SKPaintStyle.Stroke };
                using var paintPoint = new SKPaint { Color = dataset.Color, IsAntialias = true };

                SKPath path = new SKPath();

                for (int i = 0; i < count; i++)
                {
                    var val = dataset.Values[i];
                    if (!val.HasValue) continue;

                    double ratio = val.Value / maxValue;
                    int x = startX + i * spacing;
                    int y = bottomY - (int)(ratio * maxHeight);

                    if (path.IsEmpty) path.MoveTo(x, y);
                    else path.LineTo(x, y);

                    canvas.DrawCircle(x, y, 5, paintPoint);

                    if (i % interval == 0)
                    {
                        string hora = DateTime.TryParse(labels[i], out var dt) ? dt.ToString("HH:mm") : labels[i];
                        using var paintVertical = new SKPaint { Color = SKColors.Black, TextSize = baseFont, IsAntialias = true };
                        canvas.Save();
                        canvas.Translate(x, bottomY + 60);
                        canvas.RotateDegrees(-90);
                        canvas.DrawText(hora, 0, 0, paintVertical);
                        canvas.Restore();
                    }
                }

                canvas.DrawPath(path, paintLine);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }
}