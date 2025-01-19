using System;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.WindowsForms;

namespace HarmonyHacker
{
    public class WavePlotter
    {
        public void PlotWave(SoundWave wave, string filePath)
        {
            var plotModel = new PlotModel { Title = "Waveform" };

            var timeAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time (s)",
                Minimum = 0,
                Maximum = wave.Duration.TotalSeconds
            };

            var amplitudeAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Amplitude",
                Minimum = -32768,
                Maximum = 32767
            };

            plotModel.Axes.Add(timeAxis);
            plotModel.Axes.Add(amplitudeAxis);

            var lineSeries = new LineSeries
            {
                Title = "Waveform",
                StrokeThickness = 1,
                Color = OxyColors.Blue
            };

            string previousNote = null;
            for (int i = 0; i < wave.Frames.Length; i++)
            {
                double time = wave.Frames[i].Time.TotalSeconds;
                double amplitude = wave.Frames[i].Data;
                lineSeries.Points.Add(new DataPoint(time, amplitude));

                if (!string.IsNullOrEmpty(wave.Frames[i].Note) && wave.Frames[i].Note != previousNote)
                {
                    var textAnnotation = new TextAnnotation
                    {
                        Text = wave.Frames[i].Note,
                        TextPosition = new DataPoint(time, 0), // Pozycja na linii 0
                        Stroke = OxyColors.Transparent,
                        TextColor = OxyColors.Red, // Zmieniono kolor na czerwony
                        FontSize = 16, // Zwiększono rozmiar czcionki
                        FontWeight = FontWeights.Bold
                    };
                    plotModel.Annotations.Add(textAnnotation);
                    previousNote = wave.Frames[i].Note;
                }
            }

            plotModel.Series.Add(lineSeries);

            // Zapisz wykres do pliku PNG
            var pngExporter = new PngExporter { Width = 800, Height = 600 };
            pngExporter.ExportToFile(plotModel, filePath);
        }
    }
}
