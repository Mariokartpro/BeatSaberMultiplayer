﻿using BeatSaberMultiplayer.Data;
using BeatSaberMultiplayer.Misc;
using BeatSaberMultiplayer.UI.UIElements;
using CustomUI.BeatSaber;
using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaberMultiplayer.UI.ViewControllers.RoomScreen
{
    public interface IPlayerManagementButtons
    {
        void MuteButtonWasPressed(PlayerInfo player);
        void TransferHostButtonWasPressed(PlayerInfo player);
    }

    class PlayerManagementViewController : VRUIViewController, TableView.IDataSource, IPlayerManagementButtons
    {
        public event Action gameplayModifiersChanged;
        public event Action<PlayerInfo> transferHostButtonPressed;

        public GameplayModifiers modifiers { get {
                if (_modifiersPanel.gameplayModifiers == null)
                {
                    Plugin.log.Error("Modifiers were null! Returning default modifiers.");
                    return GameplayModifiers.defaultModifiers;
                }                
                return _modifiersPanel.gameplayModifiers;
            } }

        RectTransform _playersTab;
        RectTransform _modifiersTab;

        TextSegmentedControl _tabControl;

        GameplayModifiersPanelController _modifiersPanel;
        RectTransform _modifiersPanelBlocker;

        Button _pageUpButton;
        Button _pageDownButton;

        TableView _playersTableView;

        LeaderboardTableCell _downloadListTableCellInstance;
        List<PlayerListTableCell> _tableCells = new List<PlayerListTableCell>();
        TableViewScroller _playersTableViewScroller;

        TextMeshProUGUI _pingText;

        bool _resetPlayerList;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _downloadListTableCellInstance = Resources.FindObjectsOfTypeAll<LeaderboardTableCell>().First();
                
                _tabControl = BeatSaberUI.CreateTextSegmentedControl(rectTransform, new Vector2(0f, 31f), new Vector2(100f, 7f), _tabControl_didSelectCellEvent);
                _tabControl.SetTexts(new string[] { "Players", "Modifiers" });

                #region Modifiers tab

                _modifiersTab = new GameObject("ModifiersTab", typeof(RectTransform)).GetComponent<RectTransform>();
                _modifiersTab.SetParent(rectTransform, false);
                _modifiersTab.anchorMin = new Vector2(0f, 0f);
                _modifiersTab.anchorMax = new Vector2(1f, 1f);
                _modifiersTab.anchoredPosition = new Vector2(0f, 0f);
                _modifiersTab.sizeDelta = new Vector2(0f, 0f);

                _modifiersPanel = Instantiate(Resources.FindObjectsOfTypeAll<GameplayModifiersPanelController>().First(), rectTransform, false);
                _modifiersPanel.SetData(GameplayModifiers.defaultModifiers);
                _modifiersPanel.gameObject.SetActive(true);
                _modifiersPanel.transform.SetParent(_modifiersTab, false);
                (_modifiersPanel.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_modifiersPanel.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_modifiersPanel.transform as RectTransform).anchoredPosition = new Vector2(0f, -23f);
                (_modifiersPanel.transform as RectTransform).sizeDelta = new Vector2(120f, -23f);

                HoverHintController hoverHintController = Resources.FindObjectsOfTypeAll<HoverHintController>().First();

                foreach (var hint in _modifiersPanel.GetComponentsInChildren<HoverHint>())
                {
                    hint.SetPrivateField("_hoverHintController", hoverHintController);
                }
                _modifiersPanel.Awake();

                var modifierToggles = _modifiersPanel.GetPrivateField<GameplayModifierToggle[]>("_gameplayModifierToggles");

                foreach (var item in modifierToggles)
                {
                    item.toggle.onValueChanged.AddListener( (enabled) => {
                        Plugin.log.Info("Toggle changed");
                        gameplayModifiersChanged?.Invoke();
                    });
                }

                _modifiersPanelBlocker = new GameObject("ModifiersPanelBlocker", typeof(RectTransform)).GetComponent<RectTransform>(); //"If it works it's not stupid"
                _modifiersPanelBlocker.SetParent(_modifiersTab, false);
                _modifiersPanelBlocker.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0f);
                _modifiersPanelBlocker.anchorMin = new Vector2(0f, 0f);
                _modifiersPanelBlocker.anchorMax = new Vector2(1f, 0f);
                _modifiersPanelBlocker.pivot = new Vector2(0.5f, 0f);
                _modifiersPanelBlocker.sizeDelta = new Vector2(-10f, 62f);
                _modifiersPanelBlocker.anchoredPosition = new Vector2(0f, 0f);
               
                #endregion

                #region Players tab

                _playersTab = new GameObject("PlayersTab", typeof(RectTransform)).GetComponent<RectTransform>();
                _playersTab.SetParent(rectTransform, false);
                _playersTab.anchorMin = new Vector2(0f, 0f);
                _playersTab.anchorMax = new Vector2(1f, 1f);
                _playersTab.anchoredPosition = new Vector2(0f, 0f);
                _playersTab.sizeDelta = new Vector2(0f, 0f);

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "PageUpButton")), _playersTab, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -18.5f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 6f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _playersTableViewScroller.PageScrollUp();

                });
                _pageUpButton.interactable = false;

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), _playersTab, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 7f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 6f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _playersTableViewScroller.PageScrollDown();

                });
                _pageDownButton.interactable = false;

                RectTransform container = new GameObject("Content", typeof(RectTransform)).transform as RectTransform;
                container.SetParent(_playersTab, false);
                container.anchorMin = new Vector2(0.15f, 0.5f);
                container.anchorMax = new Vector2(0.85f, 0.5f);
                container.sizeDelta = new Vector2(0f, 49f);
                container.anchoredPosition = new Vector2(0f, -3f);

                var tableGameObject = new GameObject("CustomTableView");
                tableGameObject.SetActive(false);
                _playersTableView = tableGameObject.AddComponent<TableView>();
                _playersTableView.gameObject.AddComponent<RectMask2D>();
                _playersTableView.transform.SetParent(container, false);

                _playersTableView.SetPrivateField("_isInitialized", false);
                _playersTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
                _playersTableView.SetPrivateField("_tableType", TableView.TableType.Vertical);

                var viewport = new GameObject("Viewport").AddComponent<RectTransform>();
                viewport.SetParent(tableGameObject.GetComponent<RectTransform>(), false);
                (viewport.transform as RectTransform).sizeDelta = new Vector2(0f, 0f);
                (viewport.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                (viewport.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                tableGameObject.GetComponent<ScrollRect>().viewport = viewport;
                _playersTableView.Init();

                (_playersTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                (_playersTableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_playersTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 0f);
                (_playersTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, 0f);

                ReflectionUtil.SetPrivateField(_playersTableView, "_pageUpButton", _pageUpButton);
                ReflectionUtil.SetPrivateField(_playersTableView, "_pageDownButton", _pageDownButton);
                tableGameObject.SetActive(true);
                _playersTableViewScroller = _playersTableView.GetPrivateField<TableViewScroller>("_scroller");
                //_playersTableViewScroller.SetPrivateField("position", zero);
                _playersTableView.dataSource = this;
                #endregion

                _pingText = this.CreateText("PING: 0", new Vector2(75f, 22.5f));
                _pingText.alignment = TextAlignmentOptions.Left;

                _tabControl_didSelectCellEvent(0);
            }
            else
            {
                for(int i = 0; i < _tableCells.Count; i++)
                {
                    Destroy(_tableCells[i].gameObject);
                }
                _tableCells.Clear();
                _playersTableView.ReloadData();
            }

            if (activationType == ActivationType.AddedToHierarchy)
            {
                StartCoroutine(ScrollWithDelay());
                SetGameplayModifiers(GameplayModifiers.defaultModifiers);
            }
        }

        private void _tabControl_didSelectCellEvent(int selectedIndex)
        {
            _playersTab.gameObject.SetActive(selectedIndex == 0);
            _modifiersTab.gameObject.SetActive(selectedIndex == 1);
        }

        public void Update()
        {
            if(Time.frameCount % 45 == 0 && _pingText != null && Client.Instance.networkClient != null && Client.Instance.networkClient.Connections.Count > 0)
                _pingText.text = "PING: "+ Math.Round(Client.Instance.networkClient.Connections[0].AverageRoundtripTime*1000, 2).ToString();
        }

        public void UpdateViewController(bool isHost, bool modifiersInteractable)
        {
            _modifiersPanelBlocker.gameObject.SetActive(!isHost || !modifiersInteractable);
        }

        public void UpdatePlayerList(RoomState state)
        {
            var players = InGameOnlineController.Instance.players;
                
            if (players.Count != _tableCells.Count || _resetPlayerList)
            {
                for (int i = 0; i < _tableCells.Count; i++)
                {
                    Destroy(_tableCells[i].gameObject);
                }
                _tableCells.Clear();
                //_playersTableView.RefreshTable(false);
                _playersTableView.ReloadData();
                //if(prevCount == 0 && _playersList.Count > 0)
                //{
                //    StartCoroutine(ScrollWithDelay());
                //}
                StartCoroutine(ScrollWithDelay());
                _resetPlayerList = false;
            }

            PlayerListTableCell buffer;
            int index = 0;
            foreach (var player in players)
            {
                if (player.Value == null)
                {
                    _resetPlayerList = true;
                    continue;
                }
                if (_tableCells.Count > index)
                {
                    buffer = _tableCells[index];
                    buffer.playerName = player.Value.playerInfo.playerName;
                    buffer.playerInfo = player.Value.playerInfo;
                    if (state == RoomState.Preparing)
                    {
                        if (player.Value.playerInfo.updateInfo.playerState == PlayerState.DownloadingSongs)
                        {
                            buffer.progress = player.Value.playerInfo.updateInfo.playerProgress / 100f;
                        }
                        else
                        {
                            buffer.progress = 1f;
                        }
                    }
                    else
                    {
                        buffer.progress = -1f;
                    }
                    buffer.IsTalking = InGameOnlineController.Instance.VoiceChatIsTalking(player.Key);
                    buffer.NameColor = player.Value.playerInfo.updateInfo.playerNameColor;
                    buffer.buttonsInterface = this;
                    buffer.Update();
                }
                index++;
            }
        }

        IEnumerator ScrollWithDelay()
        {
            yield return null;
            yield return null;

            _playersTableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
        }

        public void SetGameplayModifiers(GameplayModifiers modifiers)
        {
            if (_modifiersPanel != null)
            {
                _modifiersPanel.SetData(modifiers);
                _modifiersPanel.Refresh();
                /*
                GameplayModifiersModelSO modifiersModel = Resources.FindObjectsOfTypeAll<GameplayModifiersModelSO>().First();

                var modifiersParams = modifiersModel.GetModifierParams(modifiers);

                foreach (GameplayModifierToggle gameplayModifierToggle in _modifiersPanel.GetPrivateField<GameplayModifierToggle[]>("_gameplayModifierToggles"))
                {
                    gameplayModifierToggle.toggle.isOn = modifiersParams.Contains(gameplayModifierToggle.gameplayModifier);
                }
                */
            }
        }

        public float CellSize()
        {
            return 7f;
        }

        public int NumberOfCells()
        {
            return InGameOnlineController.Instance.players.Count;
        }

        public TableCell CellForIdx(TableView tableView, int row)
        {
            LeaderboardTableCell _originalCell = Instantiate(_downloadListTableCellInstance);

            PlayerListTableCell _tableCell = _originalCell.gameObject.AddComponent<PlayerListTableCell>();

            _tableCell.Init();

            _tableCell.rank = 0;
            _tableCell.showFullCombo = false;
            _tableCell.playerName = string.Empty;
            _tableCell.progress = 0f;
            _tableCell.IsTalking = false;
            _tableCell.NameColor = new Color32(255,255,255,255);
            _tableCell.playerInfo = null;
            _tableCell.buttonsInterface = this;
            _tableCell.Update();

            _tableCells.Add(_tableCell);
            return _tableCell;
        }

        public void MuteButtonWasPressed(PlayerInfo player)
        {
            if (player == null)
                return;

            if (InGameOnlineController.Instance.mutedPlayers.Contains(player.playerId))
            {
                InGameOnlineController.Instance.mutedPlayers.Remove(player.playerId);
            }
            else
            {
                InGameOnlineController.Instance.mutedPlayers.Add(player.playerId);
            }
        }

        public void TransferHostButtonWasPressed(PlayerInfo player)
        {
            if (player == null)
                return;

            if (Client.Instance.connected && Client.Instance.isHost)
            {
                transferHostButtonPressed?.Invoke(player);
            }
        }
    }
}
