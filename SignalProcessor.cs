using System;
using System.Collections.Generic;
using System.Linq;

namespace HarmonyHacker
{
    public class SignalProcessor
    {
        /// <summary>
        /// Klasa przechowująca wartość amplitudy i jej indeks.
        /// </summary>
        public class AmplitudeData
        {
            public int Index { get; set; }
            public short Amplitude { get; set; }
        }

        /// <summary>
        /// Filtruje dane, pozostawiając tylko dodatnie wartości amplitudy i usuwając wartości poniżej średniej.
        /// </summary>
        public List<AmplitudeData> FilterPositiveAmplitudes(short[] data)
        {
            List<AmplitudeData> filteredData = new List<AmplitudeData>();
            double averageAmplitude = data.Where(d => d > 0).Average(d => (double)d);

            for (int i = 0; i < data.Length; i++)
            {
                // Dodajemy tylko dodatnie wartości amplitudy powyżej średniej do listy filteredData
                if (data[i] > 0 && data[i] >= averageAmplitude)
                {
                    filteredData.Add(new AmplitudeData { Index = i, Amplitude = data[i] });
                }
            }
            return filteredData;
        }

        /// <summary>
        /// Wygładza sygnał za pomocą średniej ruchomej.
        /// </summary>
        public List<AmplitudeData> SmoothSignal(List<AmplitudeData> data, int windowSize = 7)
        {
            List<AmplitudeData> smoothedData = new List<AmplitudeData>();

            for (int i = 0; i < data.Count; i++)
            {
                int start = Math.Max(0, i - windowSize / 2);
                int end = Math.Min(data.Count - 1, i + windowSize / 2);
                double sum = 0;
                int count = 0;

                for (int j = start; j <= end; j++)
                {
                    sum += data[j].Amplitude;
                    count++;
                }

                short smoothedAmplitude = (short)(sum / count);
                smoothedData.Add(new AmplitudeData { Index = data[i].Index, Amplitude = smoothedAmplitude });
            }

            return smoothedData;
        }

        /// <summary>
        /// Filtruje dane, znajdując maksima w ramkach czasowych.
        /// </summary>
        public List<AmplitudeData> FilterMaxima(List<AmplitudeData> data, int windowSize = 11)
        {
            List<AmplitudeData> maxima = new List<AmplitudeData>();

            for (int i = 0; i < data.Count; i += windowSize)
            {
                int start = i;
                int end = Math.Min(i + windowSize, data.Count);
                short maxAmplitude = 0;
                int maxIndex = -1;

                for (int j = start; j < end; j++)
                {
                    // Znajdujemy maksymalną wartość amplitudy i jej indeks w oknie czasowym
                    if (data[j].Amplitude > maxAmplitude)
                    {
                        maxAmplitude = data[j].Amplitude;
                        maxIndex = data[j].Index;
                    }
                }

                // Dodajemy maksymalną wartość amplitudy do listy maxima
                if (maxIndex != -1)
                {
                    maxima.Add(new AmplitudeData { Index = maxIndex, Amplitude = maxAmplitude });
                }
            }

            return maxima;
        }

        /// <summary>
        /// Filtruje dane, usuwając wartości poniżej progu amplitudy.
        /// </summary>
        public List<AmplitudeData> FilterByAmplitudeThreshold(List<AmplitudeData> data, short amplitudeThreshold)
        {
            List<AmplitudeData> filteredData = new List<AmplitudeData>();

            foreach (var currentMax in data)
            {
                if (filteredData.Count == 0)
                {
                    filteredData.Add(currentMax);
                }
                else
                {
                    var previousMax = filteredData.Last();
                    if (Math.Abs(currentMax.Amplitude - previousMax.Amplitude) >= amplitudeThreshold)
                    {
                        filteredData.Add(currentMax);
                    }
                    else
                    {
                        // Dodajemy większą wartość z dwóch porównywanych amplitud
                        var maxAmplitudeData = currentMax.Amplitude > previousMax.Amplitude ? currentMax : previousMax;
                        filteredData[filteredData.Count - 1] = maxAmplitudeData;
                    }
                }
            }

            return filteredData;
        }

        /// <summary>
        /// Filtruje dane, usuwając spadki amplitudy.
        /// </summary>
        public List<AmplitudeData> FilterDrops(List<AmplitudeData> data)
        {
            List<AmplitudeData> filteredData = new List<AmplitudeData>();

            for (int i = 0; i < data.Count; i++)
            {
                if (i == 0 || data[i].Amplitude >= data[i - 1].Amplitude)
                {
                    filteredData.Add(data[i]);
                }
            }

            return filteredData;
        }

        /// <summary>
        /// Filtruje dane, usuwając szczyty o amplitudzie poniżej określonego progu.
        /// </summary>
        public List<AmplitudeData> FilterLowPeaks(List<AmplitudeData> data, short peakThreshold)
        {
            return data.Where(d => d.Amplitude >= peakThreshold).ToList();
        }

        /// <summary>
        /// Znajduje szczyty w danych fali dźwiękowej.
        /// </summary>
        public List<int> FindPeaks(List<AmplitudeData> data, short amplitudeThreshold = 2000, short peakThreshold = 5000)
        {
            // Filtrujemy dane, znajdując maksima w ramkach czasowych
            List<AmplitudeData> maxima = FilterMaxima(data);

            // Wygładzamy sygnał za pomocą średniej ruchomej
            List<AmplitudeData> smoothedData = SmoothSignal(maxima);

            // Filtrujemy dane, usuwając wartości poniżej progu amplitudy
            smoothedData = FilterByAmplitudeThreshold(smoothedData, amplitudeThreshold);

            // Filtrujemy dane, usuwając spadki amplitudy
            smoothedData = FilterDrops(smoothedData);

            // Filtrujemy dane, usuwając szczyty o amplitudzie poniżej określonego progu
            //smoothedData = FilterLowPeaks(smoothedData, peakThreshold);

            // Tworzymy listę indeksów maksimów
            List<int> peakIndices = smoothedData.Select(max => max.Index).ToList();

            return peakIndices;
        }

        /// <summary>
        /// Wykrywa szczyty i wypisuje dane debugowe.
        /// </summary>
        public void DetectAndDebugPeaks(SoundWave wave)
        {
            Console.WriteLine("\nWykrywanie szczytów...");

            // Filtruj dane, pozostawiając tylko dodatnie wartości amplitudy powyżej średniej
            List<AmplitudeData> filteredData = FilterPositiveAmplitudes(wave.Frames.Select(f => f.Data).ToArray());

            // Wykrywanie szczytów
            List<int> peakIndices = FindPeaks(filteredData);

            // Przypisanie wykrytych szczytów do zmiennej PeakIndices w klasie SoundWave
            wave.PeakIndices = peakIndices;

            // Wypisywanie danych debugowych
            int peakCount = 0;
            foreach (int index in peakIndices)
            {
                peakCount++;
                string note = wave.Frames[index].Note ?? "---";
                Console.WriteLine($"Szczyt {peakCount} znaleziony na indeksie {index}, czas: {wave.Frames[index].Time.TotalMilliseconds} ms, amplituda: {wave.Frames[index].Data}, nuta: {note}");
            }

            Console.WriteLine($"Liczba wykrytych szczytów: {peakCount}");
        }
    }
}
