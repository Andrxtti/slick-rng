// Points the UMA body at UMA's character prefab and a standing idle. Made by Dream > Set up 3D characters and kept
// in Resources/Dream/UMARefs so builds include it. (Its own file: Unity needs the file name to match the class.)
using UnityEngine;

namespace CFDream.Character
{
    public class DreamUMARefs : ScriptableObject
    {
        public GameObject avatar;                 // UMA's DynamicCharacterAvatar prefab
        public RuntimeAnimatorController idle;    // a standing idle, so the character never stands in a T-pose
        public string maleRace = "Human Male 3.0", femaleRace = "Human Female 3.0";
    }
}
