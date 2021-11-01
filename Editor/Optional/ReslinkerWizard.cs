using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Cocos2Unity
{
    public class ReslinkerWizard : ScriptableWizard
    {
        [MenuItem("ToUnity2D/Reslinker")]
        static void CreateWizard()
        {
            var wzd = ScriptableWizard.DisplayWizard<Csd2UnityPrefab>("Link Res Used in Project JSON", "Create", "Apply");
        }

        void OnWizardCreate()
        {
            GenerateReslink();
        }

        void OnWizardUpdate()
        {
            helpString = @"-------------------------------";
        }

        // When the user presses the "Apply" button OnWizardOtherButton is called.
        void OnWizardOtherButton()
        {
            GenerateReslink();
        }

        public Dictionary<string, string> FindExtension = new Dictionary<string, string>
        {
            {".mp3", ".mp3"},
            {".csb", ".prefab"},
            {".png", ".png"},
            {".plist", ".png"},
        };

        void GenerateReslink()
        {

        }
    }
}
