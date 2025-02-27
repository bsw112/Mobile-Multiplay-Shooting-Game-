﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class PlayerSetup : CharacterSetup, IPunInstantiateMagicCallback
{

    private PlayerMotor motor;
    private Shooter shooter;
    private PhotonView photonView;
    private FollowCam followCam;
    private ItemPickup pickup;
    private Transform canvas;
    public TextMeshProUGUI nameText;
    [HideInInspector] private Button ultiBtn;


    private Joystick attackJoystick;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        //부모가 있다면 떼버린다.
        transform.parent = null;

        photonView = GetComponent<PhotonView>();
        motor = GetComponent<PlayerMotor>();
        shooter = GetComponent<Shooter>();

        if (photonView.IsMine)
        {
            canvas = GameObject.Find("Canvas").transform;
            followCam = Camera.main.transform.GetComponent<FollowCam>();
            followCam.target = gameObject;
            motor.moveJoystick = canvas.Find("Movement Joystick").GetComponent<Joystick>();
            attackJoystick = canvas.Find("Attack Joystick").GetComponent<Joystick>();
            motor.attackJoystick = attackJoystick;
            attackJoystick.onPointerUp += GetComponent<Shooter>().OnShotButtonClicked;
            ultiBtn = canvas.Find("UltimateButton").GetComponent<Button>();
            ultiBtn.onClick.AddListener(GetComponent<Shooter>().OnUltiButtonClicked);



        }
        else
        {
            motor.enabled = false;
            shooter.enabled = false;
        }

        SetPlayerName();
    }



    private void SetPlayerName()
    {
        nameText.text = photonView.Owner.NickName;
    }

    public override void SetTeamRPC(int _team)
    {
        if (photonView == null)
            Debug.LogError("포톤 뷰가 없습니다");
        photonView.RPC("SetTeam", RpcTarget.AllBuffered, _team);
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + "의 팀은" + Team);
    }

    [PunRPC]
    private void SetTeam(int _team)
    {
        Team = _team;
    }


}
