using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState{
	wait,
	move,
	win,
	lose,
	pause
}

public enum TileKind{
	Breakable,
	Blank,
	Lock,
	Ice,
	Normal
}

[System.Serializable]
public class MatchType
{
	public int type;
	public string color;
}

[System.Serializable]
public class TileType{
	public int x;
	public int y;
	public TileKind tileKind;
}

public class Board : MonoBehaviour {

	[Header("Scriptable Objects Stuff")]
	public World world;
	public int level;

	public GameState currentState = GameState.move;
	[Header("Board Dimensions")]
	public int width;
	public int height;
	public int offSet;

	[Header("Prefabs")]
	public GameObject tilePrefab;
	public GameObject breakableTilePrefab;
	public GameObject lockedTilePrefab;
	public GameObject iceTilePrefab;
	public GameObject[] dots;
	public GameObject destroyEffect;

	[Header("Layout")]
	public TileType[] boardLayout;
	private bool[,] blankSpaces;
	private BackgroundTile[,] breakableTiles;
	public BackgroundTile[,] lockedTiles;
	private BackgroundTile[,] iceTiles;
	public GameObject[,] allDots;

	[Header("Match Stuff")]
	public MatchType matchType;
	public Dot currentDot;
	private FindMatches findMatches;
	public int basePieceValue = 20;
	private int streakValue = 1;
	private ScoreManager scoreManager;
	private SoundManager soundManager;
	private GoalManager goalManager;
	public float refillDelay = 0.5f;
	public int [] scoreGoals;

	private void Awake()
	{
		if(PlayerPrefs.HasKey("Current Level")) 
		{
			level = PlayerPrefs.GetInt("Current Level");
		}
		if(world != null)
		{
			if (level < world.levels.Length)
			{
				if (world.levels[level] != null)
				{
					width = world.levels[level].width;
					height = world.levels[level].height;
					dots = world.levels[level].dots;
					scoreGoals = world.levels[level].scoreGoals;
					boardLayout = world.levels[level].boardLayout;
				}
			}
		}
	}

	// Use this for initialization
	void Start () {
		goalManager = FindObjectOfType<GoalManager>();
		soundManager = FindObjectOfType<SoundManager> ();
		scoreManager = FindObjectOfType<ScoreManager> ();
		breakableTiles = new BackgroundTile[width, height];
		lockedTiles= new BackgroundTile[width, height];
		iceTiles= new BackgroundTile[width, height];
		findMatches = FindObjectOfType<FindMatches>();
		blankSpaces = new bool[width, height];
		allDots = new GameObject[width, height];
		SetUp ();
		currentState = GameState.pause;
	}

	public void GenerateBlankSpaces(){
		for (int i = 0; i < boardLayout.Length; i++) {
			if (boardLayout [i].x <= width - 1 && boardLayout [i].y <= height - 1) {
				if (boardLayout [i].tileKind == TileKind.Blank) {
					blankSpaces [boardLayout [i].x, boardLayout [i].y] = true;
				}
			}
		}
	}

	public void GenerateBreakableTiles(){
		//просмотр всех фишек
		for (int i = 0; i < boardLayout.Length; i++) {
			//если фишка желейка 
			if (boardLayout [i].x <= width - 1 && boardLayout [i].y <= height - 1) {
				if (boardLayout [i].tileKind == TileKind.Breakable) {
					//создать желейку-фишку на этом месте
					Vector2 tempPosition = new Vector2 (boardLayout [i].x, boardLayout [i].y);
					GameObject tile = Instantiate (breakableTilePrefab, tempPosition, Quaternion.identity);
					breakableTiles [boardLayout [i].x, boardLayout [i].y] = tile.GetComponent<BackgroundTile> ();
				}
			}
		}
	}

