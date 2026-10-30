// Hotfix (Steven 11:00/11:01: "the game crashed and cleared as soon as I clicked open Spacepad"): the career is also kept in
// localStorage, written synchronously on every save, so a crash or a reload in the middle of IndexedDB's asynchronous sync
// can never cost it. Save.Load reads both and keeps the newest valid one (Flow.cs, Save.rev).
mergeInto(LibraryManager.library, {
  Dream_SaveMirror: function (ptr) { try { localStorage.setItem("dream_beta1_save", UTF8ToString(ptr)); } catch (e) { console.warn("[Dream] save mirror: " + e); } },
  Dream_LoadMirror: function () {
    var s = ""; try { s = localStorage.getItem("dream_beta1_save") || ""; } catch (e) {}
    var n = lengthBytesUTF8(s) + 1, buf = _malloc(n); stringToUTF8(s, buf, n); return buf;
  },
  Dream_EraseMirror: function () {
    try { var s = localStorage.getItem("dream_beta1_save"); if (s) localStorage.setItem("dream_beta1_save_erased", s); localStorage.removeItem("dream_beta1_save"); } catch (e) {}
  },
});
