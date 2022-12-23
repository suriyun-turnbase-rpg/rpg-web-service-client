using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class WebServiceClient : BaseGameService
{
    public string serviceUrl = "http://localhost/tbrpg-php-service";
    public bool debug;
    [FormerlySerializedAs("sendLoginTokenViaGetMethod")]
    public bool sendLoginTokenViaRequestQuery;
    public bool sendActionTargetViaRequestQuery;

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
        gameInstance.SetupGameDatabase();

        var path = EditorUtility.SaveFilePanel("Export Game Database", Application.dataPath, "GameData", "json");
        if (path.Length > 0)
            File.WriteAllText(path, GameInstance.GameDatabase.ToJson());
    }
#endif

    public void Get(string action, System.Action<UnityWebRequest> onDone, string loginToken = "")
    {
        StartCoroutine(GetRoutine(action, onDone, loginToken));
    }

    public void GetAsDecodedJSON<T>(string action, System.Action<UnityWebRequest, T> onDone, string loginToken = "") where T : GameServiceResult, new()
    {
        StartCoroutine(GetRoutine(action, (www) =>
        {
            T result = new T();
            if (www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.DataProcessingError)
                result.error = GameServiceErrorCode.UNKNOW;
            else if (www.result == UnityWebRequest.Result.ConnectionError)
                result.error = GameServiceErrorCode.NETWORK;
            else
                result = JsonConvert.DeserializeObject<T>(www.downloadHandler.text);

            onDone(www, result);
        }, loginToken));
    }

    public IEnumerator GetRoutine(string action, System.Action<UnityWebRequest> onDone, string loginToken = "")
    {
        var path = string.Empty;
        if (sendActionTargetViaRequestQuery)
        {
            if (!string.IsNullOrEmpty(action))
            {
                if (path.Contains("?"))
                    path += $"&action={action}";
                else
                    path += $"?action={action}";
            }
        }
        else
        {
            path += action;
        }

        if (sendLoginTokenViaRequestQuery && !string.IsNullOrEmpty(loginToken))
        {
            if (path.Contains("?"))
                path += $"&logintoken={loginToken}";
            else
                path += $"?logintoken={loginToken}";
        }

        UnityWebRequest www = UnityWebRequest.Get($"{serviceUrl}/{path}");

        if (!sendLoginTokenViaRequestQuery && !string.IsNullOrEmpty(loginToken))
        {
            www.SetRequestHeader("Authorization", "Bearer " + loginToken);
            if (debug)
                Debug.Log("[GET->Authorization] " + path + " " + loginToken);
        }
        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.LogError("[GET->Error] " + path + " " + www.error + " " + www.downloadHandler.text);

        if (debug)
            Debug.Log("[GET->Result] " + path + " " + www.downloadHandler.text);

        if (onDone != null)
            onDone.Invoke(www);
    }

    public void Post(string action, System.Action<UnityWebRequest> onDone, Dictionary<string, object> data, string loginToken = "")
    {
        StartCoroutine(PostRoutine(action, onDone, data, loginToken));
    }

    public void PostAsDecodedJSON<T>(string action, System.Action<UnityWebRequest, T> onDone, Dictionary<string, object> data, string loginToken = "")
        where T : GameServiceResult, new()
    {
        StartCoroutine(PostRoutine(action, (www) =>
        {
            T result = new T();
            if (www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.DataProcessingError)
                result.error = GameServiceErrorCode.UNKNOW;
            else if (www.result == UnityWebRequest.Result.ConnectionError)
                result.error = GameServiceErrorCode.NETWORK;
            else
                result = JsonConvert.DeserializeObject<T>(www.downloadHandler.text);

            onDone(www, result);
        }, data, loginToken));
    }

    public IEnumerator PostRoutine(string action, System.Action<UnityWebRequest> onDone, Dictionary<string, object> data, string loginToken = "")
    {
        var path = string.Empty;
        if (sendActionTargetViaRequestQuery)
        {
            if (!string.IsNullOrEmpty(action))
            {
                if (path.Contains("?"))
                    path += $"&action={action}";
                else
                    path += $"?action={action}";
            }
        }
        else
        {
            path += action;
        }

        if (sendLoginTokenViaRequestQuery && !string.IsNullOrEmpty(loginToken))
        {
            if (path.Contains("?"))
                path += $"&logintoken={loginToken}";
            else
                path += $"?logintoken={loginToken}";
        }

        var jsonData = JsonConvert.SerializeObject(data);
        UnityWebRequest www = UnityWebRequest.Post($"{serviceUrl}/{path}", jsonData);

        if (!sendLoginTokenViaRequestQuery && !string.IsNullOrEmpty(loginToken))
        {
            www.SetRequestHeader("Authorization", "Bearer " + loginToken);
            if (debug)
                Debug.Log("[POST->Authorization] " + path + " " + loginToken);
        }
        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Content-Type", "application/json");
        if (debug)
            Debug.Log("[POST->Data] " + path + " " + jsonData);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.LogError("[POST->Error] " + path + " " + www.error + " " + www.downloadHandler.text);

        if (debug)
            Debug.Log("[POST->Result] " + path + " " + www.downloadHandler.text);

        if (onDone != null)
            onDone.Invoke(www);
    }

    #region Listing Services
    protected override void DoGetAchievementList(string playerId, string loginToken, UnityAction<AchievementListResult> onFinish)
    {
        GetAsDecodedJSON<AchievementListResult>("achievements", (www, result) =>
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
        GetAsDecodedJSON<ItemListResult>("items", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetCurrencyList(string playerId, string loginToken, UnityAction<CurrencyListResult> onFinish)
    {
        GetAsDecodedJSON<CurrencyListResult>("currencies", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetStaminaList(string playerId, string loginToken, UnityAction<StaminaListResult> onFinish)
    {
        GetAsDecodedJSON<StaminaListResult>("staminas", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetFormationList(string playerId, string loginToken, UnityAction<FormationListResult> onFinish)
    {
        GetAsDecodedJSON<FormationListResult>("formations", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetUnlockItemList(string playerId, string loginToken, UnityAction<UnlockItemListResult> onFinish)
    {
        GetAsDecodedJSON<UnlockItemListResult>("unlock-items", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetClearStageList(string playerId, string loginToken, UnityAction<ClearStageListResult> onFinish)
    {
        GetAsDecodedJSON<ClearStageListResult>("clear-stages", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetServiceTime(UnityAction<ServiceTimeResult> onFinish)
    {
        GetAsDecodedJSON<ServiceTimeResult>("service-time", (www, result) =>
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
        PostAsDecodedJSON<PlayerResult>("login", (www, result) =>
        {
            onFinish(result);
        }, dict);
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
        PostAsDecodedJSON<PlayerResult>("guest-login", (www, result) =>
        {
            onFinish(result);
        }, dict);
    }

    protected override void DoValidateLoginToken(string playerId, string loginToken, bool refreshToken, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("refreshToken", refreshToken);
        PostAsDecodedJSON<PlayerResult>("validate-login-token", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoSetProfileName(string playerId, string loginToken, string profileName, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("profileName", profileName);
        PostAsDecodedJSON<PlayerResult>("set-profile-name", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoRegister(string username, string password, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("username", username);
        dict.Add("password", password);
        PostAsDecodedJSON<PlayerResult>("register", (www, result) =>
        {
            onFinish(result);
        }, dict);
    }
    #endregion

    #region Item Services
    protected override void DoLevelUpItem(string playerId, string loginToken, string itemId, Dictionary<string, int> materials, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("itemId", itemId);
        dict.Add("materials", materials);
        PostAsDecodedJSON<ItemResult>("levelup-item", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoEvolveItem(string playerId, string loginToken, string itemId, Dictionary<string, int> materials, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("itemId", itemId);
        dict.Add("materials", materials);
        PostAsDecodedJSON<ItemResult>("evolve-item", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoSellItems(string playerId, string loginToken, Dictionary<string, int> items, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("items", items);
        PostAsDecodedJSON<ItemResult>("sell-items", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoEquipItem(string playerId, string loginToken, string characterId, string equipmentId, string equipPosition, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("characterId", characterId);
        dict.Add("equipmentId", equipmentId);
        dict.Add("equipPosition", equipPosition);
        PostAsDecodedJSON<ItemResult>("equip-item", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoUnEquipItem(string playerId, string loginToken, string equipmentId, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("equipmentId", equipmentId);
        PostAsDecodedJSON<ItemResult>("unequip-item", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoCraftItem(string playerId, string loginToken, string itemCraftId, Dictionary<string, int> materials, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("itemCraftId", itemCraftId);
        dict.Add("materials", materials);
        PostAsDecodedJSON<ItemResult>("craft-item", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoGetAvailableLootBoxList(UnityAction<AvailableLootBoxListResult> onFinish)
    {
        GetAsDecodedJSON<AvailableLootBoxListResult>("available-lootboxes", (www, result) =>
        {
            onFinish(result);
        });
    }

    protected override void DoGetAvailableIapPackageList(UnityAction<AvailableIapPackageListResult> onFinish)
    {
        GetAsDecodedJSON<AvailableIapPackageListResult>("available-iap-packages", (www, result) =>
        {
            onFinish(result);
        });
    }

    protected override void DoGetAvailableInGamePackageList(UnityAction<AvailableInGamePackageListResult> onFinish)
    {
        GetAsDecodedJSON<AvailableInGamePackageListResult>("available-ingame-packages", (www, result) =>
        {
            onFinish(result);
        });
    }

    protected override void DoOpenLootBox(string playerId, string loginToken, string lootBoxDataId, int packIndex, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("lootBoxDataId", lootBoxDataId);
        dict.Add("packIndex", packIndex);
        PostAsDecodedJSON<ItemResult>("open-lootbox", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoEarnAchievementReward(string playerId, string loginToken, string achievementId, UnityAction<EarnAchievementResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("achievementId", achievementId);
        PostAsDecodedJSON<EarnAchievementResult>("earn-achievement-reward", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoConvertHardCurrency(string playerId, string loginToken, int requireHardCurrency, UnityAction<HardCurrencyConversionResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("requireHardCurrency", requireHardCurrency);
        PostAsDecodedJSON<HardCurrencyConversionResult>("convert-hard-currency", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoRefillStamina(string playerId, string loginToken, string staminaDataId, UnityAction<RefillStaminaResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("staminaDataId", staminaDataId);
        PostAsDecodedJSON<RefillStaminaResult>("refill-stamina", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoGetRefillStaminaInfo(string playerId, string loginToken, string staminaDataId, UnityAction<RefillStaminaInfoResult> onFinish)
    {
        if (sendActionTargetViaRequestQuery)
        {
            GetAsDecodedJSON<RefillStaminaInfoResult>($"refill-stamina-info&staminaDataId={staminaDataId}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
        else
        {
            GetAsDecodedJSON<RefillStaminaInfoResult>($"refill-stamina-info/{staminaDataId}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
    }
    #endregion

    #region Social Services
    protected override void DoGetHelperList(string playerId, string loginToken, UnityAction<PlayerListResult> onFinish)
    {
        GetAsDecodedJSON<PlayerListResult>("helpers", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetFriendList(string playerId, string loginToken, UnityAction<PlayerListResult> onFinish)
    {
        GetAsDecodedJSON<PlayerListResult>("friends", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetFriendRequestList(string playerId, string loginToken, UnityAction<PlayerListResult> onFinish)
    {
        GetAsDecodedJSON<PlayerListResult>("friend-requests", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetPendingRequestList(string playerId, string loginToken, UnityAction<PlayerListResult> onFinish)
    {
        GetAsDecodedJSON<PlayerListResult>("pending-requests", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoFindUser(string playerId, string loginToken, string displayName, UnityAction<PlayerListResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("profileName", displayName);
        PostAsDecodedJSON<PlayerListResult>("find-player", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFriendRequest(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("friend-request", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFriendAccept(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("friend-accept", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFriendDecline(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("friend-decline", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFriendDelete(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("friend-delete", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFriendRequestDelete(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("friend-request-delete", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    #region Battle Services
    protected override void DoStartStage(string playerId, string loginToken, string stageDataId, string helperPlayerId, UnityAction<StartStageResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("stageDataId", stageDataId);
        dict.Add("helperPlayerId", helperPlayerId);
        PostAsDecodedJSON<StartStageResult>("start-stage", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFinishStage(string playerId, string loginToken, string session, EBattleResult battleResult, int totalDamage, int deadCharacters, UnityAction<FinishStageResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("session", session);
        dict.Add("battleResult", battleResult);
        dict.Add("totalDamage", totalDamage);
        dict.Add("deadCharacters", deadCharacters);
        PostAsDecodedJSON<FinishStageResult>("finish-stage", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoReviveCharacters(string playerId, string loginToken, UnityAction<CurrencyResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        PostAsDecodedJSON<CurrencyResult>("revive-characters", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoSelectFormation(string playerId, string loginToken, string formationName, EFormationType formationType, UnityAction<PlayerResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("formationName", formationName);
        dict.Add("formationType", formationType);
        PostAsDecodedJSON<PlayerResult>("select-formation", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoSetFormation(string playerId, string loginToken, string characterId, string formationName, int position, UnityAction<FormationListResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("characterId", characterId);
        dict.Add("formationName", formationName);
        dict.Add("position", position);
        PostAsDecodedJSON<FormationListResult>("set-formation", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoGetAvailableStageList(UnityAction<AvailableStageListResult> onFinish)
    {
        GetAsDecodedJSON<AvailableStageListResult>("available-stages", (www, result) =>
        {
            onFinish(result);
        });
    }

    #endregion

    #region Arena Services
    protected override void DoGetArenaOpponentList(string playerId, string loginToken, UnityAction<PlayerListResult> onFinish)
    {
        GetAsDecodedJSON<PlayerListResult>("opponents", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoStartDuel(string playerId, string loginToken, string targetPlayerId, UnityAction<StartDuelResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<StartDuelResult>("start-duel", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFinishDuel(string playerId, string loginToken, string session, EBattleResult battleResult, int totalDamage, int deadCharacters, UnityAction<FinishDuelResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("session", session);
        dict.Add("battleResult", battleResult);
        dict.Add("totalDamage", totalDamage);
        dict.Add("deadCharacters", deadCharacters);
        PostAsDecodedJSON<FinishDuelResult>("finish-duel", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    #region IAP
    protected override void DoOpenIapPackage_iOS(string playerId, string loginToken, string iapPackageDataId, string receipt, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("iapPackageDataId", iapPackageDataId);
        dict.Add("receipt", receipt);
        PostAsDecodedJSON<ItemResult>("ios-buy-goods", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoOpenIapPackage_Android(string playerId, string loginToken, string iapPackageDataId, string data, string signature, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("iapPackageDataId", iapPackageDataId);
        dict.Add("data", data);
        dict.Add("signature", signature);
        PostAsDecodedJSON<ItemResult>("google-play-buy-goods", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    #region In-Game Package
    protected override void DoOpenInGamePackage(string playerId, string loginToken, string inGamePackageDataId, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("inGamePackageDataId", inGamePackageDataId);
        PostAsDecodedJSON<ItemResult>("open-ingame-package", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    #region Clan
    protected override void DoCreateClan(string playerId, string loginToken, string clanName, UnityAction<CreateClanResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("clanName", clanName);
        PostAsDecodedJSON<CreateClanResult>("create-clan", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFindClan(string playerId, string loginToken, string clanName, UnityAction<ClanListResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("clanName", clanName);
        PostAsDecodedJSON<ClanListResult>("find-clan", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoClanJoinRequest(string playerId, string loginToken, string clanId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("clanId", clanId);
        PostAsDecodedJSON<GameServiceResult>("clan-join-request", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoClanJoinAccept(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("clan-join-accept", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoClanJoinDecline(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("clan-join-decline", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoClanMemberDelete(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("clan-member-delete", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoClanJoinRequestDelete(string playerId, string loginToken, string clanId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("clanId", clanId);
        PostAsDecodedJSON<GameServiceResult>("clan-join-request-delete", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoGetClanMemberList(string playerId, string loginToken, UnityAction<PlayerListResult> onFinish)
    {
        GetAsDecodedJSON<PlayerListResult>("clan-members", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoClanOwnerTransfer(string playerId, string loginToken, string targetPlayerId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        PostAsDecodedJSON<GameServiceResult>("clan-owner-transfer", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoClanTerminate(string playerId, string loginToken, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        PostAsDecodedJSON<GameServiceResult>("clan-terminate", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoGetClan(string playerId, string loginToken, UnityAction<ClanResult> onFinish)
    {
        GetAsDecodedJSON<ClanResult>("clan", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetClanJoinRequestList(string playerId, string loginToken, UnityAction<PlayerListResult> onFinish)
    {
        GetAsDecodedJSON<PlayerListResult>("clan-join-requests", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetClanJoinPendingRequestList(string playerId, string loginToken, UnityAction<ClanListResult> onFinish)
    {
        GetAsDecodedJSON<ClanListResult>("clan-join-pending-requests", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoClanExit(string playerId, string loginToken, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        PostAsDecodedJSON<GameServiceResult>("clan-exit", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoClanSetRole(string playerId, string loginToken, string targetPlayerId, byte clanRole, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("targetPlayerId", targetPlayerId);
        dict.Add("clanRole", clanRole);
        PostAsDecodedJSON<GameServiceResult>("clan-set-role", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoClanCheckin(string playerId, string loginToken, UnityAction<ClanCheckinResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        PostAsDecodedJSON<ClanCheckinResult>("clan-checkin", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoGetClanCheckinStatus(string playerId, string loginToken, UnityAction<ClanCheckinStatusResult> onFinish)
    {
        GetAsDecodedJSON<ClanCheckinStatusResult>("clan-checkin-status", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoClanDonation(string clanDonationDataId, string playerId, string loginToken, UnityAction<ClanDonationResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("clanDonationDataId", clanDonationDataId);
        PostAsDecodedJSON<ClanDonationResult>("clan-donation", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoGetClanDonationStatus(string playerId, string loginToken, UnityAction<ClanDonationStatusResult> onFinish)
    {
        GetAsDecodedJSON<ClanDonationStatusResult>("clan-donation-status", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }
    #endregion

    #region Chat
    protected override void DoGetChatMessages(string playerId, string loginToken, long lastTime, UnityAction<ChatMessageListResult> onFinish)
    {
        if (sendActionTargetViaRequestQuery)
        {
            GetAsDecodedJSON<ChatMessageListResult>($"chat-messages&lastTime={lastTime}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
        else
        {
            GetAsDecodedJSON<ChatMessageListResult>($"chat-messages/{lastTime}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
    }

    protected override void DoGetClanChatMessages(string playerId, string loginToken, long lastTime, UnityAction<ChatMessageListResult> onFinish)
    {
        if (sendActionTargetViaRequestQuery)
        {
            GetAsDecodedJSON<ChatMessageListResult>($"clan-chat-messages&lastTime={lastTime}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
        else
        {
            GetAsDecodedJSON<ChatMessageListResult>($"clan-chat-messages/{lastTime}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
    }

    protected override void DoEnterChatMessage(string playerId, string loginToken, string message, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("message", message);
        PostAsDecodedJSON<GameServiceResult>("enter-chat-message", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoEnterClanChatMessage(string playerId, string loginToken, string message, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("message", message);
        PostAsDecodedJSON<GameServiceResult>("enter-clan-chat-message", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    #region Raid Event
    protected override void DoGetRaidEventList(string playerId, string loginToken, UnityAction<RaidEventListResult> onFinish)
    {
        GetAsDecodedJSON<RaidEventListResult>("raid-events", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoStartRaidBossBattle(string playerId, string loginToken, string eventId, UnityAction<StartRaidBossBattleResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("eventId", eventId);
        PostAsDecodedJSON<StartRaidBossBattleResult>("start-raid-boss-battle", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFinishRaidBossBattle(string playerId, string loginToken, string session, EBattleResult battleResult, int totalDamage, int deadCharacters, UnityAction<FinishRaidBossBattleResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("session", session);
        dict.Add("battleResult", battleResult);
        dict.Add("totalDamage", totalDamage);
        dict.Add("deadCharacters", deadCharacters);
        PostAsDecodedJSON<FinishRaidBossBattleResult>("finish-raid-boss-battle", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    #region Mail
    protected override void DoGetMailList(string playerId, string loginToken, UnityAction<MailListResult> onFinish)
    {
        GetAsDecodedJSON<MailListResult>("mails", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoReadMail(string playerId, string loginToken, string id, UnityAction<ReadMailResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("id", id);
        PostAsDecodedJSON<ReadMailResult>("read-mail", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoClaimMailRewards(string playerId, string loginToken, string id, UnityAction<ItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("id", id);
        PostAsDecodedJSON<ItemResult>("claim-mail-rewards", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoDeleteMail(string playerId, string loginToken, string id, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("id", id);
        PostAsDecodedJSON<GameServiceResult>("delete-mail", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoGetMailsCount(string playerId, string loginToken, UnityAction<MailsCountResult> onFinish)
    {
        GetAsDecodedJSON<MailsCountResult>("mails-count", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }
    #endregion

    #region Clan Event
    protected override void DoGetClanEventList(string playerId, string loginToken, UnityAction<ClanEventListResult> onFinish)
    {
        GetAsDecodedJSON<ClanEventListResult>("clan-events", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoStartClanBossBattle(string playerId, string loginToken, string eventId, UnityAction<StartClanBossBattleResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("eventId", eventId);
        PostAsDecodedJSON<StartClanBossBattleResult>("start-clan-boss-battle", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoFinishClanBossBattle(string playerId, string loginToken, string session, EBattleResult battleResult, int totalDamage, int deadCharacters, UnityAction<FinishClanBossBattleResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("session", session);
        dict.Add("battleResult", battleResult);
        dict.Add("totalDamage", totalDamage);
        dict.Add("deadCharacters", deadCharacters);
        PostAsDecodedJSON<FinishClanBossBattleResult>("finish-clan-boss-battle", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    #region Random Store Event
    protected override void DoGetRandomStore(string playerId, string loginToken, string id, UnityAction<RandomStoreResult> onFinish)
    {
        if (sendActionTargetViaRequestQuery)
        {
            GetAsDecodedJSON<RandomStoreResult>($"random-store&id={id}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
        else
        {
            GetAsDecodedJSON<RandomStoreResult>($"random-store/{id}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
    }

    protected override void DoPurchaseRandomStoreItem(string playerId, string loginToken, string id, int index, UnityAction<PurchaseRandomStoreItemResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("id", id);
        dict.Add("index", index);
        PostAsDecodedJSON<PurchaseRandomStoreItemResult>("purchase-random-store-item", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoRefreshRandomStore(string playerId, string loginToken, string id, UnityAction<RefreshRandomStoreResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("id", id);
        PostAsDecodedJSON<RefreshRandomStoreResult>("refresh-random-store", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    #region Daily Reward
    protected override void DoGetAllDailyRewardList(string playerId, string loginToken, UnityAction<AllDailyRewardListResult> onFinish)
    {
        GetAsDecodedJSON<AllDailyRewardListResult>($"all-daily-rewarding", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetDailyRewardList(string playerId, string loginToken, string id, UnityAction<DailyRewardListResult> onFinish)
    {
        if (sendActionTargetViaRequestQuery)
        {
            GetAsDecodedJSON<DailyRewardListResult>($"daily-rewarding&id={id}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
        else
        {
            GetAsDecodedJSON<DailyRewardListResult>($"daily-rewarding/{id}", (www, result) =>
            {
                onFinish(result);
            }, loginToken);
        }
    }

    protected override void DoClaimDailyReward(string playerId, string loginToken, string id, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("id", id);
        PostAsDecodedJSON<RefreshRandomStoreResult>("daily-rewarding-claim", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    #region Profile
    protected override void DoGetUnlockIconList(string playerId, string loginToken, UnityAction<UnlockIconListResult> onFinish)
    {
        GetAsDecodedJSON<UnlockIconListResult>("unlock-icons", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetUnlockFrameList(string playerId, string loginToken, UnityAction<UnlockFrameListResult> onFinish)
    {
        GetAsDecodedJSON<UnlockFrameListResult>("unlock-frames", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetUnlockTitleList(string playerId, string loginToken, UnityAction<UnlockTitleListResult> onFinish)
    {
        GetAsDecodedJSON<UnlockTitleListResult>("unlock-titles", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetClanUnlockIconList(string playerId, string loginToken, UnityAction<ClanUnlockIconListResult> onFinish)
    {
        GetAsDecodedJSON<ClanUnlockIconListResult>("clan-unlock-icons", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetClanUnlockFrameList(string playerId, string loginToken, UnityAction<ClanUnlockFrameListResult> onFinish)
    {
        GetAsDecodedJSON<ClanUnlockFrameListResult>("clan-unlock-frames", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoGetClanUnlockTitleList(string playerId, string loginToken, UnityAction<ClanUnlockTitleListResult> onFinish)
    {
        GetAsDecodedJSON<ClanUnlockTitleListResult>("clan-unlock-titles", (www, result) =>
        {
            onFinish(result);
        }, loginToken);
    }

    protected override void DoSetPlayerIcon(string playerId, string loginToken, string iconDataId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("iconDataId", iconDataId);
        PostAsDecodedJSON<GameServiceResult>("set-icon", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoSetPlayerFrame(string playerId, string loginToken, string frameDataId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("frameDataId", frameDataId);
        PostAsDecodedJSON<GameServiceResult>("set-frame", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoSetPlayerTitle(string playerId, string loginToken, string titleDataId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("titleDataId", titleDataId);
        PostAsDecodedJSON<GameServiceResult>("set-title", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoSetClanIcon(string playerId, string loginToken, string iconDataId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("iconDataId", iconDataId);
        PostAsDecodedJSON<GameServiceResult>("set-clan-icon", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoSetClanFrame(string playerId, string loginToken, string frameDataId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("frameDataId", frameDataId);
        PostAsDecodedJSON<GameServiceResult>("set-clan-frame", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }

    protected override void DoSetClanTitle(string playerId, string loginToken, string titleDataId, UnityAction<GameServiceResult> onFinish)
    {
        var dict = new Dictionary<string, object>();
        dict.Add("titleDataId", titleDataId);
        PostAsDecodedJSON<GameServiceResult>("set-clan-title", (www, result) =>
        {
            onFinish(result);
        }, dict, loginToken);
    }
    #endregion

    protected override void DoGetFormationCharactersAndEquipments(string playerId, string formationDataId, UnityAction<FormationCharactersAndEquipmentsResult> onFinish)
    {
        if (sendActionTargetViaRequestQuery)
        {
            GetAsDecodedJSON<FormationCharactersAndEquipmentsResult>($"formation-characters-and-equipments&playerId={playerId}&formationDataId={formationDataId}", (www, result) =>
            {
                onFinish(result);
            });
        }
        else
        {
            GetAsDecodedJSON<FormationCharactersAndEquipmentsResult>($"formation-characters-and-equipments/{playerId}/{formationDataId}", (www, result) =>
            {
                onFinish(result);
            });
        }
    }

    protected override void DoGetArenaFormationCharactersAndEquipments(string playerId, UnityAction<FormationCharactersAndEquipmentsResult> onFinish)
    {
        if (sendActionTargetViaRequestQuery)
        {
            GetAsDecodedJSON<FormationCharactersAndEquipmentsResult>($"arena-formation-characters-and-equipments&playerId={playerId}", (www, result) =>
            {
                onFinish(result);
            });
        }
        else
        {
            GetAsDecodedJSON<FormationCharactersAndEquipmentsResult>($"arena-formation-characters-and-equipments/{playerId}", (www, result) =>
            {
                onFinish(result);
            });
        }
    }
}
