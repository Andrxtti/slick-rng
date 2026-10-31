// The career save also lives in localStorage on the web: it is per-site (not per-URL like Unity's IndexedDB
// store), synchronous, and far more reliable on iPhone Safari than IndexedDB syncs.
mergeInto(LibraryManager.library, {
  DreamStoreSet: function (key, val) {
    try { localStorage.setItem(UTF8ToString(key), UTF8ToString(val)); return 1; } catch (e) { return 0; }
  },
  DreamStoreGet: function (key) {
    var v = null;
    try { v = localStorage.getItem(UTF8ToString(key)); } catch (e) { v = null; }
    if (v === null) v = '';
    var n = lengthBytesUTF8(v) + 1, p = _malloc(n); stringToUTF8(v, p, n); return p;
  },
  DreamStoreDel: function (key) {
    try { localStorage.removeItem(UTF8ToString(key)); } catch (e) {}
  }
});
