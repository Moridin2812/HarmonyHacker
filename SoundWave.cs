namespace HarmonyHacker {
    public class SoundWave {
        public int NumberOfFrames { get; set; }
        public System.TimeSpan Duration { get; set; }
        public uint SampleRate { get; set; }
        public ushort BitsPerSample { get; set; }
        public Frame[] Frames { get; set; }
    }

    public class Frame {
        public int Index { get; set; }
        public System.TimeSpan Time { get; set; }
        public short Data { get; set; }
    }
}
