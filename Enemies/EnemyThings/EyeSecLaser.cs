﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class EyeSecLaser : MonoBehaviour {

    public Transform StartPoint;
    public Transform EndPoint;
    public LineRenderer Laser;
    private float timeElapsed;
    private float LerpDuration = 8f;
    private Color myColor;
    public bool IsActive;

    private void Start() {
        if(Laser == null) {
            Laser = GetComponent<LineRenderer>();
        }
        Laser.enabled = false;
        Laser.startWidth = 0.5f;
        Laser.endWidth = 0.5f;

    }
    private void Update() {
        Laser.SetPosition(0, StartPoint.position);
        Laser.SetPosition(1, EndPoint.position);
        if (timeElapsed < LerpDuration) {
            myColor = Color.Lerp(Color.green, Color.red, timeElapsed / LerpDuration);
            Laser.startColor = myColor; 
            Laser.endColor = myColor;
            timeElapsed += Time.deltaTime;
            return;
        }
        timeElapsed = 0;
        Laser.startColor = Color.red;
        Laser.endColor = Color.red;
    }
    public void SetLaserEnabled(bool NewEnabled, float LaserSpeed) {
        Laser.enabled = NewEnabled;
        if(NewEnabled == false) {
            Laser.startColor = Color.green;
            Laser.endColor = Color.green;
            timeElapsed = 0;
        } else {
            timeElapsed = 0;
            LerpDuration = LaserSpeed + 1;
        }
    }
}
