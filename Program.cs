using System;

namespace HarmonyHacker {
    class Program {
        static void Main(string[] args) {
            // Zakomentowane, ponieważ znamy ścieżkę do pliku MP3
            // if (args.Length < 1)
            // {
            //     Console.WriteLine("Usage: HarmonyHacker <input_mp3_file>");
            //     return;
            // }

            // string mp3FilePath = args[0];
            string mp3FilePath = @"C:\Users\morid\OneDrive\Pulpit\sample.mp3";
            string wavFilePath = System.IO.Path.ChangeExtension(mp3FilePath, ".wav");

            var soundWave = SoundReader.ConvertAndReadWavFile(mp3FilePath, wavFilePath);

            Console.WriteLine("[SoundWave] Info:");
            Console.WriteLine($"[SoundWave] Number of Frames: {soundWave.NumberOfFrames}");
            Console.WriteLine($"[SoundWave] Duration: {soundWave.Duration} seconds");
            Console.WriteLine($"[SoundWave] Sample Rate: {soundWave.SampleRate}");
            Console.WriteLine($"[SoundWave] Bits Per Sample: {soundWave.BitsPerSample}");

            // Zakomentowane wyświetlanie danych WAV
            // short? previousData = null;
            // foreach (var frame in soundWave.Frames)
            // {
            //     if (frame.Data != previousData)
            //     {
            //         string time = frame.Time.ToString(@"mm\:ss\.fff");
            //         Console.WriteLine($"{time} {frame.Data} [{frame.Index}]");
            //         previousData = frame.Data;
            //     }
            // }
        }
    }
}
