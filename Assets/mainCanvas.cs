using LitJson;
using GoClient;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Google.Protobuf;
using UnityEditor;

public class mainCanvas : MonoBehaviour
{
    private LeafClient pc;
    private string host = "127.0.0.1";
    //private string gateIP = "39.98.124.41";
    private int port = 3563;

    private Button btn_connect;
    private Button btn_login;
    private Button btn_register;
    private Button btn_checkVersion;
    private Button btn_checkWhite;
    private Button btn_selectArea;
    private Button btn_disconnect;
    private Button btn_chat;
    private Button btn_doChat;

    private InputField input_username;

    private bool connected = false;

    #region Unity Event

    void Start()
    {
        Debug.Log("Start is called!");
        SimonProto.ReqGateGetConnector getConnector = new SimonProto.ReqGateGetConnector();
        getConnector.Region = "abc";
        byte[] databytes = getConnector.ToByteArray();
        IMessage imPerson = new SimonProto.ReqGateGetConnector();
        SimonProto.ReqGateGetConnector get2 = (SimonProto.ReqGateGetConnector)imPerson.Descriptor.Parser.ParseFrom(databytes);
        Debug.Log(get2.Region);
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Update is called!");
        if (connected)
        {
            btn_connect.interactable = false;
        }
        else
        {
            btn_connect.interactable = true;
        }
    }

    // Use this for initialization
    void Awake()
    {
        input_username = GameObject.Find("input_username").GetComponent<InputField>();

        btn_connect = GameObject.Find("btn_connect").GetComponent<Button>();
        btn_login = GameObject.Find("btn_login").GetComponent<Button>();
        btn_register = GameObject.Find("btn_register").GetComponent<Button>();
        btn_checkVersion = GameObject.Find("btn_checkVersion").GetComponent<Button>();
        btn_checkWhite = GameObject.Find("btn_checkWhite").GetComponent<Button>();
        btn_selectArea = GameObject.Find("btn_selectArea").GetComponent<Button>();
        btn_disconnect = GameObject.Find("btn_disconnect").GetComponent<Button>();
        btn_chat = GameObject.Find("btn_chat").GetComponent<Button>();
        btn_doChat = GameObject.Find("btn_doChat").GetComponent<Button>();

        btn_connect.onClick.AddListener(onConnectClick);
        //btn_login.onClick.AddListener(onLoginClick);
        //btn_register.onClick.AddListener(onRegisterClick);
        //btn_checkVersion.onClick.AddListener(onCheckVersionClick);
        //btn_checkWhite.onClick.AddListener(onCheckWhiteClick);
        //btn_selectArea.onClick.AddListener(onSelectAreaClick);
        btn_disconnect.onClick.AddListener(onDisconnectClick);
        btn_chat.onClick.AddListener(onChatClick);
        btn_doChat.onClick.AddListener(onDoChatClick);
    }

    private void OnDestroy()
    {
        pc?.Disconnect("quit game");
    }

    #endregion

    #region Button Click
    private void onDoChatClick()
    {
        string interfaceName = "ReqGateGetConnector";
        SimonProto.ReqGateGetConnector getConnector = new SimonProto.ReqGateGetConnector();
        getConnector.Region = "abc";
        byte[] databytes = getConnector.ToByteArray();
        pc.Req(interfaceName, databytes, response =>
        {
            SimonProto.ResGateGetConnector res = SimonProto.ResGateGetConnector.Parser.ParseFrom((byte[])response);
            Debug.Log("[Protocol]response:" + res.ToString());
        });
    }

    private void onChatClick()
    {
        string interfaceName = "scene.chatHandler.joinChatRoom";

        // pc.Request(interfaceName, "", response =>
        //   {
        //       Debug.Log(response);
        //   });

    }

    private void onDisconnectClick()
    {
        pc.Disconnect("click disconnect button");
    }

    void onConnectClick()
    {
        pc = new LeafClient(host, port);
        bool res = pc.ConnectServer();

        if (res)
        {
            this.InitSimpleEventListener();

            Debug.Log("Connected connector server!");
            connected = true;
        }
    }
    #endregion

    private void InitSimpleEventListener()
    {
        pc.On("disconnect", msg =>
        {
            Debug.Log("connector disconnect : " + msg);
            connected = false;
        });

        //pc.On(ServerPushInterface.OnMainStateChanged.InterfaceName, data =>
        //{
        //    ServerPushInterface.OnMainStateChanged onlineState = (ServerPushInterface.OnMainStateChanged)data;

        //    Debug.Log("OnMainStateChanged:" + JsonMapper.ToJson(onlineState));
        //});

        pc.On("onChat", data =>
        {
            Debug.Log("onChat:" + JsonMapper.ToJson(data));
        });

        //_pomeloSceneClient.On(ServerPushInterface.OnReceiveFriendGameInvitation.InterfaceName, data =>
        //{
        //    AppendLog("onInvite:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //    ServerPushInterface.OnReceiveFriendGameInvitation res = (ServerPushInterface.OnReceiveFriendGameInvitation)data;
        //});

        //_pomeloSceneClient.On(ServerPushInterface.OnReceiveCancelFriendGameInvitation.InterfaceName, data =>
        //{
        //    AppendLog("onCancelInvite:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //});

        //_pomeloSceneClient.On(ServerPushInterface.OnMatched.InterfaceName, data =>
        //{
        //    AppendLog("onMatched:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //});

        //_pomeloSceneClient.On(ServerPushInterface.OnAddedFriend.InterfaceName, data =>
        //{
        //    AppendLog("OnAddedFriend:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //});

        //_pomeloSceneClient.On(ServerPushInterface.OnReceiveAddFriendInvitation.InterfaceName, data =>
        //{
        //    AppendLog("onReceiveAddFriendInvitation:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //});

        //_pomeloSceneClient.On(ServerPushInterface.OnReceiveReplyAddFriendInvitation.InterfaceName, data =>
        //{
        //    AppendLog("onReceiveReplyAddFriendInvitation:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //});

        //_pomeloSceneClient.On(ServerPushInterface.OnReceiveFinishVirusRecruit.InterfaceName, data =>
        //{
        //    AppendLog("OnReceiveFinishVirusRecruit:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //});

        //_pomeloSceneClient.On(ServerPushInterface.OnReceiveUnionMsg.InterfaceName, data =>
        //{
        //    AppendLog("unionMsg:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //});

        //_pomeloSceneClient.On(ServerPushInterface.OnAcceptEnterUnion.InterfaceName, data =>
        //{
        //    if (data != null)
        //    {
        //        AppendLog("OnAcceptEnterUnion:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //    }
        //    else
        //    {
        //        AppendLog("OnAcceptEnterUnion:", this.tb_info_friend);
        //    }

        //});

        //_pomeloSceneClient.On(ServerPushInterface.OnReceivedTrumpet.InterfaceName, data =>
        //{
        //    AppendLog("trumpet msg:" + JsonMapper.ToJson(data), this.tb_info_friend);
        //});
    }
}
