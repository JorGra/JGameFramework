mergeInto(LibraryManager.library, {
  JGVfx_RegisterMessageListener: function () {
    window.addEventListener('message', function (e) {
      var d = e.data;
      if (!d || d.type !== 'vfx:apply' || d.payload === undefined) return;
      var s = typeof d.payload === 'string' ? d.payload : JSON.stringify(d.payload);
      SendMessage('VfxPreview', 'ApplyPayload', s);
    });
    if (window.parent && window.parent !== window) {
      window.parent.postMessage({ type: 'vfx:ready' }, '*');
    }
  },

  JGVfx_PostStatus: function (msgPtr) {
    if (window.parent && window.parent !== window) {
      var payload;
      try {
        payload = JSON.parse(UTF8ToString(msgPtr));
      } catch (err) {
        payload = { ok: false, message: 'unparseable status' };
      }
      window.parent.postMessage({ type: 'vfx:status', payload: payload }, '*');
    }
  }
});
