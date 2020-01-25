using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WebServiceClient : BaseGameService
{
    public string serviceUrl = "http://localhost/tbrpg-php-service";
    public bool debug;
    public bool sendLoginTokenViaGetMethod;

#if UNITY_EDITOR
    [ContextMenu("Export Game Database")]
    public void ExportGameDatabase()
    {
        var gameInstance = FindObjectOfType<GameInstance>();
        if (gameInstance == null)
        {
            Debug.LogError("Cannot export game database, no game instance found");
            return;
        }
        var gameDatabase = gameInstance.gameDatabase;
        if (gameDatabase == null)
        {
            Debug.LogError("Cannot export game database, no game database found");
            return;
        }
        gameDatabase.Setup();
        var achievementsJson = "";
        var itemsJson = "";
        var currenciesJson = "";
        var staminasJson = "";
        var formationsJson = "";
        var stagesJson = "";
        var lootBoxesJson = "";
        var iapPackagesJson = "";
        var hardCurrencyConvertionsJson = "";
        var startItemsJson = "";
        var startCharactersJson = "";
        var unlockStagesJson = "";
        var arenaRanksJson = "";

        foreach (var achievement in gameDatabase.Achievements)
        {
            if (!string.IsNullOrEmpty(achievementsJson))
                achievementsJson += ",";
            achievementsJson += "\"" + achievement.Key + "\":" + achievement.Value.ToJson();
        }
        achievementsJson = "{" + achievementsJson + "}";

        foreach (var item in gameDatabase.Items)
        {
            if (!string.IsNullOrEmpty(itemsJson))
                itemsJson += ",";
            itemsJson += "\"" + item.Key + "\":" + item.Value.ToJson();
        }
        itemsJson = "{" + itemsJson + "}";

        currenciesJson = "{\"SOFT_CURRENCY\":" + gameDatabase.softCurrency.ToJson() + ", \"HARD_CURRENCY\":" + gameDatabase.hardCurrency.ToJson() + "}";
        staminasJson = "{\"STAGE\":" + gameDatabase.stageStamina.ToJson() + ", \"ARENA\":" + gameDatabase.arenaStamina.ToJson() + "}";

        foreach (var entry in gameDatabase.Formations)
        {
            if (!string.IsNullOrEmpty(formationsJson))
                formationsJson += ",";
            formationsJson += "\"" + entry.Key + "\"";
        }
        formationsJson = "[" + formationsJson + "]";

        foreach (var entry in gameDatabase.Stages)
        {
            if (!string.IsNullOrEmpty(stagesJson))
                stagesJson += ",";
            stagesJson += "\"" + entry.Key + "\":" + entry.Value.ToJson();
        }
        stagesJson = "{" + stagesJson + "}";

        foreach (var entry in gameDatabase.LootBoxes)
        {
            if (!string.IsNullOrEmpty(lootBoxesJson))
                lootBoxesJson += ",";
            lootBoxesJson += "\"" + entry.Key + "\":" + entry.Value.ToJson();
        }
        lootBoxesJson = "{" + lootBoxesJson + "}";

        foreach (var entry in gameDatabase.IapPackages)
        {
            if (!string.IsNullOrEmpty(iapPackagesJson))
                iapPackagesJson += ",";
            iapPackagesJson += "\"" + entry.Key + "\":" + entry.Value.ToJson();
        }
        iapPackagesJson = "{" + iapPackagesJson + "}";

        foreach (var entry in gameDatabase.hardCurrencyConvertions)
        {
            if (!string.IsNullOrEmpty(hardCurrencyConvertionsJson))
                hardCurrencyConvertionsJson += ",";
            hardCurrencyConvertionsJson += entry.ToJson();
        }
        hardCurrencyConvertionsJson = "[" + hardCurrencyConvertionsJson + "]";

        foreach (var entry in gameDatabase.startItems)
        {
            if (entry == null || entry.item == null)
                continue;
            if (!string.IsNullOrEmpty(startItemsJson))
                startItemsJson += ",";
            startItemsJson += entry.ToJson();
        }
        startItemsJson = "[" + startItemsJson + "]";

        foreach (var entry in gameDatabase.startCharacters)
        {
            if (entry == null)
                continue;
            if (!string.IsNullOrEmpty(startCharactersJson))
                startCharactersJson += ",";
            startCharactersJson += "\"" + entry.Id + "\"";
        }
        startCharactersJson = "[" + startCharactersJson + "]";

        foreach (var entry in gameDatabase.unlockStages)
        {
            if (entry == null)
                continue;
            if (!string.IsNullOrEmpty(unlockStagesJson))
                unlockStagesJson += ",";
            unlockStagesJson += "\"" + entry.Id + "\"";
        }
        unlockStagesJson = "[" + unlockStagesJson + "]";

        foreach (var entry in gameDatabase.arenaRanks)
        {
            if (entry == null)
                continue;
            if (!string.IsNullOrEmpty(arenaRanksJson))
                arenaRanksJson += ",";
            arenaRanksJson += entry.ToJson();
        }
        arenaRanksJson = "[" + arenaRanksJson + "]";

        var jsonCombined = "{" +
            "\"achievements\":" + achievementsJson + "," +
            "\"items\":" + itemsJson + "," +
            "\"currencies\":" + currenciesJson + "," +
            "\"staminas\":" + staminasJson + "," +
            "\"formations\":" + formationsJson + "," +
            "\"stages\":" + stagesJson + "," +
            "\"lootBoxes\":" + lootBoxesJson + "," +
            "\"iapPackages\":" + iapPackagesJson + "," +
            "\"hardCurrencyConvertions\":" + hardCurrencyConvertionsJson + "," +
            "\"startItems\":" + startItemsJson + "," +
            "\"startCharacters\":" + startCharactersJson + "," +
            "\"unlockStages\":" + unlockStagesJson + "," +
            "\"arenaRanks\":" + arenaRanksJson + "," +
            "\"arenaWinScoreIncrease\":" + gameDatabase.arenaWinScoreIncrease + "," +
            "\"arenaLoseScoreDecrease\":" + gameDatabase.arenaLoseScoreDecrease + "," +
            "\"playerMaxLevel\":" + gameDatabase.playerMaxLevel + "," +
            "\"playerExpTable\":" + gameDatabase.playerExpTable.ToJson() + "," +
            "\"revivePrice\":" + gameDatabase.revivePrice + "," +
            "\"resetItemLevelAfterEvolve\":" + (gameDatabase.resetItemLevelAfterEvolve ? 1 : 0) + "}";
        
        var path = EditorUtility.SaveFilePanel("Export Game Database", Application.dataPath, "GameData", "json");
        if (path.Length > 0)
            File.WriteAllText(path, jsonCombined);
    }
#endif

    public void Get(string path, System.Action<UnityWebRequest> onDone, string loginToken = "")
    {
        StartCoroutine(GetRoutine(path, onDone, loginToken));
    }

    public void GetAsDecodedJSON<T>(string path, System.Action<UnityWebRequest, T> onDone, string loginToken = "") where T : GameServiceResult, new()
    {
        StartCoroutine(GetRoutine(path, (www) =>
        {
            T result = new T();
            if (www.isHttpError)
                result.error = GameServiceErrorCode.UNKNOW;
            else if (www.isNetworkError)
                result.error = GameServiceErrorCode.NETWORK;
            else
                result = JsonConvert.DeserializeObject<T>(www.downloadHandler.text);

            onDone(www, result);
        }, loginToken));
    }

    public IEnumerator GetRoutine(string path, System.Action<UnityWebRequest> onDone, string loginToken = "")
    {
        if (sendLoginTokenViaGetMethod && !string.IsNullOrEmpty(loginToken))
        {
            if (path.Contains("?"))
                path += "&logintoken=" + loginToken;
            else
                path += "?logintoken=" + loginToken;
        }

        var www = UnityWebRequest.Get(serviceUrl + path);

        if (!sendLoginTokenViaGetMethod && !string.IsNullOrEmpty(loginToken))
        {
            www.SetRequestHeader("Authorization", "Bearer " + loginToken);
            if (debug)
                Debug.Log("[GET->Authorization] " + path + " " + loginToken);
        }
        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
            Debug.LogError("[GET->Error]" + path + " " + www.error + " " + www.downloadHandler.text);

        if (debug)
            Debug.Log("[GET->Result] " + path + " " + www.downloadHandler.text);

        if (onDone != null)
            onDone.Invoke(www);
    }

    public void Post(string path, System.Action<UnityWebRequest> onDone, string data = "{}", string loginToken = "")
    {
        StartCoroutine(PostRoutine(path, onDone, data, loginToken));
    }

    public void PostAsDecodedJSON<T>(string path, System.Action<UnityWebRequest, T> onDone, string data = "{}", string loginToken = "") where T : GameServiceResult, new()
    {
        StartCoroutine(PostRoutine(path, (www) =>
        {
            T result = new T();
            if (www.isHttpError)
                result.error = GameServiceErrorCode.UNKNOW;
            else if (www.isNetworkError)
                result.error = GameServiceErrorCode.NETWORK;
            else
                result = JsonConvert.DeserializeObject<T>(www.downloadHandler.text);

            onDone(www, result);
        }, data, loginToken));
    }

    public IEnumerator PostRoutine(string path, System.Action<UnityWebRequest> onDone, string data = "{}", string loginToken = "")
    {
        if (sendLoginTokenViaGetMethod && !string.IsNullOrEmpty(loginToken))
        {
            if (path.Contains("?"))
                path += "&logintoken=" + loginToken;
            else
                path += "?logintoken=" + loginToken;
        }

        var www = UnityWebRequest.Post(serviceUrl + path, data);

        if (!sendLoginTokenViaGetMethod && !string.IsNullOrEmpty(loginToken))
        {
            www.SetRequestHeader("Authorization", "Bearer " + loginToken);
            if (debug)
                Debug.Log("[POST->Authorization] " + path + " " + loginToken);
        }
        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        if (debug)
            Debug.Log("[POST->Data] " + path + " " + data);

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
            Debug.LogError("[POST->Error]" + path + " " + www.error + " " + www.downloadHandler.text);

        if (debug)
            Debug.Log("[POST->Result] " + path + " " + www.downloadHandler.text);

        if (onDone != null)
            onDone.Invoke(www);
    }

    #region Listing Services
    protected override void DoGetAchievementList(string playerId, string loginToken, UnityAction<AchievementListResult> onFinish)
    {
        GetAsDecodedJSON<AchievementListResult>("/achievements", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetAuthList(string playerId, string loginToken, UnityAction<AuthListResult> onFinish)
    {
        // TODO: This may not be used
        var result = new AuthListResult();
        onFinish(result);
    }

    protected override void DoGetItemList(string playerId, string loginToken, UnityAction<ItemListResult> onFinish)
    {
        GetAsDecodedJSON<ItemListResult>("/items", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetCurrencyList(string playerId, string loginToken, UnityAction<CurrencyListResult> onFinish)
    {
        GetAsDecodedJSON<CurrencyListResult>("/currencies", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetStaminaList(string playerId, string loginToken, UnityAction<StaminaListResult> onFinish)
    {
        GetAsDecodedJSON<StaminaListResult>("/staminas", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetFormationList(string playerId, string loginToken, UnityAction<FormationListResult> onFinish)
    {
        GetAsDecodedJSON<FormationListResult>("/formations", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetUnlockItemList(string playerId, string loginToken, UnityAction<UnlockItemListResult> onFinish)
    {
        GetAsDecodedJSON<UnlockItemListResult>("/unlock-items", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetClearStageList(string playerId, string loginToken, UnityAction<ClearStageListResult> onFinish)
    {
        GetAsDecodedJSON<ClearStageListResult>("/clear-stages", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetServiceTime(UnityAction<ServiceTimeResult> onFinish)
    {
        GetAsDecodedJSON<ServiceTimeResult>("/service-time", (www, result) =>
        {
            onFinish(result);
        });
    }
    #endregion

    #region Auth Services
    protected override void DoLogin(string username, string password, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("username", username);
        dict.Add("password", password);
        PostAsDecodedJSON<PlayerResult>("/login", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict));
    }

    protected override void DoRegisterOrLogin(string username, string password, UnityAction<PlayerResult> onFinish)
    {
        DoRegister(username, password, (registerResult) =>
        {
            if (registerResult.Success)
                DoLogin(username, password, onFinish);
            else
                onFinish(registerResult);
        });
    }

    protected override void DoGuestLogin(string deviceId, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("deviceId", deviceId);
        PostAsDecodedJSON<PlayerResult>("/guest-login", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict));
    }

    protected override void DoValidateLoginToken(string playerId, string loginToken, bool refreshToken, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("refreshToken", refreshToken);
        PostAsDecodedJSON<PlayerResult>("/validate-login-token", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoSetProfileName(string playerId, string loginToken, string profileName, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("profileName", profileName);
        PostAsDecodedJSON<PlayerResult>("/set-profile-name", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoRegister(string username, string password, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("username", username);
        dict.Add("password", password);
        PostAsDecodedJSON<PlayerResult>("/register", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict));
    }
    #endregion

    #region Item Services
    protected override void DoLevelUpItem(string playerId, string loginToken, string itemId, Dictionary<string, int> materials, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("itemId", itemId);
        dict.Add("materials", materials);
        PostAsDecodedJSON<ItemResult>("/levelup-item", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoEvolveItem(string playerId, string loginToken, string itemId, Dictionary<string, int> materials, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("itemId", itemId);
        dict.Add("materials", materials);
        PostAsDecodedJSON<ItemResult>("/evolve-item", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoSellItems(string playerId, string loginToken, Dictionary<string, int> items, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("items", items);
        PostAsDecodedJSON<ItemResult>("/sell-items", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoEquipItem(string playerId, string loginToken, string characterId, string equipmentId, string equipPosition, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("characterId", characterId);
        dict.Add("equipmentId", equipmentId);
        dict.Add("equipPosition", equipPosition);
        PostAsDecodedJSON<ItemResult>("/equip-item", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoUnEquipItem(string playerId, string loginToken, string equipmentId, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("equipmentId", equipmentId);
        PostAsDecodedJSON<ItemResult>("/unequip-item", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoGetAvailableLootBoxList(UnityAction<AvailableLootBoxListResult> onFinish)
    {
        GetAsDecodedJSON<AvailableLootBoxListResult>("/available-lootboxes", (www, result) =>
        {
            onFinish(result);
        });
    }

    protected override void DoGetAvailableIapPackageList(UnityAction<AvailableIapPackageListResult> onFinish)
    {
        GetAsDecodedJSON<AvailableIapPackageListResult>("/available-iap-packages", (www, result) =>
        {
            onFinish(result);
        });
    }

    protected override void DoOpenLootBox(string playerId, string loginToken, string lootBoxDataId, int packIndex, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("lootBoxDataId", lootBoxDataId);
        dict.Add("packIndex", packIndex);
        PostAsDecodedJSON<ItemResult>("/open-lootbox", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoEarnAchievementReward(string playerId, string loginToken, string achievementId, UnityAction<EarnAchievementResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("achievementId", achievementId);
        PostAsDecodedJSON<EarnAchievementResult>("/earn-achievement-reward", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }
    #endregion

    #region Social Services
    protected override void DoGetHelperList(string playerId, string loginToken, UnityAction<FriendListResult> onFinish)
    {
        GetAsDecodedJSON<FriendListResult>("/helpers", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetFriendList(string playerId, string loginToken, UnityAction<FriendListResult> onFinish)
    {
        GetAsDecodedJSON<FriendListResult>("/friends", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetFriendRequestList(string playerId, string loginToken, UnityAction<FriendListResult> onFinish)
    {
        GetAsDecodedJSON<FriendListResult>("/friend-requests", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoFindUser(string playerId, string loginToken, string displayName, UnityAction<FriendListResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("profileName", displayName);
        PostAsDecodedJSON<FriendListResult>("/find-player", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoFriendRequest(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("/friend-request", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoFriendAccept(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("/friend-accept", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoFriendDecline(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("/friend-decline", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoFriendDelete(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("/friend-delete", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }
    #endregion

    #region Battle Services
    protected override void DoStartStage(string playerId, string loginToken, string stageDataId, string helperPlayerId, UnityAction<StartStageResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("stageDataId", stageDataId);
        dict.Add("helperPlayerId", helperPlayerId);
        PostAsDecodedJSON<StartStageResult>("/start-stage", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoFinishStage(string playerId, string loginToken, string session, EBattleResult battleResult, int deadCharacters, UnityAction<FinishStageResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("session", session);
        dict.Add("battleResult", battleResult);
        dict.Add("deadCharacters", deadCharacters);
        PostAsDecodedJSON<FinishStageResult>("/finish-stage", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoReviveCharacters(string playerId, string loginToken, UnityAction<CurrencyResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        PostAsDecodedJSON<CurrencyResult>("/revive-characters", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoSelectFormation(string playerId, string loginToken, string formationName, EFormationType formationType, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("formationName", formationName);
        dict.Add("formationType", formationType);
        PostAsDecodedJSON<PlayerResult>("/select-formation", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoSetFormation(string playerId, string loginToken, string characterId, string formationName, int position, UnityAction<FormationListResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("characterId", characterId);
        dict.Add("formationName", formationName);
        dict.Add("position", position);
        PostAsDecodedJSON<FormationListResult>("/set-formation", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }
    #endregion

    #region Arena Services
    protected override void DoArenaGetOpponentList(string playerId, string loginToken, UnityAction<FriendListResult> onFinish)
    {
        GetAsDecodedJSON<FriendListResult>("/opponents", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoStartDuel(string playerId, string loginToken, string targetPlayerId, UnityAction<StartDuelResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<StartDuelResult>("/start-duel", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoFinishDuel(string playerId, string loginToken, string session, EBattleResult battleResult, int deadCharacters, UnityAction<FinishDuelResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("session", session);
        dict.Add("battleResult", battleResult);
        dict.Add("deadCharacters", deadCharacters);
        PostAsDecodedJSON<FinishDuelResult>("/finish-duel", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }
    #endregion

    #region IAP
    protected override void DoOpenIapPackage_iOS(string playerId, string loginToken, string iapPackageDataId, string receipt, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("iapPackageDataId", iapPackageDataId);
        dict.Add("receipt", receipt);
        PostAsDecodedJSON<ItemResult>("/ios-buy-goods", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }

    protected override void DoOpenIapPackage_Android(string playerId, string loginToken, string iapPackageDataId, string data, string signature, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("iapPackageDataId", iapPackageDataId);
        dict.Add("data", data);
        dict.Add("signature", signature);
        PostAsDecodedJSON<ItemResult>("/google-play-buy-goods", (www, result) =>
        {
            onFinish(result);
        }, JsonConvert.SerializeObject(dict), loginToken);
    }
    #endregion
}
