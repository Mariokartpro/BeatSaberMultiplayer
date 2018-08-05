﻿using BeatSaberMultiplayer.Misc;
using BeatSaberMultiplayer.UI;
using IllusionPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;

namespace BeatSaberMultiplayer
{
    public class Plugin : IPlugin
    {
        public string Name => "Beat Saber Multiplayer";

        public string Version => "0.5.0.0";
        public static uint pluginVersion = 500;

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
            Log.Info("Loading scene " + nextScene.name);
            if (nextScene.name == "Menu")
            {
                BeatSaberUI.OnLoad();
                PluginUI.OnLoad();
                //BSMultiplayerClient.OnLoad(level, Version);
            }
            else if(nextScene.name == "StandardLevel")
            {
                Log.Info("Initializing online...");
                //BSMultiplayerClient.OnLoad(level, Version);
            }
        }

        public void OnApplicationQuit()
        {
        }

        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Config.Instance.Save();
            Base64Sprites.ConvertSprites();
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnUpdate()
        {
        }
    }
}
