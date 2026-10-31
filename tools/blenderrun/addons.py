# List add-ons/extensions Blender can see and which are enabled (headless check after installs)
import addon_utils, bpy
mods = sorted(m.__name__ for m in addon_utils.modules())
on = sorted(bpy.context.preferences.addons.keys())
print("ADDONS_ALL", len(mods), [m for m in mods if any(k in m.lower() for k in ("rig", "mpfb", "node_wrangler", "fbx", "gltf", "arkit", "shak", "retarget", "mixamo", "poly", "surface", "bake", "lip", "proto", "shape", "expression"))])
print("ADDONS_ON", on)
