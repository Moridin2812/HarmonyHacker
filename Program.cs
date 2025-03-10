﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HarmonyHacker {
    class Program {
        static void Main(string[] args) {
            // Zakomentowane, ponieważ znamy ścieżkę do pliku MP3
            // if (args.Length < 1)
            // {
            //     Console.WriteLine("Usage: HarmonyHacker <input_mp3_file>");
            //     return;
            // }

            // Ścieżka do pliku MP3
            string mp3FilePath = @"C:\Users\morid\OneDrive\Pulpit\sample.mp3";

            // Ścieżka do pliku WAV
            string wavFilePath = System.IO.Path.ChangeExtension(mp3FilePath, ".wav");

            // Konwersja i odczyt pliku WAV
            var soundWave = SoundReader.ConvertAndReadWavFile(mp3FilePath, wavFilePath);

            Console.WriteLine("[SoundWave] Info:");
            Console.WriteLine($"[SoundWave] Number of Frames: {soundWave.NumberOfFrames}");
            Console.WriteLine($"[SoundWave] Duration: {soundWave.Duration} seconds");
            Console.WriteLine($"[SoundWave] Sample Rate: {soundWave.SampleRate}");
            Console.WriteLine($"[SoundWave] Bits Per Sample: {soundWave.BitsPerSample}");

            // Wyświetlanie niepowtarzających się nut w czasie ich wystąpienia
            List<string> previousNotes = null;
            foreach (var frame in soundWave.Frames) {
                if (frame.Notes != null && frame.Notes.Any()) {
                    string time = frame.Time.ToString("mm':'ss'.'fff");
                    string notesString = string.Join(", ", frame.Notes);
                    Console.WriteLine($"Czas: {time} - Nuty: {notesString} ({frame.Data} Hz)");
                    previousNotes = frame.Notes;
                }
            }

            // Rysowanie wykresu i zapis do pliku PNG
            var plotter = new WavePlotter();
            string pngFilePath = System.IO.Path.ChangeExtension(mp3FilePath, ".png");
            plotter.PlotWave(soundWave, pngFilePath);

            Console.WriteLine($"Wykres zapisany do pliku: {pngFilePath}");
        }
    }
}
