# Enable Blender's bundled add-ons we use (Rigify auto-rigger, Node Wrangler) and save the preferences
import bpy, addon_utils
for m in ("rigify", "node_wrangler"):
    try:
        addon_utils.enable(m, default_set=True, persistent=True); print("ENABLED", m)
    except Exception as e:
        print("FAILED", m, e)
bpy.ops.wm.save_userpref()
print("ADDONS_ON", sorted(bpy.context.preferences.addons.keys()))
