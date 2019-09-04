﻿using BeatSaberMultiplayer.Data;
using BeatSaberMultiplayer.Misc;
using BeatSaberMultiplayer.UI.FlowCoordinators;
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

namespace BeatSaberMultiplayer.UI.ViewControllers
{
    class RoomListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action createRoomButtonPressed;
        public event Action<ServerHubRoom> selectedRoom;
        public event Action refreshPressed;

        private Button _pageUpButton;
        private Button _pageDownButton;
        private Button _createRoom;
        private Button _refreshButton;

        TableView _serverTableView;
        LevelListTableCell _serverTableCellInstance;
        public TableViewScroller _serverTableViewScroller;
        GameObject tableGameObject;
        TextMeshProUGUI _noRoomsText;

        List<ServerHubRoom> availableRooms = new List<ServerHubRoom>();

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            try
            {
                if (firstActivation && type == ActivationType.AddedToHierarchy)
                {
                    _serverTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                    _refreshButton = this.CreateUIButton("PracticeButton", new Vector2(-25f, 36.5f), new Vector2(6.5f, 6.5f), () => { refreshPressed?.Invoke(); }, "", Sprites.refreshIcon);
                    var _refreshIconLayout = _refreshButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
                    _refreshIconLayout.padding = new RectOffset(0, 0, 1, 1);

                    _createRoom = BeatSaberUI.CreateUIButton(rectTransform, "CancelButton");
                    _createRoom.SetButtonText("Create room");
                    _createRoom.SetButtonTextSize(3f);
                    (_createRoom.transform as RectTransform).sizeDelta = new Vector2(38f, 6f);
                    (_createRoom.transform as RectTransform).anchoredPosition = new Vector2(0f, 36.5f);
                    _createRoom.onClick.RemoveAllListeners();
                    _createRoom.onClick.AddListener(delegate ()
                    {
                        createRoomButtonPressed?.Invoke();
                    });

                    _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "PageUpButton")), rectTransform, false);
                    (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                    (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                    (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -14.5f);
                    (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                    _pageUpButton.interactable = true;
                    _pageUpButton.onClick.AddListener(delegate ()
                    {
                        _serverTableViewScroller.PageScrollUp();
                        _serverTableView.RefreshScrollButtons();
                    });

                    _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                    (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                    (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                    (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 9f);
                    (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                    _pageDownButton.interactable = true;
                    _pageDownButton.onClick.AddListener(delegate ()
                    {
                        _serverTableViewScroller.PageScrollDown();
                        _serverTableView.RefreshScrollButtons();
                    });

                    RectTransform container = new GameObject("Content", typeof(RectTransform)).transform as RectTransform;
                    container.SetParent(rectTransform, false);
                    container.anchorMin = new Vector2(0.3f, 0.5f);
                    container.anchorMax = new Vector2(0.7f, 0.5f);
                    container.sizeDelta = new Vector2(0f, 40f);
                    container.anchoredPosition = new Vector2(0f, -3f);

                    tableGameObject = new GameObject("CustomTableView");
                    tableGameObject.SetActive(false);
                    _serverTableView = tableGameObject.AddComponent<TableView>();
                    _serverTableView.gameObject.AddComponent<RectMask2D>();
                    _serverTableView.transform.SetParent(container, false);
                    (_serverTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                    (_serverTableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                    (_serverTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 0f);
                    (_serverTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, 0f);

                    _serverTableView.SetPrivateField("_isInitialized", false);
                    _serverTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);

                    RectTransform viewport = new GameObject("Viewport").AddComponent<RectTransform>(); //Make a Viewport RectTransform
                    viewport.SetParent(_serverTableView.transform as RectTransform, false); //It expects one from a ScrollRect, so we have to make one ourselves.
                    viewport.sizeDelta = new Vector2(0f, 40f);

                    _serverTableView.Init();
                    _serverTableView.SetPrivateField("_scrollRectTransform", viewport);
                    tableGameObject.SetActive(true);
                    ReflectionUtil.SetPrivateField(_serverTableView, "_pageUpButton", _pageUpButton);
                    ReflectionUtil.SetPrivateField(_serverTableView, "_pageDownButton", _pageDownButton);

                    _serverTableView.Init();

                    tableGameObject.SetActive(true);
                    _serverTableView.dataSource = this;
                    //_serverTableView.SetPrivateField("_hideScrollButtonsIfNotNeeded", false);
                    _serverTableViewScroller = _serverTableView.GetPrivateField<TableViewScroller>("_scroller");
                    _serverTableView.didSelectCellWithIdxEvent += ServerTableView_DidSelectRow;

                    _noRoomsText = BeatSaberUI.CreateText(rectTransform, "No rooms available to join...", new Vector2(0f, 0f));
                    _noRoomsText.fontSize = 8f;
                    _noRoomsText.alignment = TextAlignmentOptions.Center;
                    _noRoomsText.rectTransform.sizeDelta = new Vector2(120f, 6f);
                    _noRoomsText.gameObject.SetActive(false);
                }
                else
                {
                    _serverTableView.ReloadData();
                }
            }
            catch (Exception e)
            {
                Plugin.log.Info(e.ToString());
            }

        }

        public void SetRooms(List<ServerHubRoom> rooms)
        {
            int prevCount = availableRooms.Count;
            if (rooms == null)
            {
                availableRooms.Clear();
            }
            else
            {
                availableRooms = rooms.OrderByDescending(y => y.roomInfo.players).ToList();
            }
            if (availableRooms.Count() > 0)
            {
                _noRoomsText.gameObject.SetActive(false);
            } else
            {
                _noRoomsText.gameObject.SetActive(true);
            }
            
            if (_serverTableView.dataSource != this)
            {
                _serverTableView.dataSource = this;
            }
            else
            {
                _serverTableView.ReloadData();
                if (prevCount == 0 && availableRooms.Count > 0)
                {
                    StartCoroutine(ScrollWithDelay());
                }
            }

        }

        IEnumerator ScrollWithDelay()
        {
            yield return null;
            yield return null;

            _serverTableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
            _serverTableView.RefreshScrollButtons();
        }

        public void SetRefreshButtonState(bool enabled)
        {
            _refreshButton.interactable = enabled;
        }

        private void ServerTableView_DidSelectRow(TableView sender, int row)
        {
            selectedRoom?.Invoke(availableRooms[row]);
        }

        public TableCell CellForIdx(TableView tableView, int row)
        {
            LevelListTableCell cell = Instantiate(_serverTableCellInstance);
            cell.reuseIdentifier = "ServerTableCell";

            RoomInfo room = availableRooms[row].roomInfo;
            
            if (room.usePassword)
            {
                cell.SetIcon(Sprites.lockedRoomIcon.texture);
            }
            else
            {
                cell.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).First(x => x.name == "CoverImage").enabled = false;
            }
            cell.SetText($"({room.players}/{((room.maxPlayers == 0)? "INF":room.maxPlayers.ToString())})" + room.name);
            cell.SetSubText($"{room.roomState.ToString()}");

            cell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
            cell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
            cell.SetPrivateField("_bought", true);
            foreach (var icon in cell.GetComponentsInChildren<UnityEngine.UI.Image>().Where(x => x.name.StartsWith("LevelTypeIcon")))
            {
                Destroy(icon.gameObject);
            }

            return cell;
        }

        public int NumberOfCells()
        {
            return availableRooms.Count;
        }

        public float CellSize()
        {
            return 10f;
        }

    }
}
