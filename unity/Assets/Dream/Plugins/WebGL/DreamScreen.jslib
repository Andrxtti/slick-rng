// The page's width in CSS pixels, so the game can size its UI like a website on big screens.
mergeInto(LibraryManager.library, {
  DreamCssWidth: function () {
    var c = (typeof Module !== 'undefined' && Module['canvas']) ? Module['canvas'] : null;
    var w = c && c.clientWidth ? c.clientWidth : (window.innerWidth || 0);
    return Math.round(w);
  }
});
