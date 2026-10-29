# Enable the bundled add-ons we use (Rigify, Node Wrangler) and save preferences so they stay on
import bpy, addon_utils
for m in ("rigify", "node_wrangler"):
    try:
        addon_utils.enable(m, default_set=True, persistent=True); print("ENABLED", m)
    except Exception as e:
        print("FAILED", m, e)
bpy.ops.wm.save_userpref(); print("PREFS SAVED")
