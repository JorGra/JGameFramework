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
            if (string.IsNullOrWhiteSpace(absolutePath))
                throw new ArgumentNullException(nameof(absolutePath));

            if (!File.Exists(absolutePath))
                throw new FileNotFoundException($"Audio file not found at '{absolutePath}'.", absolutePath);

            var ext = Path.GetExtension(absolutePath)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !_extensionToType.TryGetValue(ext, out var audioType))
                throw new NotSupportedException($"Unsupported audio extension '{ext}' for {absolutePath}.");

            var uri = new Uri(absolutePath);
            using var request = UnityWebRequestMultimedia.GetAudioClip(uri.AbsoluteUri, audioType);
            var downloadHandler = (DownloadHandlerAudioClip)request.downloadHandler;
            downloadHandler.streamAudio = false;

            var asyncOp = request.SendWebRequest();
            while (!asyncOp.isDone)
            {
                Thread.Sleep(1);
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
        }

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
