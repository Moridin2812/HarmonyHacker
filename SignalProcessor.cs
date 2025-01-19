using System;
using System.Collections.Generic;
using System.Linq;

namespace HarmonyHacker {
    public class SignalProcessor {
        /// <summary>
        /// Klasa przechowująca wartość amplitudy i jej indeks.
        /// </summary>
        public class AmplitudeData {
            public int Index { get; set; }
            public short Amplitude { get; set; }
        }

        /// <summary>
        /// Filtruje dane, pozostawiając tylko dodatnie wartości amplitudy.
        /// </summary>
        public List<AmplitudeData> FilterPositiveAmplitudes(short[] data) {
            List<AmplitudeData> filteredData = new List<AmplitudeData>();
            for (int i = 0; i < data.Length; i++) {
                if (data[i] > 0) {
                    filteredData.Add(new AmplitudeData { Index = i, Amplitude = data[i] });
                }
            }
            return filteredData;
        }

        /// <summary>
        /// Znajduje szczyty w danych fali dźwiękowej.
        /// </summary>
        public List<int> FindPeaks(List<AmplitudeData> data, short amplitudeThreshold = 200, int minDistance = 1000) {
            List<int> peakIndices = new List<int>();
            short previousAmplitude = 0;
            bool isFalling = false;

            for (int i = 0; i < data.Count; i++) {
                if (data[i].Amplitude > previousAmplitude) {
                    if (isFalling) {
                        peakIndices.Add(data[i].Index);
                        isFalling = false;
                    }
                    previousAmplitude = data[i].Amplitude;
                } else if (data[i].Amplitude < previousAmplitude) {
                    isFalling = true;
                }
            }

            return peakIndices;
        }

        /// <summary>
        /// Wykrywa szczyty i wypisuje dane debugowe.
        /// </summary>
        public void DetectAndDebugPeaks(SoundWave wave) {
            Console.WriteLine("\nWykrywanie szczytów...");

            // Filtruj dane, pozostawiając tylko dodatnie wartości amplitudy
            List<AmplitudeData> filteredData = FilterPositiveAmplitudes(wave.Frames.Select(f => f.Data).ToArray());

            // Wykrywanie szczytów
            List<int> peakIndices = FindPeaks(filteredData);

            // Przypisanie wykrytych szczytów do zmiennej PeakIndices w klasie SoundWave
            wave.PeakIndices = peakIndices;

            // Wypisywanie danych debugowych
            int peakCount = 0;
            foreach (int index in peakIndices) {
                peakCount++;
                string note = wave.Frames[index].Note ?? "---";
                Console.WriteLine($"Szczyt {peakCount} znaleziony na indeksie {index}, czas: {wave.Frames[index].Time.TotalMilliseconds} ms, amplituda: {wave.Frames[index].Data}, nuta: {note}");
            }

            Console.WriteLine($"Liczba wykrytych szczytów: {peakCount}");
        }
    }
}
