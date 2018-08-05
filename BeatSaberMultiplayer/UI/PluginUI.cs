﻿using BeatSaberMultiplayer.Misc;
using BeatSaberMultiplayer.UI.FlowCoordinators;
using BeatSaberMultiplayer.UI.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMultiplayer.UI
{
    class PluginUI : MonoBehaviour
    {
        public static PluginUI instance;
        
        private MainMenuViewController _mainMenuViewController;
        private RectTransform _mainMenuRectTransform;

        public ServerHubFlowCoordinator serverHubFlowCoordinator;
        public RoomCreationFlowCoordinator roomCreationFlowCoordinator;
        public RoomFlowCoordinator roomFlowCoordinator;

        public static void OnLoad()
        {
            if (instance != null)
            {
                return;
            }
            new GameObject("Multiplayer Plugin").AddComponent<PluginUI>();
        }

        public void Awake()
        {
            instance = this;
            GetUserInfo.UpdateUserInfo();
        }

        public void Start()
        {
            try
            {
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;

                if (serverHubFlowCoordinator == null)
                {
                    serverHubFlowCoordinator = new GameObject("ServerHubFlow").AddComponent<ServerHubFlowCoordinator>();
                    serverHubFlowCoordinator.mainMenuViewController = _mainMenuViewController;
                }
                if (roomCreationFlowCoordinator == null)
                {
                    roomCreationFlowCoordinator = new GameObject("RoomCreationFlow").AddComponent<RoomCreationFlowCoordinator>();
                }
                if (roomFlowCoordinator == null)
                {
                    roomFlowCoordinator = new GameObject("RoomFlow").AddComponent<RoomFlowCoordinator>();
                }

                CreateOnlineButton();
            }
            catch (Exception e)
            {
                Log.Exception($"EXCEPTION ON AWAKE(TRY CREATE BUTTON): {e}");
            }
        }

        private void CreateOnlineButton()
        {
            Button _multiplayerButton = BeatSaberUI.CreateUIButton(_mainMenuRectTransform, "PartyButton");
            _multiplayerButton.transform.SetParent(Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "SoloButton").transform.parent);

            BeatSaberUI.SetButtonText(_multiplayerButton, "Online");
            BeatSaberUI.SetButtonIcon(_multiplayerButton, Base64Sprites.onlineIcon);

            _multiplayerButton.onClick.AddListener(delegate ()
            {
                try
                {
                    serverHubFlowCoordinator.OnlineButtonPressed();
                }
                catch (Exception e)
                {
                    Log.Exception($"EXCETPION IN ONLINE BUTTON: {e}");
                }
            });
        }
    }
}
