using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace HarmonyHacker {
    // Klasa reprezentująca falę dźwiękową
    public class SoundWave {
        // Liczba ramek w fali dźwiękowej
        public int NumberOfFrames { get; set; }
        // Czas trwania fali dźwiękowej
        public TimeSpan Duration { get; set; }
        // Częstotliwość próbkowania
        public uint SampleRate { get; set; }
        // Liczba bitów na próbkę
        public ushort BitsPerSample { get; set; }
        // Tablica ramek
        public Frame[] Frames { get; set; }

        // Tablica przechowująca indeksy szczytów
        public List<int> PeakIndices { get; set; } = new List<int>();

        /// <summary>
        /// Generuje słownik częstotliwości nut.
        /// </summary>
        public static Dictionary<double, string> GenerateNoteFrequencies() {
            var notes = new Dictionary<double, string>();
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            // Zakres od C0 do B8
            for (int octave = 0; octave <= 8; octave++) {
                for (int i = 0; i < 12; i++) {
                    int midiNumber = (octave * 12) + i;
                    double frequency = 440.0 * Math.Pow(2, (midiNumber - 69) / 12.0);
                    string noteName = $"{noteNames[i]}{octave}";
                    notes[frequency] = noteName;
                }
            }

            // Dodaj oznaczenie ciszy
            notes[0.0] = "---";
            return notes;
        }

        /// <summary>
        /// Przypisuje nuty do ramek na podstawie danych audio.
        /// </summary>
        public void AssignNotes() {
            // Wywołaj funkcję DetectAndDebugPeaks na początku
            SignalProcessor signalProcessor = new SignalProcessor();
            signalProcessor.DetectPeaks(this);

            var noteFrequencies = GenerateNoteFrequencies();
            var frequencies = noteFrequencies.Keys.ToArray();

            // Konwersja danych audio na float[]
            float[] samples = Frames.Select(f => (float)f.Data).ToArray();

            // Analiza częstotliwości z użyciem STFT
            AnalyzeFrequenciesWithSTFT(samples, (int)SampleRate, noteFrequencies);
        }

        /// <summary>
        /// Analizuje częstotliwości z użyciem STFT.
        /// </summary>
        public void AnalyzeFrequenciesWithSTFT(float[] samples, int sampleRate, Dictionary<double, string> noteFrequencies) {
            Console.WriteLine("\nWykonywanie analizy częstotliwości...");

            int windowSize = 2048; // Rozmiar okna
            int hopSize = 1024; // Przesunięcie okna
            double amplitudeThreshold = 0.005; // Próg amplitudy

            string previousNoteName = null;
            int totalWindows = (samples.Length - windowSize) / hopSize;

            for (int i = 0; i <= totalWindows; i++) {
                int startIndex = i * hopSize;
                float[] windowSamples = new float[windowSize];

                // Skopiuj próbki do okna
                Array.Copy(samples, startIndex, windowSamples, 0, windowSize);

                // Zastosuj okno Hanninga
                var window = Window.Hann(windowSize);
                for (int j = 0; j < windowSize; j++) {
                    windowSamples[j] *= (float)window[j];
                }

                // Oblicz RMS (Root Mean Square) - miara energii sygnału
                double rms = Math.Sqrt(windowSamples.Select(s => s * s).Average());

                string noteName;
                double noteFrequency;

                if (rms < amplitudeThreshold) {
                    // Cisza
                    noteName = "---";
                    noteFrequency = 0.0;
                } else {
                    // FFT (Fast Fourier Transform)
                    var complexWindow = windowSamples.Select(s => new Complex32(s, 0)).ToArray();
                    Fourier.Forward(complexWindow, FourierOptions.NoScaling);
                    var magnitudes = complexWindow.Take(complexWindow.Length / 2).Select(c => c.Magnitude).ToArray();
                    int maxIndex = Array.IndexOf(magnitudes, magnitudes.Max());
                    double frequency = maxIndex * sampleRate / windowSize;

                    // Mapuj częstotliwość na najbliższą nutę
                    var closestNote = noteFrequencies.OrderBy(n => Math.Abs(n.Key - frequency)).First();
                    noteName = closestNote.Value;
                    noteFrequency = closestNote.Key;
                }

                // Oblicz czas
                double windowTime = startIndex / (double)sampleRate;
                int timeMilliseconds = (int)(windowTime * 1000);

                // Ustaw nutę w odpowiedniej ramce
                int frameIndex = (int)(timeMilliseconds * SampleRate / 1000);
                if (frameIndex < Frames.Length) {
                    Frames[frameIndex].Note = noteName;
                }

                previousNoteName = noteName;
            }
        }

        /// <summary>
        /// Formatuje czas w milisekundach na format mm:ss.fff.
        /// </summary>
        public static string FormatTime(int timeMilliseconds) {
            TimeSpan time = TimeSpan.FromMilliseconds(timeMilliseconds);
            return time.ToString(@"mm\:ss\.fff");
        }
    }

    // Klasa reprezentująca ramkę dźwiękową
    public class Frame {
        public int Index { get; set; }
        public TimeSpan Time { get; set; }
        public short Data { get; set; }
        public string Note { get; set; } // Dodane pole Note
    }

    // Klasa reprezentująca nutę
    public class Note {
        public double Frequency { get; set; }
        public string Name { get; set; }
        public int TimeMilliseconds { get; set; }
    }
}
