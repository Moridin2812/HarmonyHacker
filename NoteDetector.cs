using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace HarmonyHacker {
    public class NoteDetector {
        // Generujemy słownik częstotliwości nut
        private Dictionary<double, string> noteFrequencies = MusicTheory.GenerateNoteFrequencies();

        /// <summary>
        /// Analizuje częstotliwości z użyciem STFT, wykrywając wiele nut jednocześnie.
        /// </summary>
        public List<string> AnalyzeFrequenciesWithSTFT(float[] samples, int sampleRate, long peakIndex) {
            int windowSize = 2048; // Rozmiar okna

            // Bierzemy próbki od indeksu szczytu
            float[] windowSamples = samples.Skip((int)peakIndex).Take(windowSize).ToArray();

            // Sprawdzamy, czy mamy wystarczająco dużo próbek
            if (windowSamples.Length < windowSize)
                return new List<string>();

            // Usunięcie średniej (DC offset)
            float mean = windowSamples.Average();
            for (int i = 0; i < windowSamples.Length; i++) {
                windowSamples[i] -= mean;
            }

            // Zastosuj okno Hanninga
            var window = Window.Hann(windowSize);
            for (int j = 0; j < windowSize; j++) {
                windowSamples[j] *= (float)window[j];
            }

            // FFT (Fast Fourier Transform)
            var complexSamples = windowSamples.Select(s => new Complex(s, 0)).ToArray();
            Fourier.Forward(complexSamples, FourierOptions.NoScaling);

            var magnitudes = complexSamples.Take(complexSamples.Length / 2).Select(c => c.Magnitude).ToArray();

            // *** Wykrywanie wielu nut jednocześnie ***

            // Ustalenie progu amplitudy
            double maxMagnitude = magnitudes.Max();
            double threshold = maxMagnitude * 0.1; // Próg na 10% maksymalnej amplitudy

            // Ograniczenie zakresu częstotliwości do interesującego nas pasma
            double minFrequency = 50.0;
            double maxFrequency = 4000.0;

            List<double> detectedFrequencies = new List<double>();
            for (int i = 0; i < magnitudes.Length; i++) {
                double frequency = i * sampleRate / (double)windowSize;
                if (magnitudes[i] >= threshold && frequency >= minFrequency && frequency <= maxFrequency) {
                    detectedFrequencies.Add(frequency);
                }
            }

            // Grupowanie częstotliwości blisko siebie (aby uniknąć wielokrotnego liczenia harmonicznych)
            detectedFrequencies = ClusterFrequencies(detectedFrequencies, 5.0); // Klasteryzacja w zakresie 5 Hz

            // Mapowanie częstotliwości do nut
            List<string> detectedNotes = new List<string>();
            foreach (var freq in detectedFrequencies) {
                var closestNote = noteFrequencies.OrderBy(n => Math.Abs(n.Key - freq)).First();
                detectedNotes.Add(closestNote.Value);
            }

            // Usuwanie duplikatów i sortowanie nut
            detectedNotes = detectedNotes.Distinct().OrderBy(n => n).ToList();

            // Wyświetlanie diagnozy
            Console.WriteLine($"Częstotliwości wykryte: {string.Join(", ", detectedFrequencies.Select(f => f.ToString("F2") + " Hz"))}");
            Console.WriteLine($"Nuty wykryte: {string.Join(", ", detectedNotes)}");

            return detectedNotes;
        }

        /// <summary>
        /// Klasteryzuje częstotliwości, łącząc blisko położone wartości.
        /// </summary>
        private List<double> ClusterFrequencies(List<double> frequencies, double clusterWidth) {
            frequencies.Sort();
            List<double> clusteredFrequencies = new List<double>();
            double clusterStart = frequencies[0];
            double clusterSum = frequencies[0];
            int clusterCount = 1;

            for (int i = 1; i < frequencies.Count; i++) {
                if (frequencies[i] - frequencies[i - 1] <= clusterWidth) {
                    clusterSum += frequencies[i];
                    clusterCount++;
                } else {
                    clusteredFrequencies.Add(clusterSum / clusterCount);
                    clusterStart = frequencies[i];
                    clusterSum = frequencies[i];
                    clusterCount = 1;
                }
            }
            clusteredFrequencies.Add(clusterSum / clusterCount);
            return clusteredFrequencies;
        }
    }
}
