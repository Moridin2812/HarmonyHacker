using System;
using System.IO;
using NAudio.Wave;

namespace HarmonyHacker {
    public class SoundReader {
        public static SoundWave ConvertAndReadWavFile(string mp3FilePath, string wavFilePath) {
            ConvertMp3ToWav(mp3FilePath, wavFilePath);
            return ReadWavFile(wavFilePath);
        }

        public static void ConvertMp3ToWav(string mp3FilePath, string wavFilePath) {
            using (var reader = new MediaFoundationReader(mp3FilePath))
            using (var resampler = new MediaFoundationResampler(reader, new WaveFormat(44100, 1))) {
                WaveFileWriter.CreateWaveFile(wavFilePath, resampler);
            }
        }

        public static SoundWave ReadWavFile(string filePath) {
            using (var reader = new BinaryReader(File.OpenRead(filePath))) {
                var wavFile = new WavFile();

                // RIFF Header
                wavFile.ChunkID = new string(reader.ReadChars(4));
                wavFile.ChunkSize = reader.ReadUInt32();
                wavFile.Format = new string(reader.ReadChars(4));

                // fmt Subchunk
                wavFile.Subchunk1ID = new string(reader.ReadChars(4));
                wavFile.Subchunk1Size = reader.ReadUInt32();
                wavFile.AudioFormat = reader.ReadUInt16();
                wavFile.NumChannels = reader.ReadUInt16();
                wavFile.SampleRate = reader.ReadUInt32();
                wavFile.ByteRate = reader.ReadUInt32();
                wavFile.BlockAlign = reader.ReadUInt16();
                wavFile.BitsPerSample = reader.ReadUInt16();

                // Skip any extra bytes in fmt subchunk
                if (wavFile.Subchunk1Size > 16) {
                    reader.ReadBytes((int)(wavFile.Subchunk1Size - 16));
                }

                // data Subchunk
                wavFile.Subchunk2ID = new string(reader.ReadChars(4));
                wavFile.Subchunk2Size = reader.ReadUInt32();

                if (wavFile.Subchunk2ID != "data") {
                    throw new InvalidDataException("Invalid WAV file: missing 'data' subchunk.");
                }

                wavFile.Data = new short[wavFile.Subchunk2Size / (wavFile.BitsPerSample / 8)];

                // Wczytywanie danych audio
                for (int i = 0; i < wavFile.Data.Length; i++) {
                    if (reader.BaseStream.Position + sizeof(short) > reader.BaseStream.Length) {
                        Console.WriteLine("Reached end of stream");
                        break;
                    }
                    wavFile.Data[i] = reader.ReadInt16();
                }

                // Tworzenie obiektu SoundWave
                var soundWave = new SoundWave {
                    NumberOfFrames = wavFile.Data.Length,
                    Duration = TimeSpan.FromSeconds((double)wavFile.Data.Length / wavFile.SampleRate),
                    SampleRate = wavFile.SampleRate,
                    BitsPerSample = wavFile.BitsPerSample,
                    Frames = new Frame[wavFile.Data.Length]
                };

                for (int i = 0; i < wavFile.Data.Length; i++) {
                    soundWave.Frames[i] = new Frame {
                        Index = i,
                        Time = TimeSpan.FromSeconds((double)i / wavFile.SampleRate),
                        Data = wavFile.Data[i]
                    };
                }

                // Przypisz nuty do ramek
                soundWave.AssignNotes();

                // Wyświetlanie informacji debugowania
                //DisplayDebugInfo(wavFile);

                return soundWave;
            }
        }

        private static void DisplayDebugInfo(WavFile wavFile) {
            Console.WriteLine("[WavFile] Debug Info:");
            Console.WriteLine($"[WavFile] ChunkID: {wavFile.ChunkID}");
            Console.WriteLine($"[WavFile] ChunkSize: {wavFile.ChunkSize}");
            Console.WriteLine($"[WavFile] Format: {wavFile.Format}");
            Console.WriteLine($"[WavFile] Subchunk1ID: {wavFile.Subchunk1ID}");
            Console.WriteLine($"[WavFile] Subchunk1Size: {wavFile.Subchunk1Size}");
            Console.WriteLine($"[WavFile] AudioFormat: {wavFile.AudioFormat}");
            Console.WriteLine($"[WavFile] NumChannels: {wavFile.NumChannels}");
            Console.WriteLine($"[WavFile] SampleRate: {wavFile.SampleRate}");
            Console.WriteLine($"[WavFile] ByteRate: {wavFile.ByteRate}");
            Console.WriteLine($"[WavFile] BlockAlign: {wavFile.BlockAlign}");
            Console.WriteLine($"[WavFile] BitsPerSample: {wavFile.BitsPerSample}");
            Console.WriteLine($"[WavFile] Subchunk2ID: {wavFile.Subchunk2ID}");
            Console.WriteLine($"[WavFile] Subchunk2Size: {wavFile.Subchunk2Size}");
        }
    }
}
