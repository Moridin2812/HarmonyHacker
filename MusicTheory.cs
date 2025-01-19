using System;
using System.Collections.Generic;

namespace HarmonyHacker {
    public static class MusicTheory {
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
    }
}
