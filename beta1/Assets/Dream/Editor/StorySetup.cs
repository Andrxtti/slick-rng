// The Yarn project for the story (Resources/Story/Dream.yarnproject, compiling every .yarn file next to it), made with
// Yarn Spinner's own utility so the file format is right. Run by "setup" and the remote command "yarn-project".
using System.IO;
using UnityEditor;
using Yarn.Unity.Editor;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class StorySetup
    {
        public const string Path = "Assets/Dream/Resources/Story/Dream.yarnproject";
        static StorySetup() { Remote.Extra["yarn-project"] = _ => Make(); }

        public static void Make()
        {
            if (File.Exists(Path)) { AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate); Remote.Log("yarn-project: reimported"); return; }
            YarnEditorUtility.CreateYarnProject(Path, new Yarn.Compiler.Project());   // the default project compiles every .yarn file under its folder
            Remote.Log("yarn-project: created " + Path);
        }
    }
}
