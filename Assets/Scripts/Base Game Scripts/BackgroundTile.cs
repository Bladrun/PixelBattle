using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundTile : MonoBehaviour {
	public int hitPoints;
	private GoalManager goalManager;

	
	private void Start(){
		goalManager = FindObjectOfType<GoalManager> ();
	}

	private void Update(){
		if (hitPoints <= 0) {
			if (goalManager != null) {
				goalManager.CompareGoal (this.gameObject.tag);
				goalManager.UpdateGoals ();
			}
			Destroy (this.gameObject);
		}
	}

	public void TakeDamage(int damage){
		hitPoints -= damage;
	}

}
