﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour {
    private bool IsTimeUp;
    private float Interval;
    private int Timer;

    [SerializeField]
    private GameObject TextObj;
    private Text Text;

    [SerializeField]
    private GameObject GameOverText;
    [SerializeField]
    private GameObject Guide;
    [SerializeField]
    private GameObject Player;

    private IEnumerator Coroutine;

    private IEnumerator ReduceTime() {
        while (!GetIsTimeUp()) {
            Text.text = (Timer / 60).ToString() + ":" + (Timer % 60).ToString();
            Timer--;
            if (Player.gameObject != null && Timer <= 0 || Player.GetComponent<PlayerData>().Alive == false) {
                GameOverText.SetActive(true);
                Guide.SetActive(false);
                Time.timeScale = 0.0f;
                Text.text = (Timer / 60).ToString() + ":" + (Timer % 60).ToString();
                IsTimeUp = true;
            }
            yield return new WaitForSeconds(Interval);
        }
    }

    public bool GetIsTimeUp() {
        return IsTimeUp;
    }

    public void SetIsTimeUp(bool b) {
        this.IsTimeUp = b;
    }

    private void Start() {
        SetIsTimeUp(false);
        Timer = 600;
        Interval = 1.0f;
        Coroutine = ReduceTime();
        Text = TextObj.GetComponent<Text>();
        StartCoroutine(ReduceTime());

    }
}