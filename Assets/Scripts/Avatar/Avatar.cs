using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Avatar : NetworkBehaviour
{
    public TextMeshProUGUI _name;
    public Button button;
    public Image image,background;
    public ulong id;
    public bool isVisible = false;

    public void SetAvatar(string _name, ulong id)
    {
        this._name.text = _name;
        this.id = id;
    }
    public void OnButtonClick()
    {
        isVisible = !isVisible;
        SetImageVisibility(isVisible);
    }
    public void SetImageVisibility(bool visible)
    {
        isVisible = visible;
        image.gameObject.SetActive(visible);
    }
}