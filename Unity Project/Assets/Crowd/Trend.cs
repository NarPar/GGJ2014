﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

// types of hats in the game
public enum Hat { Pope, Bowler, Top }

public class Trend : MonoBehaviour
{
  private Animator Anim; // Reference to the player's animator component.
  protected Dictionary<Hat,GameObject> HatObjects; // collection of our hat game objects
  public Hat CurrentHat; // hat currently wornly by character
  
  // default hat worn by characters when the game starts
  protected virtual Hat DefaultHat { get { return Hat.Bowler; } }
  
  // hat cooldown
  protected virtual double HatCooldownMillseconds { get { return 3000; } } // minimum time between hat uses
  protected Timer HatCooldownTimer;
  protected bool HatOnCooldown = false; // whether the hat can be used currently
  
  protected bool TransformWaiting = false;
  protected Hat TransformHat;
  protected float TransformTransmission = 0.0f;
  
  protected bool HatWaiting = false;
  protected Hat HatOnCall;
  
  protected bool AnimTriggerWaiting = false;
  protected string AnimTriggerString = null;

  protected virtual void Awake()
  {
    // Setting up references.
    Anim = GetComponent<Animator>();
    HatObjects = new Dictionary<Hat,GameObject>()
    {
      {Hat.Pope, transform.Find("PopeHat").gameObject},
      {Hat.Bowler, transform.Find("BowlerHat").gameObject},
      {Hat.Top, transform.Find("TopHat").gameObject},
    };
  }
  
	// Use this for initialization
	protected virtual void Start()
  {
    SetCurrentHat(DefaultHat);
    HatCooldownTimer = new Timer(HatCooldownMillseconds) // timer counts down after hat has been used
    {
      AutoReset = false,
    };
    HatCooldownTimer.Elapsed += HatCooldownTimer_Elapsed;
	}
	
	// Update is called once per frame
  protected virtual void Update()
  {

    Debug.DrawLine(transform.position, transform.position + Vector3.right * 3);
    // these must be done in main thread because they access UnityEngine's renderer
    if (AnimTriggerWaiting)
    {
      Anim.SetTrigger(AnimTriggerString);
      
      AnimTriggerWaiting = false;
    }

    // only npcs and the trend setter can cause trends
    if (TransformWaiting && ((tag == "Player" && GetComponent<PlayerClass>().playerClass == Class.TrendSetter) || tag != "Player"))
    {
      //Debug.Log(TransformTransmission);
      var furtherTransmissionChance = TransformTransmission * 0.5f - 0.05f;
      var nearbyObjects = GetObjectsInRadius(transform.position, 1);
      int trendCount = 0; // for counting how many you successfully influenced (trendsetter)
      foreach (var gameObject in nearbyObjects)
      {
        if(gameObject.tag != "Player") {
          var trend = gameObject.GetComponent<Trend>();

          if (TransformTransmission > Random.value) {
            trendCount++;
            trend.ChangeHat(TransformHat, furtherTransmissionChance);
           
          }
        }
      }

      if(tag == "Player") {
        GetComponent<PlayerClass>().HandleTrendSet(trendCount);
      }
      
      TransformWaiting = false;
    }
    
	  if (HatWaiting)
    {
      // hide all hats
      foreach (var hatObject in HatObjects.Values)
        hatObject.renderer.enabled = false;
      
      // display the new hat and update reference to the current hat
      HatObjects[HatOnCall].renderer.enabled = true;
      CurrentHat = HatOnCall;
      
      HatWaiting = false;
    }
  }
  
  protected virtual void HatCooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
  {
    HatOnCooldown = false;
  }
  
  // determines whether the character can change to a given hat at this time
  protected virtual bool CanChangeHat(Hat newHat)
  {
    return !HatOnCooldown;
  }

  public void TryChangeHat(Hat newHat)
  {
    if (CanChangeHat(newHat)) // check if the hat can be swapped
    {
      ChangeHat(newHat, 1.0f);
    }
  }
  
  public virtual void ChangeHat(Hat newHat, float transmissionChance)
  {

    // update the current hat
    SetCurrentHat(newHat);
    // display swap animation (must be done in main thread)
    AnimTriggerString = "Hat";
    AnimTriggerWaiting = true;
    
    // trigger local trend
    if (transmissionChance > 0)
      InitiateTrend(newHat, transmissionChance);
    
    // initiate hat cooldown
    HatOnCooldown = true;
    HatCooldownTimer.Start();
  }
  
  protected virtual void InitiateTrend(Hat trendyHat, float transmissionChance)
  {
    TransformHat = trendyHat;
    TransformTransmission = transmissionChance;
    TransformWaiting = true;
  }
  
  IEnumerable<GameObject> GetObjectsInRadius(Vector3 center, float radius)
  {
    var colliders = Physics2D.OverlapCircleAll(center, radius);//Physics.OverlapSphere(center, radius);
    return colliders.Select(u => u.gameObject);
    //return Physics.OverlapSphere(center, radius).Select(u => u.gameObject);
  }
  
  protected void SetCurrentHat(Hat newHat)
  {
    // this must be done in main thread because it accesses UnityEngine's renderer
    HatOnCall = newHat;
    HatWaiting = true;
  }
}
