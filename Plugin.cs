using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

namespace bepinex_soundmod
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        public static readonly Dictionary<string, AudioClip> replacedClips = [];

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            string soundsPath = Path.Combine(Path.GetDirectoryName(Info.Location), "sounds");
            Logger.LogInfo($"Sounds path: \"{soundsPath}\"");

            if (!Directory.Exists(soundsPath)) Directory.CreateDirectory(soundsPath);
            foreach (string file in Directory.GetFiles(soundsPath, "*", SearchOption.AllDirectories))
            {
                string format = Path.GetExtension(file).Substring(1);
                string name = Path.GetFileNameWithoutExtension(file);
                string fullName = $"{name}.{format}";
                Logger.LogInfo($"Loading file \"{fullName}\"...");

                AudioClip clip = LoadClip(Path.Combine(soundsPath, file), format.ToLower());
                if (clip != null)
                {
                    Logger.LogInfo($"Loaded file \"{fullName}\"!");
                    clip.name = name;
                    replacedClips.Add(name, clip);
                }
            }

            string keys = "";
            foreach (string key in replacedClips.Keys) keys += $"\"{key}\", ";
            Logger.LogInfo($"{replacedClips.Count} sound replacements: {keys.Substring(0, keys.Length - 2)}");

            Harmony.CreateAndPatchAll(typeof(AudioSourcePatch));
        }

        [HarmonyPatch(typeof(AudioSource))]
        internal class AudioSourcePatch
        {
            [HarmonyPatch(nameof(AudioSource.Play), [])]
            [HarmonyPrefix]
            public static void Play_Patch(AudioSource __instance) => ReplaceClip(__instance);
            [HarmonyPatch(nameof(AudioSource.Play), [typeof(ulong)])]
            [HarmonyPrefix]
            public static void Play_UlongPatch(AudioSource __instance) => ReplaceClip(__instance);
            [HarmonyPatch(nameof(AudioSource.Play), [typeof(double)])]
            [HarmonyPrefix]
            public static void Play_DoublePatch(AudioSource __instance) => ReplaceClip(__instance);
            [HarmonyPatch(nameof(AudioSource.PlayDelayed), [typeof(float)])]
            [HarmonyPrefix]
            public static void PlayDelayed_Patch(AudioSource __instance) => ReplaceClip(__instance);
            [HarmonyPatch(nameof(AudioSource.PlayOneShotHelper), [typeof(AudioSource), typeof(AudioClip), typeof(float)])]
            [HarmonyPrefix]
            public static void PlayOneShotHelper_Patch(AudioSource source, ref AudioClip clip, float volumeScale) => clip = ReplaceClip(clip, source);
            public static void ReplaceClip(AudioSource source) => source.clip = ReplaceClip(source.clip, source);
            public static AudioClip ReplaceClip(AudioClip clip, AudioSource source)
            {
                Logger.LogDebug($"Audio \"{(clip == null ? "unk" : clip.name)}\", source \"{(source == null ? "unk" : source.name)}\", replaced: {replacedClips.ContainsKey(clip.name)}");
                if (replacedClips.ContainsKey(clip.name)) return replacedClips[clip.name];
                return clip;
            }
        }

        public static AudioClip LoadClip(string path, string format)
        {
            AudioType type = AudioType.UNKNOWN;
            switch (format)
            {
                case "ogg":
                    type = AudioType.OGGVORBIS;
                    break;
                case "wav":
                    type = AudioType.WAV;
                    break;
                case "aif":
                case "aiff":
                    type = AudioType.AIFF;
                    break;
                case "acc":
                    type = AudioType.ACC;
                    break;
                case "mpg":
                case "mpeg":
                    type = AudioType.MPEG;
                    break;
            }

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, type))
            {
                uwr.SendWebRequest();

                try
                {
                    while (!uwr.isDone) { };

                    if (uwr.result == UnityWebRequest.Result.Success) return DownloadHandlerAudioClip.GetContent(uwr);
                    else Logger.LogWarning($"Failed to load clip: {uwr.error}");
                }
                catch (Exception err)
                {
                    Logger.LogError($"Caught error while loading clip: {err.Message}, {err.StackTrace}");
                }
            }

            return null;
        }
    }
}
