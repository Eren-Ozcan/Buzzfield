using System;
using System.Collections.Generic;
using System.IO;
using Buzzfield.Audio;
using UnityEditor;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Fills the sound library from Assets/_Project/Audio by file name (docs/audio.md): an effect
    /// takes every clip named after it, with or without a _1, _2... variant suffix; a loop takes
    /// the clip with its exact name. Runs from Create Default Data, from the menu
    /// (Buzzfield > Audio > Link Clips) and after audio files are imported, so a new file needs
    /// no manual wiring. Also sets the import settings of everything in that folder.
    /// </summary>
    public sealed class SoundClipLinker : AssetPostprocessor
    {
        public const string AudioRoot = EditorAssets.Root + "/Audio";
        const string LibraryPath = EditorAssets.DataRoot + "/Settings/SoundLibrary.asset";

        [MenuItem("Buzzfield/Audio/Link Clips", priority = 30)]
        public static void LinkFromMenu()
        {
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (library == null)
            {
                Debug.LogError("No sound library yet. Run Buzzfield > Create Default Data first.");
                return;
            }
            EnsureEntries(library);
            Link(library);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Adds an entry with the default mix for every effect and loop the library does not list yet.</summary>
        public static void EnsureEntries(SoundLibrary library)
        {
            var so = new SerializedObject(library);
            SerializedProperty effects = so.FindProperty("effects");
            var haveEffects = new HashSet<int>();
            for (int i = 0; i < effects.arraySize; i++)
                haveEffects.Add(effects.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex);
            foreach (Sfx id in Enum.GetValues(typeof(Sfx)))
            {
                if (haveEffects.Contains((int)id))
                    continue;
                (SoundBus bus, float volume, float perSecond, float jitter) = DefaultMix(id);
                effects.arraySize++;
                SerializedProperty entry = effects.GetArrayElementAtIndex(effects.arraySize - 1);
                entry.FindPropertyRelative("id").enumValueIndex = (int)id;
                entry.FindPropertyRelative("bus").enumValueIndex = (int)bus;
                entry.FindPropertyRelative("clips").arraySize = 0;
                entry.FindPropertyRelative("volume").floatValue = volume;
                entry.FindPropertyRelative("maxPerSecond").floatValue = perSecond;
                entry.FindPropertyRelative("pitchJitter").floatValue = jitter;
            }

            SerializedProperty loops = so.FindProperty("loops");
            var haveLoops = new HashSet<int>();
            for (int i = 0; i < loops.arraySize; i++)
                haveLoops.Add(loops.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex);
            foreach (SoundLoop id in Enum.GetValues(typeof(SoundLoop)))
            {
                if (haveLoops.Contains((int)id))
                    continue;
                loops.arraySize++;
                SerializedProperty entry = loops.GetArrayElementAtIndex(loops.arraySize - 1);
                entry.FindPropertyRelative("id").enumValueIndex = (int)id;
                entry.FindPropertyRelative("clip").objectReferenceValue = null;
                entry.FindPropertyRelative("volume").floatValue = DefaultLoopVolume(id);
            }
            if (so.ApplyModifiedPropertiesWithoutUndo())
                EditorUtility.SetDirty(library);
        }

        /// <summary>Points every entry at the clips in the audio folder named after it; entries with no file get none.</summary>
        public static void Link(SoundLibrary library)
        {
            Dictionary<string, AudioClip> clips = FindClips();
            var so = new SerializedObject(library);
            SerializedProperty effects = so.FindProperty("effects");
            int linked = 0;
            for (int i = 0; i < effects.arraySize; i++)
            {
                SerializedProperty entry = effects.GetArrayElementAtIndex(i);
                string name = SoundFiles.Name((Sfx)entry.FindPropertyRelative("id").enumValueIndex);
                List<AudioClip> matches = Variants(clips, name);
                SerializedProperty list = entry.FindPropertyRelative("clips");
                list.arraySize = matches.Count;
                for (int c = 0; c < matches.Count; c++)
                    list.GetArrayElementAtIndex(c).objectReferenceValue = matches[c];
                linked += matches.Count;
            }

            SerializedProperty loops = so.FindProperty("loops");
            for (int i = 0; i < loops.arraySize; i++)
            {
                SerializedProperty entry = loops.GetArrayElementAtIndex(i);
                string name = SoundFiles.Name((SoundLoop)entry.FindPropertyRelative("id").enumValueIndex);
                clips.TryGetValue(name, out AudioClip clip);
                entry.FindPropertyRelative("clip").objectReferenceValue = clip;
                if (clip != null)
                    linked++;
            }
            if (so.ApplyModifiedPropertiesWithoutUndo())
                EditorUtility.SetDirty(library);
            Debug.Log($"Sound library: {linked} clips linked from {AudioRoot}.");
        }

        // ---- Import ----

        /// <summary>
        /// Effects decompress on load (ADPCM, mono): they must start on the frame they are played.
        /// Music and ambience loops stream (Vorbis), so long files cost little memory.
        /// </summary>
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot + "/", StringComparison.Ordinal))
                return;
            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool loop = assetPath.Contains("/Music/") || assetPath.Contains("/Ambience/");
            if (loop)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.65f;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                importer.forceToMono = true;
            }
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = loop;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!TouchesAudio(imported) && !TouchesAudio(deleted) && !TouchesAudio(moved) && !TouchesAudio(movedFrom))
                return;
            // Link once the import has settled; the library may be mid-import itself.
            EditorApplication.delayCall += () =>
            {
                var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
                if (library == null)
                    return;
                Link(library);
                AssetDatabase.SaveAssets();
            };
        }

        static bool TouchesAudio(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.StartsWith(AudioRoot + "/", StringComparison.Ordinal) && IsAudioFile(path))
                    return true;
            }
            return false;
        }

        static bool IsAudioFile(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".wav" || extension == ".ogg" || extension == ".mp3" || extension == ".aif" || extension == ".aiff";
        }

        static Dictionary<string, AudioClip> FindClips()
        {
            var clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            if (!AssetDatabase.IsValidFolder(AudioRoot))
                return clips;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    clips[Path.GetFileNameWithoutExtension(path)] = clip;
            }
            return clips;
        }

        /// <summary>Clips named <paramref name="name"/> or <paramref name="name"/>_N, in name order.</summary>
        static List<AudioClip> Variants(Dictionary<string, AudioClip> clips, string name)
        {
            var names = new List<string>();
            foreach (string key in clips.Keys)
            {
                if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase) || IsVariant(key, name))
                    names.Add(key);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            var result = new List<AudioClip>(names.Count);
            foreach (string key in names)
                result.Add(clips[key]);
            return result;
        }

        static bool IsVariant(string key, string name)
        {
            if (key.Length <= name.Length + 1 || !key.StartsWith(name + "_", StringComparison.OrdinalIgnoreCase))
                return false;
            for (int i = name.Length + 1; i < key.Length; i++)
            {
                if (!char.IsDigit(key[i]))
                    return false;
            }
            return true;
        }

        /// <summary>Bus, level, plays per second and pitch spread of each effect (docs/audio.md, Mixing rules).</summary>
        static (SoundBus, float, float, float) DefaultMix(Sfx id)
        {
            switch (id)
            {
                case Sfx.UiTap: return (SoundBus.Ui, 0.6f, 12f, 0.04f);
                case Sfx.UiRefused: return (SoundBus.Ui, 0.7f, 4f, 0f);
                case Sfx.UiOpen: return (SoundBus.Ui, 0.6f, 4f, 0f);
                case Sfx.UiClose: return (SoundBus.Ui, 0.6f, 4f, 0f);
                case Sfx.UpgradeBuy: return (SoundBus.Effects, 0.8f, 8f, 0.05f);
                case Sfx.BeeSpawn: return (SoundBus.Effects, 0.8f, 8f, 0.06f);
                case Sfx.BeeEvolve: return (SoundBus.Effects, 0.9f, 4f, 0f);
                case Sfx.HoneyDeposit: return (SoundBus.Effects, 0.35f, 4f, 0.08f);
                case Sfx.PollenShake: return (SoundBus.Effects, 0.7f, 8f, 0.06f);
                case Sfx.FlowerBloom: return (SoundBus.Effects, 0.8f, 6f, 0f);
                case Sfx.SproutPop: return (SoundBus.Effects, 0.6f, 6f, 0.05f);
                case Sfx.GardenComplete:
                case Sfx.QueenMove:
                case Sfx.PurchaseSuccess:
                    return (SoundBus.Stinger, 1f, 1f, 0f);
                case Sfx.ButterflyAppear: return (SoundBus.Effects, 0.6f, 1f, 0f);
                default: return (SoundBus.Effects, 0.9f, 2f, 0f);
            }
        }

        static float DefaultLoopVolume(SoundLoop id)
        {
            switch (id)
            {
                case SoundLoop.Music: return 0.6f;
                case SoundLoop.AmbienceDry:
                case SoundLoop.AmbienceBloom:
                    return 0.35f;
                default: return 0.3f;
            }
        }
    }
}
