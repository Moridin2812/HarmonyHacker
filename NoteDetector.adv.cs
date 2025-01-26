using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace HarmonyHacker {
    public class NoteDetectorAdv {
        /// <summary>
        /// Analizuje częstotliwości z użyciem STFT i detekcji nut.
        /// </summary>
        public List<string> AnalyzeFrequenciesWithSTFT(float[] samples, int sampleRate, long peakIndex, bool enableChordDetection) {
            int windowSize = 2048; // Większe okno dla lepszej rozdzielczości częstotliwości
            int hopSize = windowSize / 2; // Nakładanie okien (50%)

            // Pobieramy próbki z nakładaniem
            int startSample = (int)peakIndex - (windowSize / 2);
            if (startSample < 0) startSample = 0;
            float[] windowSamples = samples.Skip(startSample).Take(windowSize).ToArray();

            // Sprawdzamy, czy mamy wystarczająco dużo próbek
            if (windowSamples.Length < windowSize)
                return new List<string>();

            // Usunięcie średniej (DC offset)
            float mean = windowSamples.Average();
            for (int i = 0; i < windowSamples.Length; i++) {
                windowSamples[i] -= mean;
            }

            // Normalizacja sygnału
            float maxAmplitude = windowSamples.Max(s => Math.Abs(s));
            if (maxAmplitude > 0) {
                for (int i = 0; i < windowSamples.Length; i++) {
                    windowSamples[i] /= maxAmplitude;
                }
            }

            // Zastosuj okno Hanninga
            var window = Window.Hann(windowSize);
            for (int j = 0; j < windowSize; j++) {
                windowSamples[j] *= (float)window[j];
            }

            List<string> detectedNotes = new List<string>();

            if (enableChordDetection) {
                // *** Wykrywanie wielu nut (akordu) ***

                // FFT (Fast Fourier Transform)
                var complexSamples = windowSamples.Select(s => new Complex(s, 0)).ToArray();
                Fourier.Forward(complexSamples, FourierOptions.NoScaling);

                var magnitudes = complexSamples.Take(complexSamples.Length / 2).Select(c => c.Magnitude).ToArray();

                // Zwiększenie progu amplitudy dla detekcji częstotliwości
                double threshold = magnitudes.Max() * 0.3; // 30% maksymalnej amplitudy

                // Wykrywanie wielu częstotliwości dominujących
                List<int> peakIndices = FindLocalMaxima(magnitudes, threshold);
                foreach (var idx in peakIndices) {
                    double frequency = idx * sampleRate / windowSize;

                    // Mapowanie częstotliwości na numer MIDI
                    int midiNumber = FrequencyToMidi(frequency);
                    string noteName = GetNoteName(midiNumber);
                    detectedNotes.Add(noteName);
                }
            } else {
                // *** Wykrywanie pojedynczej nuty z użyciem autokorelacji ***

                double fundamentalFrequency = DetectFundamentalFrequencyUsingAutocorrelation(windowSamples, sampleRate);

                if (fundamentalFrequency > 0) {
                    // Mapowanie częstotliwości na numer MIDI
                    int midiNumber = FrequencyToMidi(fundamentalFrequency);
                    string noteName = GetNoteName(midiNumber);
                    detectedNotes.Add(noteName);

                    // Wyświetlanie diagnostyki
                    Console.WriteLine($"Fundamental Frequency: {fundamentalFrequency:F2} Hz, MIDI Number: {midiNumber}, Note: {noteName}");
                } else {
                    Console.WriteLine("Nie udało się wykryć częstotliwości podstawowej.");
                }
            }

            // Usuwamy duplikaty
            detectedNotes = detectedNotes.Distinct().ToList();

            return detectedNotes;
        }

        /// <summary>
        /// Wykrywa częstotliwość podstawową za pomocą metody autokorelacji.
        /// </summary>
        private double DetectFundamentalFrequencyUsingAutocorrelation(float[] samples, int sampleRate) {
            int size = samples.Length;
            double[] autocorrelation = new double[size];

            // Oblicz autokorelację
            for (int lag = 0; lag < size; lag++) {
                double sum = 0;
                for (int i = 0; i < size - lag; i++) {
                    sum += samples[i] * samples[i + lag];
                }
                autocorrelation[lag] = sum / (size - lag); // Normalizacja
            }

            // Znajdź pierwsze lokalne maksimum po lag = 0
            int minLag = sampleRate / 2000; // Dla 2000 Hz
            int maxLag = sampleRate / 50;   // Dla 50 Hz

            double prevAutocorrelation = autocorrelation[minLag];
            int peakIndex = -1;

            for (int lag = minLag + 1; lag <= maxLag; lag++) {
                if (autocorrelation[lag] > prevAutocorrelation && autocorrelation[lag] > autocorrelation[lag + 1]) {
                    peakIndex = lag;
                    break;
                }
                prevAutocorrelation = autocorrelation[lag];
            }

            if (peakIndex == -1) {
                // Jeśli nie znaleziono lokalnego maksimum, zwracamy 0
                return 0.0;
            }

            double fundamentalFrequency = (double)sampleRate / peakIndex;

            // Diagnostyka
            Console.WriteLine($"Autocorrelation Peak Index: {peakIndex}, Fundamental Frequency: {fundamentalFrequency:F2} Hz");

            return fundamentalFrequency;
        }

        /// <summary>
        /// Znajduje lokalne maksima w tablicy amplitud powyżej zadanego progu.
        /// </summary>
        private List<int> FindLocalMaxima(double[] magnitudes, double threshold) {
            List<int> peakIndices = new List<int>();
            for (int i = 1; i < magnitudes.Length - 1; i++) {
                if (magnitudes[i] > magnitudes[i - 1] && magnitudes[i] > magnitudes[i + 1] && magnitudes[i] >= threshold) {
                    peakIndices.Add(i);
                }
            }
            return peakIndices;
        }

        /// <summary>
        /// Konwertuje częstotliwość na numer MIDI.
        /// </summary>
        private int FrequencyToMidi(double frequency) {
            if (frequency <= 0)
                return 0; // Brak dźwięku lub nieprawidłowa częstotliwość

            double midiNumber = 69 + 12 * Math.Log(frequency / 440.0, 2);
            int roundedMidiNumber = (int)Math.Round(midiNumber);

            // Ograniczamy zakres do realnych numerów MIDI (21 - 108 dla standardowego pianina)
            if (roundedMidiNumber < 21) roundedMidiNumber = 21;
            if (roundedMidiNumber > 108) roundedMidiNumber = 108;

            return roundedMidiNumber;
        }

        /// <summary>
        /// Zwraca nazwę nuty na podstawie numeru MIDI.
        /// </summary>
        private string GetNoteName(int midiNoteNumber) {
            if (midiNoteNumber == 0)
                return "Nieznana";

            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int octave = (midiNoteNumber / 12) - 1;
            int noteIndex = midiNoteNumber % 12;
            return $"{noteNames[noteIndex]}{octave}";
        }
    }
}
