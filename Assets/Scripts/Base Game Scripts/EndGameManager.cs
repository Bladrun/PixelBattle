using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameType{
	Moves
}

[System.Serializable]
public class EndGameRequirements{
	public GameType gameType;
	public int counterValue;
}

public class EndGameManager : MonoBehaviour {

	public GameObject movesLabel;
	public GameObject youWinPanel;
	public GameObject tryAgainPanel;
    public Text counter;
	public EndGameRequirements requirements;
	public int currentCounterValue;
	private Board board;

	// Use this for initialization
	void Start () {
		board = FindObjectOfType<Board> ();
		SetGameType();
		SetupGame ();
	}

	void SetGameType()
	{
		if(board.world != null)
		{
			if (board.level < board.world.levels.Length)
			{
				if (board.world.levels[board.level] != null)
				{
					requirements = board.world.levels[board.level].endGameRequirements;
				}
			}
		}
	}

	void SetupGame(){
		currentCounterValue = requirements.counterValue;
		if (requirements.gameType == GameType.Moves) {
			movesLabel.SetActive (true);
		}
		counter.text = "" + currentCounterValue;
	}

	public void DecreaseCounterValue(){
		if (board.currentState != GameState.pause){
			currentCounterValue--;
			counter.text = "" + currentCounterValue;
		if (currentCounterValue <= 0) {
				LoseGame ();
			}
		}
	}

	public void WinGame(){
		youWinPanel.SetActive (true);
        board.currentState = GameState.win;
		counter.text = "" + currentCounterValue;
		FadePanelController fade = FindObjectOfType<FadePanelController> ();
		fade.GameOver ();
	}

	public void LoseGame(){
		tryAgainPanel.SetActive (true);
		board.currentState = GameState.lose;
		Debug.Log ("Lose");
		currentCounterValue = 0;
		counter.text = "" + currentCounterValue;
		FadePanelController fade = FindObjectOfType<FadePanelController> ();
		fade.GameOver ();
	}

	// Update is called once per frame
	void Update () {
	}
}
