using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Przypisuje nuty do ramek na podstawie danych audio.
        /// </summary>
public void AssignNotes() {
    // Wykrywanie szczytów w sygnale
    SignalProcessor signalProcessor = new SignalProcessor();
    PeakIndices = signalProcessor.DetectPeaks(this);

    // Konwersja danych audio na float[]
    float[] samples = Frames.Select(f => (float)f.Data).ToArray();

    // Utworzenie obiektu NoteDetector
    NoteDetector noteDetector = new NoteDetector();

    // Analiza częstotliwości z użyciem STFT tylko dla ramek z indeksami z DetectPeaks
    foreach (var peakIndex in PeakIndices) {
        var notes = noteDetector.AnalyzeFrequenciesWithSTFT(samples, (int)SampleRate, peakIndex);

        // Ustawienie nut w odpowiedniej ramce
        int frameIndex = (int)(peakIndex);
        if (frameIndex < Frames.Length) {
            Frames[frameIndex].Notes = notes;

            // Wypisywanie danych debugowych
            double timeInSeconds = frameIndex / (double)SampleRate;
            string formattedTime = FormatTime((int)(timeInSeconds * 1000));
            Console.WriteLine($"Czas: {formattedTime}, Nuty: {string.Join(" ", notes)}");
        }
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
        public List<string> Notes { get; set; } // Zastępuje pola Note i Chord
    }


}
