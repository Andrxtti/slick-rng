// B3 (freezing on iPhone after switching apps/tabs): tell the game when the page is hidden and shown again, and when
// the browser takes the WebGL context away (iOS does under memory pressure while in the background). A lost context
// can't come back in Unity, so the page reloads; the game saved on every change (Flow.Write), so it resumes there.
mergeInto(LibraryManager.library, {
  Dream_HookPage: function (goPtr) {
    var go = UTF8ToString(goPtr);
    var send = function (method) {
      console.log("[Dream] page " + method);
      try {
        if (typeof Module !== "undefined" && Module.SendMessage) Module.SendMessage(go, method, "");
        else SendMessage(go, method, "");
      } catch (e) { console.warn("[Dream] page hook: " + e); }
    };
    console.log("[Dream] page hook on " + go);
    document.addEventListener("visibilitychange", function () { send(document.hidden ? "OnPageHidden" : "OnPageShown"); });
    window.addEventListener("pageshow", function () { send("OnPageShown"); });
    window.addEventListener("focus", function () { send("OnPageShown"); });
    // capture phase on the document: catches the loss on whichever canvas Unity made
    document.addEventListener("webglcontextlost", function (e) {
      console.log("[Dream] WebGL context lost: reloading");
      e.preventDefault();
      send("OnPageHidden");
      setTimeout(function () { location.reload(); }, 250);
    }, true);
  },
});
