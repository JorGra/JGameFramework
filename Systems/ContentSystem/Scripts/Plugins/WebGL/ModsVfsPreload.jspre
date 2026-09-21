// Copies the Mods folder that sits beside index.html into the in-memory filesystem before the
// engine starts, so the mod loader reads /Mods with plain System.IO exactly like on desktop.
// The file list comes from Mods/mods-index.json, written by ModsFolderBuildPostprocessor.
// Set `modsUrl` in the template's createUnityInstance config to load mods from elsewhere.
if (typeof ENVIRONMENT_IS_PTHREAD === 'undefined' || !ENVIRONMENT_IS_PTHREAD) {
  if (!Module['preRun']) Module['preRun'] = [];
  Module['preRun'].push(function () {
    var RUN_DEPENDENCY = 'JG_ModsPreload';
    var VFS_ROOT = '/Mods';
    var PARALLEL_FETCHES = 8;
    var AUDIO_EXTENSIONS = /\.(wav|ogg|mp3|aiff?)$/i;

    var baseUrl = new URL((Module['modsUrl'] || 'Mods').replace(/\/+$/, '') + '/', document.URL).href;
    var startTime = performance.now();
    var totalBytes = 0;
    var audioContext = null;

    function fileUrl(rel, version) {
      return baseUrl + rel.split('/').map(encodeURIComponent).join('/') + '?v=' + encodeURIComponent(version);
    }

    function writeFile(rel, bytes) {
      var path = VFS_ROOT + '/' + rel;
      FS.mkdirTree(path.substring(0, path.lastIndexOf('/')));
      FS.writeFile(path, bytes, { canOwn: true });
    }

    // Unity cannot decode audio synchronously on the web, so SoundEffectResolver reads this
    // sidecar instead: int32 channels, int32 sampleRate, int32 frames, interleaved float32.
    function writePcmSidecar(rel, encoded) {
      var Ctx = window.OfflineAudioContext || window.webkitOfflineAudioContext;
      if (!Ctx) return Promise.resolve();
      if (!audioContext) audioContext = new Ctx(1, 1, 44100);

      return new Promise(function (resolve) {
        function failed(err) {
          console.warn('[ModsPreload] Could not decode audio ' + rel + ':', err);
          resolve();
        }

        function decoded(buffer) {
          var channels = buffer.numberOfChannels;
          var frames = buffer.length;
          var out = new ArrayBuffer(12 + frames * channels * 4);
          var header = new DataView(out);
          header.setInt32(0, channels, true);
          header.setInt32(4, buffer.sampleRate, true);
          header.setInt32(8, frames, true);

          var samples = new Float32Array(out, 12);
          for (var c = 0; c < channels; c++) {
            var data = buffer.getChannelData(c);
            for (var i = 0; i < frames; i++) samples[i * channels + c] = data[i];
          }

          writeFile(rel + '.pcm', new Uint8Array(out));
          resolve();
        }

        try {
          // decodeAudioData detaches its input, so hand it a copy.
          var pending = audioContext.decodeAudioData(encoded.slice(0), decoded, failed);
          if (pending && pending.catch) pending.catch(function () {});
        } catch (err) {
          failed(err);
        }
      });
    }

    function loadFile(rel, version) {
      return fetch(fileUrl(rel, version), { cache: 'no-store' })
        .then(function (response) {
          if (!response.ok) throw new Error('HTTP ' + response.status);
          return response.arrayBuffer();
        })
        .then(function (buffer) {
          totalBytes += buffer.byteLength;
          var sidecar = AUDIO_EXTENSIONS.test(rel) ? writePcmSidecar(rel, buffer) : Promise.resolve();
          return sidecar.then(function () { writeFile(rel, new Uint8Array(buffer)); });
        })
        .catch(function (err) {
          console.error('[ModsPreload] Failed to load ' + rel + ':', err);
        });
    }

    function loadAll(index) {
      var files = index.files || [];
      var next = 0;

      function worker() {
        if (next >= files.length) return Promise.resolve();
        return loadFile(files[next++], index.version).then(worker);
      }

      var workers = [];
      for (var i = 0; i < PARALLEL_FETCHES; i++) workers.push(worker());

      return Promise.all(workers).then(function () {
        console.log('[ModsPreload] ' + files.length + ' files, ' +
          (totalBytes / 1048576).toFixed(1) + ' MB in ' +
          ((performance.now() - startTime) / 1000).toFixed(1) + ' s -> ' + VFS_ROOT);
      });
    }

    Module.addRunDependency(RUN_DEPENDENCY);
    fetch(baseUrl + 'mods-index.json?v=' + Date.now(), { cache: 'no-store' })
      .then(function (response) {
        // Builds without a Mods folder (e.g. the VFX preview player) simply have no index.
        return response.ok ? response.json() : null;
      })
      .then(function (index) {
        if (index) return loadAll(index);
      })
      .catch(function (err) {
        console.error('[ModsPreload] Mod preload failed, starting without mods:', err);
      })
      .then(function () {
        Module.removeRunDependency(RUN_DEPENDENCY);
      });
  });
}
