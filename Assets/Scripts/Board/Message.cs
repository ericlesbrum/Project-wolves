using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Message : NetworkBehaviour
{
    public TextMeshProUGUI title, description;
    public GameObject background;
    public bool endGame;
    public void SetMsg(string title, string description,bool endGame)
    {
        this.title.text = title;
        this.description.text = description;
        this.endGame = endGame;
    }
    public void Confirm()
    {
        if(endGame)
        {
            background.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
