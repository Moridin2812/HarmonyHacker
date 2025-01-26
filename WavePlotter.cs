
using System;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.WindowsForms;
using System.Reflection;
using System.Linq;


namespace HarmonyHacker {
    public class WavePlotter {
        public void PlotWave(SoundWave wave, string filePath) {
            var plotModel = new PlotModel { Title = "Waveform" };

            var timeAxis = new LinearAxis {
                Position = AxisPosition.Bottom,
                Title = "Time (s)",
                Minimum = 0,
                Maximum = wave.Duration.TotalSeconds
            };

            var amplitudeAxis = new LinearAxis {
                Position = AxisPosition.Left,
                Title = "Amplitude",
                Minimum = -32768,
                Maximum = 32767
            };

            plotModel.Axes.Add(timeAxis);
            plotModel.Axes.Add(amplitudeAxis);

            var lineSeries = new LineSeries {
                Title = "Waveform",
                StrokeThickness = 1,
                Color = OxyColors.Blue
            };

            List<string> previousNotes = null;
            for (int i = 0; i < wave.Frames.Length; i++) {
                double time = wave.Frames[i].Time.TotalSeconds;
                double amplitude = wave.Frames[i].Data;
                lineSeries.Points.Add(new DataPoint(time, amplitude));

                /*
                var currentNotes = wave.Frames[i].Notes;
                if (currentNotes != null && currentNotes.Any() && (previousNotes == null || !currentNotes.SequenceEqual(previousNotes))) {
                    string notesText = string.Join(", ", currentNotes);
                    var textAnnotation = new TextAnnotation {
                        Text = notesText,
                        TextPosition = new DataPoint(time, 0), // Pozycja na linii 0
                        Stroke = OxyColors.Transparent,
                        TextColor = OxyColors.Red, // Zmieniono kolor na czerwony
                        FontSize = 16, // Zwiększono rozmiar czcionki
                        FontWeight = FontWeights.Bold
                    };
                    plotModel.Annotations.Add(textAnnotation);
                    previousNotes = new List<string>(currentNotes);
                }
                */
            }



            // Zaznaczanie wykrytych szczytów
            foreach (int index in wave.PeakIndices) {
                double time = wave.Frames[index].Time.TotalSeconds;

                var textAnnotation = new TextAnnotation {
                    Text = "X",
                    TextPosition = new DataPoint(time, 0), // Pozycja na osi 0
                    Stroke = OxyColors.Transparent,
                    TextColor = OxyColors.Green,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold
                };
                plotModel.Annotations.Add(textAnnotation);
            }

            plotModel.Series.Add(lineSeries);

            // Zapisz wykres do pliku PNG
            var pngExporter = new PngExporter { Width = 800, Height = 600 };
            pngExporter.ExportToFile(plotModel, filePath);
        }
    }
}
