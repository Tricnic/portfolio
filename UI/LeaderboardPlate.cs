using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPlate : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI _rankText = default;
    [SerializeField] private Image _rankImage = default;
    [SerializeField] private TMPro.TextMeshProUGUI _playerNameText = default;
    [SerializeField] private TMPro.TextMeshProUGUI _statText = default;
    [SerializeField] private Sprite _rank1Sprite = default;
    [SerializeField] private Sprite _rank2Sprite = default;
    [SerializeField] private Sprite _rank3Sprite = default;
    [SerializeField] private GameObject _localPlayerOverlay = default;

    public void Populate(PlayerLeaderboardEntry leaderboardEntry, bool isLocalPlayer = false)
    {
        _playerNameText.text = leaderboardEntry.DisplayName;
        _statText.text = leaderboardEntry.StatValue.ToString();
        SetRankDisplay(leaderboardEntry.Position + 1);
        _localPlayerOverlay.SetActive(isLocalPlayer);
    }

    public void PopulateNoStat(string playerName, bool isLocalPlayer = false)
    {
        _rankText.text = string.Empty;
        _rankImage.gameObject.SetActive(false);
        _playerNameText.text = playerName;
        _statText.text = "0";
        _localPlayerOverlay.SetActive(isLocalPlayer);
    }

    private void SetRankDisplay(int rank)
    {
        bool top3Rank = false;
        if (rank == 1)
        {
            top3Rank = true;
            _rankImage.overrideSprite = _rank1Sprite;
        }
        else if (rank == 2)
        {
            top3Rank = true;
            _rankImage.overrideSprite = _rank2Sprite;
        }
        else if (rank == 3)
        {
            top3Rank = true;
            _rankImage.overrideSprite = _rank3Sprite;
        }
        else
        {
            _rankText.text = rank.ToString();
        }

        _rankImage.gameObject.SetActive(top3Rank);
        _rankText.gameObject.SetActive(!top3Rank);
    }
}
