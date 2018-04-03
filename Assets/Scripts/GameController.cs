﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : Photon.PunBehaviour, IPunObservable {
	#region IPunObservable implementation
	void IPunObservable.OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info)
	{
		
	}
	#endregion

	private static GameController _instance;

	public static GameController Instance {
		get {
			if (_instance == null) {
				_instance = (GameController)FindObjectOfType (typeof(GameController));
			}
			return _instance;
		}
	}

	[SerializeField]
	private float COMBO_MULTIPLIER;

	[SerializeField]
	private const float SPECIAL_BAR_MODIFIER = 0.01f;
	[SerializeField]
	private const float HEALTH_BAR_MODIFIER = 1f;

	[SerializeField]
	private GameObject blockingPanel;

	[SerializeField]
	private Text problemText;
	[SerializeField]
	private Text answerText;
	[SerializeField]
	private Button backspaceButton;
	[SerializeField]
	private Button[] numberButtons;
	[SerializeField]
	private Button[] difficultyButtons;
	[SerializeField]
	private Text comboText;
	[SerializeField]
	private Slider comboTimerSlider;
	[SerializeField]
	private GameObject specialButton;

	[SerializeField]
	private Slider ownSpecialBarSlider;
	[SerializeField]
	private Slider ownHealthBarSlider;

	[SerializeField]
	private Slider opponentSpecialBarSlider;
	[SerializeField]
	private Slider opponentHealthBarSlider;

	private KeyValuePair<string, int> problemSet;
	private int solution;
	private int difficulty;

	private int combo;
	private float comboTimer;

	private float ownSpecialGauge;
	private float ownHealthGauge;

	private float opponentSpecialGauge;
	private float opponentHealthGauge;

	public Character ownCharacter;
	public Character opponentCharacter;

	#region Gameplay related

	void Start () {
		Debug.Log ("SCRIPT STARTED");
		ownCharacter = (Character)gameObject.AddComponent (System.Type.GetType (CharacterHolder.Instance.OwnCharacterName));
		this.photonView.RPC ("assignOpponentCharacter", PhotonTargets.Others, CharacterHolder.Instance.OwnCharacterName);

		// Force portrain orientation
		// Screen.orientation = ScreenOrientation.Landscape;

		// Add onClick listener to all number buttons
		foreach (Button button in numberButtons) {
			button.onClick.AddListener (delegate {
				addNumberToAnswer (button.name [9]);
			});
		}

		// Add onClick listener to all difficulty buttons
		difficultyButtons [Difficulty.EASY].onClick.AddListener (delegate {
			changeDifficulty (Difficulty.EASY);
		});
		difficultyButtons [Difficulty.MEDIUM].onClick.AddListener (delegate {
			changeDifficulty (Difficulty.MEDIUM);
		});
		difficultyButtons [Difficulty.HARD].onClick.AddListener (delegate {
			changeDifficulty (Difficulty.HARD);
		});

		// Initialize combo & comboTimer
		combo = 0;
		comboTimerSlider.maxValue = ownCharacter.getComboTimer ();

		// Initialize own special & health
		ownSpecialGauge = 0;
		ownHealthGauge = ownCharacter.getMaxHp ();
		ownHealthBarSlider.maxValue = ownHealthGauge;
		ownHealthBarSlider.value = ownHealthGauge;

		// Generate problem
		difficulty = Difficulty.MEDIUM;
		difficultyButtons [difficulty].interactable = false;
		problemSet = ownCharacter.generateProblem (difficulty);
		problemText.text = problemSet.Key;
	}

	void Update () {
		// Update combo timer
		if (comboTimer > 0) {
			comboTimer -= Time.deltaTime;
			comboTimerSlider.value = comboTimer;
		} else {
			resetCombo ();
		}

		// Animate bars
		AnimateSlider (ownSpecialBarSlider, ownSpecialGauge, SPECIAL_BAR_MODIFIER);
		AnimateSlider (opponentSpecialBarSlider, opponentSpecialGauge, SPECIAL_BAR_MODIFIER);
		AnimateSlider (ownHealthBarSlider, ownHealthGauge, HEALTH_BAR_MODIFIER);
		AnimateSlider (opponentHealthBarSlider, opponentHealthGauge, HEALTH_BAR_MODIFIER);
	}

	void AnimateSlider (Slider slider, float gauge, float modifier) {
		if (slider.value < gauge && slider.value + modifier <= gauge) {
			slider.value += modifier;
		} else if (slider.value > gauge && slider.value - modifier >= gauge) {
			slider.value -= modifier;
		} else {
			slider.value = gauge;
		}
	}

	void addNumberToAnswer (char number) {
		if (answerText.text != "0") {
			answerText.text += number;
		} else {
			answerText.text = "" + number;
		}
	}

	public void backspace () {
		answerText.text = answerText.text.Substring (0, answerText.text.Length - 1);
		if (answerText.text == "") {
			answerText.text = "0";
		}
	}

	void generateNewProblem () {
		problemSet = ownCharacter.generateProblem (difficulty);
		problemText.text = problemSet.Key;
	}

	void changeDifficulty(int difficulty) {
		difficultyButtons [this.difficulty].interactable = true;
		this.difficulty = difficulty;
		generateNewProblem ();
		difficultyButtons [difficulty].interactable = false;
	}

	void resetCombo () {
		combo = 0;
		comboText.text = "";
		comboTimer = 0;
		comboTimerSlider.value = 0;
	}

	public void judgeAnswer() {
		if (int.Parse (answerText.text) == problemSet.Value) {
			generateNewProblem ();

			// Add combo
			++combo;
			comboText.text = "" + combo;
			comboTimer = ownCharacter.getComboTimer ();

			// Increase own special gauge
			ownSpecialGauge += ownCharacter.getSpecialBarIncrease () [difficulty];
			if (ownSpecialGauge >= 1) {
				ownSpecialGauge = 1;
				specialButton.SetActive (true);
			}

			// Decrease opponent's health
			opponentHealthGauge -= ownCharacter.getDamage () [difficulty] * (1 + combo * COMBO_MULTIPLIER);

			// Call RPC
			this.photonView.RPC ("modifyOpponentSpecialGauge", PhotonTargets.Others, ownSpecialGauge);
			this.photonView.RPC ("modifyOwnHealthGauge", PhotonTargets.Others, opponentHealthGauge);
		} else {
			resetCombo ();
		}
		answerText.text = "0";
	}

	[PunRPC]
	void modifyOpponentSpecialGauge (float specialGauge) {
		opponentSpecialGauge = specialGauge;
	}

	[PunRPC]
	void modifyOwnHealthGauge (float healthGauge) {
		ownHealthGauge = healthGauge;
	}
					
	public void useSpecial () {
		ownSpecialGauge = 0;
		specialButton.SetActive (false);
		this.photonView.RPC ("modifyOpponentSpecialGauge", PhotonTargets.Others, ownSpecialGauge);
		this.photonView.RPC ("opponentCharacter.useSpecial", PhotonTargets.Others);
	}

	public Button[] getNumberButtons() {
		return numberButtons;
	}

	[PunRPC]
	public void assignOpponentCharacter (string characterName) {
		opponentCharacter = (Character)gameObject.AddComponent (System.Type.GetType (characterName));
		opponentSpecialGauge = 0;
		opponentHealthGauge = opponentCharacter.getMaxHp ();
		opponentHealthBarSlider.maxValue = opponentHealthGauge;
		opponentHealthBarSlider.value = opponentHealthGauge;

		blockingPanel.SetActive (false);
	}
		
	#endregion

	#region others

	public override void OnLeftRoom () {
		SceneManager.LoadScene (0);
	}

	public void leaveRoom () {
		PhotonNetwork.LeaveRoom ();
	}

	public override void OnPhotonPlayerDisconnected (PhotonPlayer other) {
		Debug.Log ("OnPhotonPlayerDisconnected()");
		leaveRoom ();
	}
		
	#endregion

}
