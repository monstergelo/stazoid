﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterHolder : MonoBehaviour {

	private string ownCharacterName;

	private static CharacterHolder instance;
	public static CharacterHolder Instance {
		get {
			if (instance == null) {
				instance = FindObjectOfType <CharacterHolder> ();
			}
			return instance;
		}
	}

	void Start () {
		DontDestroyOnLoad (this.gameObject);
	}

	public string OwnCharacterName {
		get {
			return ownCharacterName;
		}
		set {
			ownCharacterName = value;
		}
	}

}
