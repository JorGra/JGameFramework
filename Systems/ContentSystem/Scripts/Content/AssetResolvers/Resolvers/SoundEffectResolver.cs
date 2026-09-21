using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using JG.GameContent.AssetResolving;
using UnityEngine;
using UnityEngine.Networking;

namespace JGameFramework.Scripts.Modding.Content.AssetResolvers.Resolvers
{
    /// Resolves common audio formats (wav/ogg/mp3/aiff) into AudioClips for content definitions.
    internal sealed class SoundEffectResolver : IDescribedPathAssetResolver
    {
        private static readonly Dictionary<string, AudioType> _extensionToType = new Dictionary<string, AudioType>(StringComparer.OrdinalIgnoreCase)
        {
            { ".wav", AudioType.WAV },
            { ".ogg", AudioType.OGGVORBIS },
            { ".mp3", AudioType.MPEG },
            { ".aiff", AudioType.AIFF },
            { ".aif", AudioType.AIFF }
        };

        private static readonly string[] _extensions = new[] { ".wav", ".ogg", ".mp3", ".aiff", ".aif" };

        public bool SupportsExtension(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext))
                return false;

            if (!ext.StartsWith('.'))
                ext = "." + ext;

            return _extensionToType.ContainsKey(ext.ToLowerInvariant());
        }

        public UnityEngine.Object LoadFromFile(string absolutePath, Type targetType)
        {
            using var _ = JG.GameContent.Diagnostics.LoadProfiler.Measure(JG.GameContent.Diagnostics.LoadProfiler.AudioDecode);

            if (string.IsNullOrWhiteSpace(absolutePath))
                throw new ArgumentNullException(nameof(absolutePath));

            if (!File.Exists(absolutePath))
                throw new FileNotFoundException($"Audio file not found at '{absolutePath}'.", absolutePath);

            var ext = Path.GetExtension(absolutePath)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !_extensionToType.TryGetValue(ext, out var audioType))
                throw new NotSupportedException($"Unsupported audio extension '{ext}' for {absolutePath}.");

#if UNITY_WEBGL && !UNITY_EDITOR
            // Single-threaded: a blocking wait on a web request never completes.
            // ModsVfsPreload.jspre decodes each audio file up front and leaves raw PCM next to it.
            return LoadFromPcmSidecar(absolutePath);
#else
            var uri = new Uri(absolutePath);
            using var request = UnityWebRequestMultimedia.GetAudioClip(uri.AbsoluteUri, audioType);
            var downloadHandler = (DownloadHandlerAudioClip)request.downloadHandler;
            downloadHandler.streamAudio = false;

            var asyncOp = request.SendWebRequest();
            // SpinWait instead of Sleep(1): OS sleep granularity (1-15ms) added latency
            // per clip while the download/decode runs on Unity's worker threads.
            while (!asyncOp.isDone)
            {
                Thread.SpinWait(64);
            }

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                throw new Exception($"Failed to load audio clip '{absolutePath}': {request.error}");
            }

            var clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
                throw new Exception($"Audio decode returned null for '{absolutePath}'.");

            clip.name = Path.GetFileNameWithoutExtension(absolutePath);
            return clip;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private const int PcmHeaderBytes = 12;

        /// Layout written by ModsVfsPreload.jspre: int32 channels, int32 sampleRate, int32 frames,
        /// then interleaved float32 samples (little endian).
        private static AudioClip LoadFromPcmSidecar(string absolutePath)
        {
            var pcmPath = absolutePath + ".pcm";
            if (!File.Exists(pcmPath))
                throw new FileNotFoundException($"No decoded PCM for '{absolutePath}'. The browser could not decode this audio format.", pcmPath);

            var bytes = File.ReadAllBytes(pcmPath);
            if (bytes.Length < PcmHeaderBytes)
                throw new Exception($"PCM sidecar for '{absolutePath}' is truncated.");

            int channels = BitConverter.ToInt32(bytes, 0);
            int sampleRate = BitConverter.ToInt32(bytes, 4);
            int frames = BitConverter.ToInt32(bytes, 8);
            int sampleCount = frames * channels;
            if (channels <= 0 || frames <= 0 || bytes.Length < PcmHeaderBytes + sampleCount * sizeof(float))
                throw new Exception($"PCM sidecar for '{absolutePath}' has an invalid header.");

            var samples = new float[sampleCount];
            Buffer.BlockCopy(bytes, PcmHeaderBytes, samples, 0, sampleCount * sizeof(float));

            var clip = AudioClip.Create(Path.GetFileNameWithoutExtension(absolutePath), frames, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
#endif

        public UnityEngine.Object LoadFromResources(string resourcesPathNoExt, Type targetType)
        {
            return Resources.Load<AudioClip>(resourcesPathNoExt);
        }

        public AssetResolverDescriptor Describe()
        {
            return new AssetResolverDescriptor(
                id: "audio",
                displayName: "Sound Effect",
                extensions: _extensions,
                previewKind: "audio",
                supportedTypes: new[] { typeof(AudioClip) }
            );
        }
    }
}