	private void GenerateLockTiles()
	{
        for (int i = 0; i < boardLayout.Length; i++)
        {
            //если фишка замок
            if (boardLayout[i].x <= width - 1 && boardLayout[i].y <= height - 1)
            {
                if (boardLayout[i].tileKind == TileKind.Lock)
                {
                    //создать желейку-фишку на этом месте
                    Vector2 tempPosition = new Vector2(boardLayout[i].x, boardLayout[i].y);
                    GameObject tile = Instantiate(lockedTilePrefab, tempPosition, Quaternion.identity);
                    lockedTiles[boardLayout[i].x, boardLayout[i].y] = tile.GetComponent<BackgroundTile>();
                }
            }
        }
    }

    private void GenerateIceTiles()
    {
        for (int i = 0; i < boardLayout.Length; i++)
        {
            //если фишка замок
            if (boardLayout[i].x <= width - 1 && boardLayout[i].y <= height - 1)
            {
                if (boardLayout[i].tileKind == TileKind.Ice)
                {
                    //создать желейку-фишку на этом месте
                    Vector2 tempPosition = new Vector2(boardLayout[i].x, boardLayout[i].y);
                    GameObject tile = Instantiate(iceTilePrefab, tempPosition, Quaternion.identity);
                    iceTiles[boardLayout[i].x, boardLayout[i].y] = tile.GetComponent<BackgroundTile>();
                }
            }
        }
    }

