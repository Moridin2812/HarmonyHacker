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
        public List<long> PeakIndices { get; set; } = new List<long>();

        // Zmienna kontrolująca wykrywanie akordów
        public bool EnableChordDetection { get; set; } = false;

        /// <summary>
        /// Generuje słownik częstotliwości nut.
        /// </summary>
        public static Dictionary<double, string> GenerateNoteFrequencies() {
            var notes = new Dictionary<double, string>();
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            // Zakres od C0 do B8
            for (int octave = 0; octave <= 8; octave++) {
                for (int j = 0; j < 12; j++) {
                    int midiNumber = (octave * 12) + j;
                    double frequency = 440.0 * Math.Pow(2, (midiNumber - 69) / 12.0);
                    string noteName = $"{noteNames[j]}{octave}";
                    notes[frequency] = noteName;
                }
            }

            // Dodaj oznaczenie ciszy
            notes[0.0] = "---";

            return notes;
        }

        /// <summary>
        /// Generuje słownik mapujący nazwy nut na półtony.
        /// </summary>
        public static Dictionary<string, int> GenerateNoteToSemitoneDictionary() {
            var noteToSemitone = new Dictionary<string, int>();
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            for (int octave = 0; octave <= 8; octave++) {
                for (int i = 0; i < 12; i++) {
                    int semitone = i;
                    string noteName = $"{noteNames[i]}{octave}";
                    noteToSemitone[noteName] = semitone;
                }
            }

            return noteToSemitone;
        }

        /// <summary>
        /// Generuje słownik interwałów akordów.
        /// </summary>
        public static Dictionary<string, int[]> GenerateChordIntervals() {
            return new Dictionary<string, int[]>
            {
                { "Major", new int[] { 0, 4, 7 } },       // Pryma, tercja wielka, kwinta czysta
                { "Minor", new int[] { 0, 3, 7 } },       // Pryma, tercja mała, kwinta czysta
                { "Diminished", new int[] { 0, 3, 6 } },  // Pryma, tercja mała, kwinta zmniejszona
                { "Augmented", new int[] { 0, 4, 8 } },   // Pryma, tercja wielka, kwinta zwiększona
                { "Dominant7", new int[] { 0, 4, 7, 10 } } // Pryma, tercja wielka, kwinta czysta, septyma mała
                // Dodaj więcej akordów według potrzeb
            };
        }

        /// <summary>
        /// Przypisuje nuty i akordy do ramek na podstawie danych audio.
        /// </summary>
        public void AssignNotes() {
            // Wywołaj funkcję DetectPeaks na początku
            SignalProcessor signalProcessor = new SignalProcessor();
            PeakIndices = signalProcessor.DetectPeaks(this);

            var noteFrequencies = GenerateNoteFrequencies();

            // Konwersja danych audio na float[]
            float[] samples = Frames.Select(f => (float)f.Data).ToArray();

            // Analiza częstotliwości z użyciem STFT tylko dla ramek z indeksami z DetectPeaks
            foreach (var peakIndex in PeakIndices) {
                AnalyzeFrequenciesWithSTFT(samples, (int)SampleRate, noteFrequencies, peakIndex, EnableChordDetection);
            }
        }

        /// <summary>
        /// Analizuje częstotliwości z użyciem STFT.
        /// </summary>
        public void AnalyzeFrequenciesWithSTFT(float[] samples, int sampleRate, Dictionary<double, string> noteFrequencies, long peakIndex, bool enableChordDetection) {
            int windowSize = 2048; // Rozmiar okna

            // Bierzemy próbki od indeksu szczytu
            float[] windowSamples = samples.Skip((int)peakIndex).Take(windowSize).ToArray();

            // Sprawdzamy, czy mamy wystarczająco dużo próbek
            if (windowSamples.Length < windowSize)
                return;

            // Zastosuj okno Hanninga
            var window = Window.Hann(windowSize);
            for (int j = 0; j < windowSize; j++) {
                windowSamples[j] *= (float)window[j];
            }

            // FFT (Fast Fourier Transform)
            var complexSamples = windowSamples.Select(s => new Complex32(s, 0)).ToArray();
            Fourier.Forward(complexSamples, FourierOptions.NoScaling);
            var magnitudes = complexSamples.Take(complexSamples.Length / 2).Select(c => c.Magnitude).ToArray();

            if (enableChordDetection) {
                // *** Wykrywanie akordów ***

                // Wykrywanie wielu częstotliwości dominujących
                double threshold = magnitudes.Max() * 0.1; // Próg na 10% maksymalnej amplitudy
                List<double> detectedFrequencies = new List<double>();
                for (int i = 0; i < magnitudes.Length; i++) {
                    if (magnitudes[i] >= threshold) {
                        double frequency = i * sampleRate / (double)windowSize;
                        detectedFrequencies.Add(frequency);
                    }
                }

                // Mapowanie częstotliwości do nut
                List<string> detectedNotes = new List<string>();
                foreach (var freq in detectedFrequencies) {
                    var closestNote = noteFrequencies.OrderBy(n => Math.Abs(n.Key - freq)).First();
                    detectedNotes.Add(closestNote.Value);
                }

                // Detekcja akordu
                string detectedChord = DetectChord(detectedNotes);

                // Ustaw akord w odpowiedniej ramce
                int frameIndex = (int)(peakIndex);
                if (frameIndex < Frames.Length) {
                    Frames[frameIndex].Chord = detectedChord;
                }

                // Wypisywanie danych debugowych
                double timeInSeconds = frameIndex / (double)SampleRate;
                Console.WriteLine($"Akord: {detectedChord}, Czas: {FormatTime((int)(timeInSeconds * 1000))}");
            } else {
                // *** Wykrywanie pojedynczych nut ***

                // Znajdowanie częstotliwości o największej amplitudzie
                int maxIndex = Array.IndexOf(magnitudes, magnitudes.Max());
                double frequency = maxIndex * sampleRate / windowSize;

                // Mapowanie częstotliwości na najbliższą nutę
                var closestNote = noteFrequencies.OrderBy(n => Math.Abs(n.Key - frequency)).First();
                string noteName = closestNote.Value;
                double noteFrequency = closestNote.Key;

                // Ustaw nutę w odpowiedniej ramce
                int frameIndex = (int)(peakIndex);
                if (frameIndex < Frames.Length) {
                    Frames[frameIndex].Note = noteName;
                }

                // Wypisywanie danych debugowych
                double timeInSeconds = frameIndex / (double)SampleRate;
                Console.WriteLine($"Nuta: {noteName}, Częstotliwość: {noteFrequency}, Czas: {FormatTime((int)(timeInSeconds * 1000))}");
            }
        }

        /// <summary>
        /// Detekuje akord na podstawie wykrytych nut.
        /// </summary>
        public string DetectChord(List<string> notes) {
            // Generujemy słownik interwałów akordów
            var chordIntervals = GenerateChordIntervals();

            // Konwertujemy nazwy nut na wartości półtonów
            Dictionary<string, int> noteToSemitone = GenerateNoteToSemitoneDictionary();

            // Usuwamy duplikaty i sortujemy
            var uniqueNotes = notes.Distinct().ToList();

            // Sprawdzamy wszystkie kombinacje nut jako potencjalne toniki
            foreach (var rootNote in uniqueNotes) {
                int rootSemitone = noteToSemitone[rootNote];

                // Konwertujemy wykryte nuty na półtony względem toniki
                List<int> intervals = new List<int>();
                foreach (var note in uniqueNotes) {
                    int semitone = (noteToSemitone[note] - rootSemitone + 12) % 12;
                    intervals.Add(semitone);
                }

                // Sprawdzamy każdy akord w słowniku
                foreach (var chord in chordIntervals) {
                    if (chord.Value.All(intervals.Contains)) {
                        return $"{rootNote} {chord.Key}";
                    }
                }
            }

            return "Nieznany akord";
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
        public string Chord { get; set; } // Dodane pole Chord
    }
}
