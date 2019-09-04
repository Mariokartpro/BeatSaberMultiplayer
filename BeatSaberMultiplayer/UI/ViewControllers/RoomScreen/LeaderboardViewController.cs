﻿using BeatSaberMultiplayer.Data;
using BeatSaberMultiplayer.Misc;
using BeatSaberMultiplayer.UI;
using CustomUI.BeatSaber;
using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaberMultiplayer
{
    class LeaderboardViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action playNowButtonPressed;

        private TableView _leaderboardTableView;
        private TableViewScroller _leaderboardTableViewScroller;
        private LeaderboardTableCell _leaderboardTableCellInstance;
        private TextMeshProUGUI _timerText;
        private TextMeshProUGUI _progressText;
        private Button _pageUpButton;
        private Button _pageDownButton;
        private Button _playNowButton;
        public IPreviewBeatmapLevel _selectedSong;

        LevelListTableCell _songTableCell;

        List<LeaderboardTableCell> _tableCells = new List<LeaderboardTableCell>();

        int _lastTime;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                _songTableCell = Instantiate(Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell")), rectTransform);
                (_songTableCell.transform as RectTransform).anchoredPosition = new Vector2(100f, -1.5f);
                _songTableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
                _songTableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
                _songTableCell.SetPrivateField("_bought", true);
                foreach (var icon in _songTableCell.GetComponentsInChildren<UnityEngine.UI.Image>().Where(x => x.name.StartsWith("LevelTypeIcon")))
                {
                    Destroy(icon.gameObject);
                }

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -12.5f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 6f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _leaderboardTableViewScroller.PageScrollUp();
                    _leaderboardTableView.RefreshScrollButtons();

                });
                _pageUpButton.interactable = false;

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 7f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 6f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _leaderboardTableViewScroller.PageScrollDown();
                    _leaderboardTableView.RefreshScrollButtons();
                });
                _pageDownButton.interactable = false;

                _playNowButton = this.CreateUIButton("CancelButton", new Vector2(-39f, 34.5f), new Vector2(28f, 8.8f), () => { playNowButtonPressed?.Invoke(); }, "Play now");
                _playNowButton.ToggleWordWrapping(false);
                _progressText = BeatSaberUI.CreateText(rectTransform, "0.0%", new Vector2(8f, 32f));
                _progressText.gameObject.SetActive(false);

                _leaderboardTableCellInstance = Resources.FindObjectsOfTypeAll<LeaderboardTableCell>().First(x => (x.name == "LeaderboardTableCell"));

                RectTransform container = new GameObject("Content", typeof(RectTransform)).transform as RectTransform;
                container.SetParent(rectTransform, false);
                container.anchorMin = new Vector2(0.15f, 0.5f);
                container.anchorMax = new Vector2(0.85f, 0.5f);
                container.sizeDelta = new Vector2(0f, 56f);
                container.anchoredPosition = new Vector2(0f, -3f);

                var tableGameObject = new GameObject("CustomTableView");
                tableGameObject.SetActive(false);
                _leaderboardTableView = tableGameObject.AddComponent<TableView>();
                _leaderboardTableView.gameObject.AddComponent<RectMask2D>();
                _leaderboardTableView.transform.SetParent(container, false);
                (_leaderboardTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                (_leaderboardTableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_leaderboardTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 0f);
                (_leaderboardTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, 0f);

                _leaderboardTableView.SetPrivateField("_isInitialized", false);
                _leaderboardTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);

                RectTransform viewport = new GameObject("Viewport").AddComponent<RectTransform>(); //Make a Viewport RectTransform
                viewport.SetParent(_leaderboardTableView.transform as RectTransform, false); //It expects one from a ScrollRect, so we have to make one ourselves.
                viewport.sizeDelta = new Vector2(0f, 56f);

                _leaderboardTableView.Init();
                _leaderboardTableView.SetPrivateField("_scrollRectTransform", viewport);

                RectMask2D viewportMask = Instantiate(Resources.FindObjectsOfTypeAll<RectMask2D>().First(x => x.name != "CustomTableView"), _leaderboardTableView.transform, false);
                viewportMask.transform.DetachChildren();
                _leaderboardTableView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Content").transform.SetParent(viewportMask.rectTransform, false);               

                ReflectionUtil.SetPrivateField(_leaderboardTableView, "_pageUpButton", _pageUpButton);
                ReflectionUtil.SetPrivateField(_leaderboardTableView, "_pageDownButton", _pageDownButton);

                _leaderboardTableView.dataSource = this;
                tableGameObject.SetActive(true);
                _leaderboardTableViewScroller = _leaderboardTableView.GetPrivateField<TableViewScroller>("_scroller");

                _timerText = BeatSaberUI.CreateText(rectTransform, "", new Vector2(0f, 34f));
                _timerText.fontSize = 8f;
                _timerText.alignment = TextAlignmentOptions.Center;
                _timerText.rectTransform.sizeDelta = new Vector2(30f, 6f);
            }

        }

        public void SetLeaderboard()
        {

            var scores = InGameOnlineController.Instance.playerScores;

            if (scores == null)
                return;
            
            if (scores.Count != _tableCells.Count)
            {
                for (int i = 0; i < _tableCells.Count; i++)
                {
                    Destroy(_tableCells[i].gameObject);
                }
                _tableCells.Clear();
                _leaderboardTableView.RefreshTable(false);
                _leaderboardTableView.RefreshScrollButtons();
                //if (prevCount == 0 && _playerInfos.Count > 0)
                //{
                //    StartCoroutine(ScrollWithDelay());
                //}
            }

            for (int i = 0; i < scores.Count; i++)
            {
                if (_tableCells.Count > i)
                {
                    _tableCells[i].playerName = scores[i].name;
                    _tableCells[i].score = (int)scores[i].score;
                    _tableCells[i].rank = i + 1;
                    _tableCells[i].showFullCombo = false;
                }
            }
        }

        IEnumerator ScrollWithDelay()
        {
            yield return null;
            yield return null;

            _leaderboardTableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
        }

        public void SetSong(SongInfo info)
        {
            if (_songTableCell == null)
                return;
            
            _selectedSong = SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).FirstOrDefault(x => x.levelID.StartsWith(info.levelId));

            if (_selectedSong != null)
            {
                _songTableCell.SetText(_selectedSong.songName + " <size=80%>" + _selectedSong.songSubName + "</size>");
                _songTableCell.SetSubText(_selectedSong.songAuthorName + " <size=80%>[" + _selectedSong.levelAuthorName + "]</size>");

                _selectedSong.GetCoverImageTexture2DAsync(new CancellationTokenSource().Token).ContinueWith((tex) =>
                {
                    if (!tex.IsFaulted)
                        _songTableCell.SetIcon(tex.Result);
                }).ConfigureAwait(false);
            }
            else
            {
                _songTableCell.SetText(info.songName);
                _songTableCell.SetSubText("Loading info...");
                SongDownloader.Instance.RequestSongByLevelID(info.hash, (song) =>
                {
                    _songTableCell.SetText( $"{song.songName} <size=80%>{song.songSubName}</size>");
                    _songTableCell.SetSubText(song.songAuthorName + " <size=80%>[" + song.levelAuthorName + "]</size>");
                    StartCoroutine(LoadScripts.LoadSpriteCoroutine(song.coverURL, (cover) => { _songTableCell.SetIcon(cover); }));

                });
            }
        }
        
        public void SetProgressBarState(bool enabled, float progress)
        {
            _progressText.gameObject.SetActive(enabled);
            _progressText.text = progress.ToString("P");
            _playNowButton.interactable = !enabled;
        }

        public void SetTimer(int time, bool results)
        {
            if (_timerText == null || _playNowButton == null)
                return;

            if (time != _lastTime)
            {

                if (results)
                {
                    _timerText.text = "RESULTS " + time.ToString();
                }
                else
                {
                    _timerText.text = EssentialHelpers.MinSecDurationText(time);
                }

                _playNowButton.interactable = time > 15f;
            }
            _lastTime = time;
        }

        public TableCell CellForIdx(TableView tableView, int row)
        {
            LeaderboardTableCell cell = Instantiate(_leaderboardTableCellInstance);

            cell.reuseIdentifier = "ResultsCell";

            cell.playerName = InGameOnlineController.Instance.playerScores[row].name;
            cell.score = (int)InGameOnlineController.Instance.playerScores[row].score;
            cell.rank = row + 1;
            cell.showFullCombo = false;

            _tableCells.Add(cell);
            return cell;
        }

        public int NumberOfCells()
        {
            return InGameOnlineController.Instance.playerScores.Count;
        }

        public float CellSize()
        {
            return 7f;
        }
    }
}