    private void SetUp(){
		GenerateBlankSpaces ();
		GenerateBreakableTiles ();
		GenerateLockTiles ();
		GenerateIceTiles ();
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (!blankSpaces [i, j] && !iceTiles[i, j]) {
					Vector2 tempPosition = new Vector2 (i, j + offSet);
					Vector2 tilePostion = new Vector2 (i, j);
					GameObject backgroundTile = Instantiate (tilePrefab, tilePostion, Quaternion.identity) as GameObject;
					backgroundTile.transform.parent = this.transform;
					backgroundTile.name = "( " + i + ", " + j +	" )";
					int dotToUse = Random.Range (0, dots.Length);

					int maxIterations = 0;
					while (MatchesAt (i, j, dots [dotToUse]) && maxIterations < 100) {
						dotToUse = Random.Range (0, dots.Length);
						maxIterations++;
						//Debug.Log (maxIterations);
					}
					maxIterations = 0;

					GameObject dot = Instantiate (dots [dotToUse], tempPosition, Quaternion.identity);
					dot.GetComponent<Dot> ().row = j;
					dot.GetComponent<Dot> ().column = i;
					dot.transform.parent = this.transform;
					dot.name = "( " + i + ", " + j + " )";
					allDots [i, j] = dot;
				}
			}
		}
	}

	private bool MatchesAt(int column, int row, GameObject piece){
		if (column > 1 && row > 1) {
			if (allDots [column - 1, row] != null && allDots [column - 2, row] != null) {
				if (allDots [column - 1, row].tag == piece.tag && allDots [column - 2, row].tag == piece.tag) {
					return true;
				}
			}
			if (allDots [column, row - 1] != null && allDots [column, row - 2] != null) {
				if (allDots [column, row - 1].tag == piece.tag && allDots [column, row - 2].tag == piece.tag) {
					return true;
				}
			}

		} else if (column <= 1 || row <= 1) {
			if (row > 1) {
				if (allDots [column, row - 1] != null && allDots [column, row - 2] != null) {
					if (allDots [column, row - 1].tag == piece.tag && allDots [column, row - 2].tag == piece.tag) {
						return true;
					}
				}
			}
			if (column > 1) {
				if (allDots [column - 1, row] != null && allDots [column - 2, row] != null) {
					
					if (allDots [column - 1, row].tag == piece.tag && allDots [column - 2, row].tag == piece.tag) {
						return true;
					}
				}
			}
		}

		return false;
	}

	private MatchType ColumnOrRow(){
		List<GameObject> matchCopy = findMatches.currentMatches as List<GameObject>;

		matchType.type = 0;
		matchType.color = "";

		for(int i = 0; i < matchCopy.Count; i++)
		{
			Dot thisDot = matchCopy[i].GetComponent<Dot>();
			string color = matchCopy[i].tag;
			int column = thisDot.column;
			int row = thisDot.row;
			int columnMatch = 0;
			int rowMatch = 0;
			for(int j = 0; j < matchCopy.Count; j++)
			{
				Dot nextDot = matchCopy[j].GetComponent<Dot>();
				if(nextDot == thisDot)
				{
					continue;
				}
				if(nextDot.column == thisDot.column && nextDot.tag == color)
				{
					columnMatch++;
				}
                if (nextDot.row == thisDot.row && nextDot.tag == color)
                {
                    rowMatch++;
                }
            }
			if(columnMatch == 4 || rowMatch == 4)
			{
				matchType.type = 1;
				matchType.color = color;
				return matchType;
			}
			else if(columnMatch == 2 && rowMatch == 2)
			{
                matchType.type = 2;
                matchType.color = color;
                return matchType;
            }
			else if(columnMatch == 3 || rowMatch == 3)
			{
                matchType.type = 3;
                matchType.color = color;
                return matchType;
            }
		}
        matchType.type = 0;
        matchType.color = "";
        return matchType;

    }

	private void CheckToMakeBombs(){
		if(findMatches.currentMatches.Count > 3)
		{
			MatchType typeOfMatch = ColumnOrRow();
			if (typeOfMatch.type == 1)
			{
				//создание цветной бомбы
				if (currentDot != null && currentDot.isMatched && currentDot.tag == typeOfMatch.color)
				{
					currentDot.isMatched = false;
					currentDot.MakeColorBomb();
				}
				else
				{
					if (currentDot.otherDot != null)
					{
						Dot otherDot = currentDot.otherDot.GetComponent<Dot>();
						if (otherDot.isMatched && otherDot.tag == typeOfMatch.color)
						{
							otherDot.isMatched = false;
							otherDot.MakeColorBomb();
						}
					}
				}
			}
			else if(typeOfMatch.type == 2)
			{
				//создание ракеты
				if (currentDot != null && currentDot.isMatched && currentDot.tag == typeOfMatch.color)
				{
					currentDot.isMatched = false;
					currentDot.MakeAdjacentBomb();
				}
				else if(currentDot.otherDot != null)
				{
					Dot otherDot = currentDot.otherDot.GetComponent<Dot>();
					if (otherDot.isMatched && otherDot.tag == typeOfMatch.color)
					{
						otherDot.isMatched = false;
						otherDot.MakeAdjacentBomb();
					}
				}
            }
			else if(typeOfMatch.type == 3)
			{
                findMatches.CheckBombs(matchType);
            }
		}
	}

	public void BombRow(int row)
	{
		for(int i = 0; i < width; i++)
		{
			if (iceTiles[i, row])
			{
				iceTiles[i, row].TakeDamage(1);
                if (iceTiles[i, row].hitPoints <= 0)
				{
                   iceTiles[i, row] = null;
                }
            }
		}
	}

    public void BombColumn(int column)
    {
        for (int i = 0; i < width; i++)
        {
           if (iceTiles[column, i])
           {
              iceTiles[column, i].TakeDamage(1);
              if (iceTiles[column, i].hitPoints <= 0)
              {
                 iceTiles[column, i] = null;
              }
           }
        }
    }


    private void DestroyMatchesAt(int column, int row){
		if (allDots [column, row].GetComponent<Dot> ().isMatched) {

			//нужно ли фишку сломать?
			if (breakableTiles [column, row] != null) {
				breakableTiles [column, row].TakeDamage (1);
				if (breakableTiles [column, row].hitPoints <= 0) {
					breakableTiles [column, row] = null;
				}
			}

            if (lockedTiles[column, row] != null)
            {
                lockedTiles[column, row].TakeDamage(1);
                if (lockedTiles[column, row].hitPoints <= 0)
                {
                    lockedTiles[column, row] = null;
                }
            }
			DamageIce(column, row);

            if (goalManager != null) {
				goalManager.CompareGoal (allDots[column, row].tag.ToString());
				goalManager.UpdateGoals ();
			}

			if (soundManager != null) {
				soundManager.PlayRandomDestroyNoise ();
			}
			GameObject particle = Instantiate (destroyEffect, allDots[column, row].transform.position, Quaternion.identity);
			Destroy (particle, .5f);
			Destroy (allDots [column, row]);
			scoreManager.IncreaseScore (basePieceValue * streakValue);
			allDots [column, row] = null;
		}
	}

	public void DestroyMatches(){
        if (findMatches.currentMatches.Count >= 4)
        {
            CheckToMakeBombs();
        }
        findMatches.currentMatches.Clear();
        for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (allDots [i, j] != null) {
					DestroyMatchesAt (i, j);
				}
			}
		}
		StartCoroutine (DecreaseRowCo2());
	}

	private void DamageIce(int column, int row)
	{
		if(column > 0)
		{
			if (iceTiles[column -1, row])
			{
				iceTiles[column - 1, row].TakeDamage(1);
                if (iceTiles[column - 1, row].hitPoints <= 0)
                {
                    iceTiles[column - 1, row] = null;
                }
            }
		}
        if (column < width - 1)
        {
            if (iceTiles[column + 1, row])
            {
                iceTiles[column + 1, row].TakeDamage(1);
                if (iceTiles[column + 1, row].hitPoints <= 0)
                {
                    iceTiles[column + 1, row] = null;
                }
            }
        }
        if (row > 0)
        {
            if (iceTiles[column, row - 1])
            {
                iceTiles[column, row - 1].TakeDamage(1);
                if (iceTiles[column, row - 1].hitPoints <= 0)
                {
                    iceTiles[column, row - 1] = null;
                }
            }
        }
        if (row < height - 1)
        {
            if (iceTiles[column, row + 1])
            {
                iceTiles[column, row + 1].TakeDamage(1);
                if (iceTiles[column, row + 1].hitPoints <= 0)
                {
                    iceTiles[column, row + 1] = null;
                }
            }
        }
    }
	private IEnumerator DecreaseRowCo2(){
		for(int i= 0; i < width; i++){
			for (int j = 0; j < height; j++) {
				//если текущая точка не пустая и незаполнена
				if (!blankSpaces [i, j] && allDots [i, j] == null && !iceTiles[i, j]) {
					//закрепить место сверху 
					for (int k = j + 1; k < height; k++) {
						//если точка найдена
						if (allDots [i, k] != null) {
							//тогда передвигаю эту фишку в пустое место
							allDots[i, k].GetComponent<Dot>().row = j;
							allDots [i, k] = null;
							break;
						}
					}
				}
			}
		}
		yield return new WaitForSeconds (refillDelay * 0.5f);
		StartCoroutine (FillBoardCo ());
	}

	private IEnumerator DecreaseRowCo(){
		int nullCount = 0;
		for(int i = 0; i < width; i++){
			for (int j = 0; j < height; j++) {
				if (allDots [i, j] == null) {
					nullCount++;
				}else if(nullCount > 0){
					allDots [i, j].GetComponent<Dot> ().row -= nullCount;
					allDots [i, j] = null;
				}
			}
			nullCount = 0;
		}
		yield return new WaitForSeconds (refillDelay * 0.5f);
		StartCoroutine (FillBoardCo());
	}

	private void RefillBoard(){
		for(int i = 0; i < width; i++){
			for (int j = 0; j < height; j++){
				if (allDots [i, j] == null && !blankSpaces[i, j] && !iceTiles[i, j]) {
					Vector2 tempPosition = new Vector2(i, j + offSet);
					int dotToUse = Random.Range (0, dots.Length);
					int maxIterations = 0;

					while (MatchesAt (i, j, dots [dotToUse]) && maxIterations < 100) {
						maxIterations++;
						dotToUse = Random.Range (0, dots.Length);
					}
					maxIterations = 0;

					GameObject piece = Instantiate (dots [dotToUse], tempPosition, Quaternion.identity);
					allDots [i, j] = piece;
					piece.GetComponent<Dot> ().row = j;
					piece.GetComponent<Dot> ().column = i;
				}
			}
		}
	}

	private bool MatchesOnBoard(){
		findMatches.FindAllMatches();
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (allDots [i, j] != null) {
					if (allDots [i, j].GetComponent<Dot> ().isMatched) {
						return true;
					}
				}
			}
		}
		return false;
	}

	private IEnumerator FillBoardCo(){
		yield return new WaitForSeconds (refillDelay);
        RefillBoard();
        yield return new WaitForSeconds(refillDelay);
        while (MatchesOnBoard ()) {
			streakValue++;
			DestroyMatches ();
			yield break;
		}
		currentDot = null;

		if (IsDeadlocked ()) {
			ShiffleBoard ();
			Debug.Log("Deadlocked!!");
		}
		yield return new WaitForSeconds (refillDelay);
        System.GC.Collect();
		if (currentState != GameState.pause)
			currentState = GameState.move;
		streakValue = 1;

	}
		
	private void SwitchPieces(int column, int row, Vector2 direction){
		if (allDots[column + (int)direction.x, row + (int)direction.y] != null)
		{
			//беру вторую фишку и сохраняю это как холдер
			//if(!blankSpaces[column + (int)direction.x, row + (int)direction.y]){
			GameObject holder = allDots[column + (int)direction.x, row + (int)direction.y] as GameObject;
			allDots[column + (int)direction.x, row + (int)direction.y] = allDots[column, row];
			allDots[column, row] = holder;
		}
		//}
	}

	private bool CheckForMatches(){
		for(int i = 0; i < width; i++){
			for (int j = 0; j < height; j++) {
				if (allDots [i, j] != null) {
					if (i < width - 2) {
						if (allDots [i + 1, j] != null && allDots [i + 2, j] != null) {
							if (allDots [i + 1, j].tag == allDots [i, j].tag
							   && allDots [i + 2, j].tag == allDots [i, j].tag) {
								return true;
							}
						}
					}
					if (j < height - 2) {

						if (allDots [i, j + 1] != null && allDots [i, j + 2] != null) {
							if (allDots [i, j + 1].tag == allDots [i, j].tag
							   && allDots [i, j + 2].tag == allDots [i, j].tag) {
								return true;
							}
						}
					}
				}
			}
		}
		return false;
	}

	public bool SwitchAndCheck(int column, int row, Vector2 direction){
		SwitchPieces (column, row, direction);
		if (CheckForMatches ()) {
			SwitchPieces(column, row, direction);
			return true;
		}
		SwitchPieces(column, row, direction);
		return false;
	}

	private bool IsDeadlocked(){
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (allDots [i, j] != null) {
					if (i < width - 1) {
						if (SwitchAndCheck (i, j, Vector2.right)) {
							return false;
						}
					}
					if (j < height - 1) {
						if(SwitchAndCheck(i, j, Vector2.up)){
							return false;
						}
					}
				}
			}
		}
		return true;
	}

	private IEnumerator ShiffleBoard(){
		yield return new WaitForSeconds(0.5f);
		List<GameObject> newBoard = new List<GameObject> ();
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (allDots [i, j] != null) {
					newBoard.Add(allDots[i, j]);
				}
			}
		}
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (!blankSpaces [i, j] && !iceTiles[i, j]) {
					int pieceToUse = Random.Range (0, newBoard.Count);
					int maxIterations = 0;
					while (MatchesAt (i, j, newBoard[pieceToUse]) && maxIterations < 100) {
						pieceToUse = Random.Range (0, newBoard.Count);
						maxIterations++;
						//Debug.Log (maxIterations);
					}
					Dot piece = newBoard [pieceToUse].GetComponent<Dot> ();
					maxIterations = 0;
					piece.column = i;
					piece.row = j;
					allDots [i, j] = newBoard [pieceToUse];
					newBoard.Remove (newBoard [pieceToUse]);
				}
			}
		}
		if (IsDeadlocked ()) {
			StartCoroutine(ShiffleBoard());
		}
	}

}
