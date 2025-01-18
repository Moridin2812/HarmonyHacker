﻿namespace HarmonyHacker {
    public class WavFile {
        public string ChunkID { get; set; }
        public uint ChunkSize { get; set; }
        public string Format { get; set; }
        public string Subchunk1ID { get; set; }
        public uint Subchunk1Size { get; set; }
        public ushort AudioFormat { get; set; }
        public ushort NumChannels { get; set; }
        public uint SampleRate { get; set; }
        public uint ByteRate { get; set; }
        public ushort BlockAlign { get; set; }
        public ushort BitsPerSample { get; set; }
        public string Subchunk2ID { get; set; }
        public uint Subchunk2Size { get; set; }
        public short[] Data { get; set; }
    }
}
