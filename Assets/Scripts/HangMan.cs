//using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemToGuess
{
    public string NameToGuess;
    public float NameGuessedQuickestTimeInMS;
    public int LowestNumberOfAttemptsToSolve;
    public string HintText;
    public string FullInformationText;
    public int OriginalIndex;
}

public class HangMan : MonoBehaviour
{
    public TMP_Text phraseToGuessText;
    public TMP_Text lettersToDisplayText;
    public TMP_Text AttemptsTakenText;
    public TMP_Text TimeRemainingText;
    public TMP_Text HintText;
    public TMP_Text InformationText;
    public TMP_Text LevelStartCountdownText;
    public TMP_Text LevelLostText;

    public Image TimeRemainingImage;

    public Canvas TitleCanvas;
    public Canvas GameCanvas;



    public List<ItemToGuess> OriginalItemsToGuessList = new List<ItemToGuess>()
    {
        new ItemToGuess()
        {
            NameToGuess = "Winston Churchill",
            HintText = "Son of Lord Randolph and Jennie",
            FullInformationText = "Sir Winston Leonard Spencer Churchill (30 November 1874 � 24 January 1965) was a British statesman, soldier, and writer who twice served as Prime Minister of the United Kingdom, from 1940 to 1945 during the Second World War, and again from 1951 to 1955."
        },
        new ItemToGuess()
        {
            NameToGuess = "Hitler",
            HintText = "20 April 1889",
            FullInformationText = "Adolf Hitler (20 April 1889 � 30 April 1945) was an Austrian-born German politician who was the dictator of Germany from 1933 until his suicide in 1945."
        },
        new ItemToGuess()
        {
            NameToGuess = "Musolini",
            HintText = "Leader Of Italy In WW2",
            FullInformationText = "He ruled Italy from 1922–1925 as Prime Minister, and from 1925–1943 as il Duce, the Fascist dictator"
        }
    };


    public List<ItemToGuess> ItemsStillToGuessList = new List<ItemToGuess>();
    public List<ItemToGuess> ItemsCorrectlyGuessedList = new List<ItemToGuess>();

    public string PhraseToGuess;
    public string UpperCasePhrase;
    public string PhraseToDisplay;
    public string LettersToFind;
    public string LettersSearchedFor;
    public string SearchedLettersToDisplay;
    public int AttemptsTakenToCompletePhrase;
    public float LevelTimerMS;
    public float TimeTakenToCompletePhrase;
    public int LowestNumberOfGuessesToSolve;
    public int IncorrectGuessTimePenalty;

    public int LevelCompleteBonusTime = 10;
    public float DelayBeforeShowingNextLevel = 2;
    public float DelayWhilstShowingHint = 3;

    public float DelayGameTimer;

    public int GameStatus;

    public float MaxLevelTime = 30;

    private const int GameStatus_ShowTitles = 1;
    private const int GameStatus_ShowHighScoreTables = 2;
    private const int GameStatus_ShowHint = 4;
    private const int GameStatus_LevelLost = 8;
    private const int GameStatus_LevelWon = 16;
    private const int GameStatus_GameWon = 32;
    private const int GameStatus_PlayingGame = 64;
    private const int GameStatus_WaitForDelay = 128;

    bool PerfectLevel = false;  // Completed the level without any incorrect guesses!

    Color Default321Colour;
    Color DefaultFailedColour;
    Color DefaultWinColour = Color.magenta;

    System.Random r;


    public int LastLevelPlayedIndex;
    // Start is called before the first frame update
    void Start()
    {
        r = new System.Random();

        for (int i = 0; i < OriginalItemsToGuessList.Count; i++)
        {
            OriginalItemsToGuessList[i].NameGuessedQuickestTimeInMS = MaxLevelTime;
            OriginalItemsToGuessList[i].OriginalIndex = i;  // Used to setting the best completed time
            OriginalItemsToGuessList[i].LowestNumberOfAttemptsToSolve = 26; // Can't be any higher..26 letters in the alphabet
        }

        Default321Colour = LevelStartCountdownText.color;
        DefaultFailedColour = LevelLostText.color;

        // Called at the start of each game (NOT the level within game)
        // Sets player score to 0 and initialises the phrase list parameters
        // To be called when player starts game after exiting title screen

        GameStatus = GameStatus_ShowTitles;
    }

    void InitGame()
    {
        // Initialise all lists.
        // Used to ensure that a random phrase is only picked once if you guess it correctly.

        ItemsStillToGuessList = new List<ItemToGuess>(OriginalItemsToGuessList);
        ItemsCorrectlyGuessedList = new List<ItemToGuess>();

        LastLevelPlayedIndex = GetLevelIndexToPlay();
        InitLevel(ItemsStillToGuessList[LastLevelPlayedIndex], MaxLevelTime);   // Phrase to search for and number of wrong attempts allowed
    }

    int GetLevelIndexToPlay()
    {
        int randomNumber = r.Next(0, ItemsStillToGuessList.Count);
        return randomNumber;
    }

    // Update is called once per frame
    void Update()
    {
        DisplayScreen();    // Update screen display
        if (DelayGameTimer > 0)
        {
            DelayGameTimer -= Time.deltaTime;
            if (DelayGameTimer <= 0)
            {
                if (GameStatus == GameStatus_ShowHint)
                {
                    GameStatus = GameStatus_PlayingGame;
                }
                else if (GameStatus == GameStatus_LevelLost)
                {
                    GameStatus = GameStatus_ShowTitles; // Go back to titles
                }
            }
            return;
        }
        if (GameStatus == GameStatus_ShowTitles)
        {
            string letter = Input.inputString;
            if (letter == " ")
                InitGame();
        }
        else if (GameStatus == GameStatus_PlayingGame)
        {
            DecreaseLevelTimer(Time.deltaTime);

            TimeTakenToCompletePhrase += Time.deltaTime;

            UpdateHangManGame();

            if (CheckForLevelLost() == true)
            {
                GameStatus = GameStatus_LevelLost;
                DelayGameTimer = DelayBeforeShowingNextLevel;
            }
            else if (CheckForLevelWon() == true)
            {
                GameStatus = GameStatus_LevelWon;
                DelayGameTimer = DelayBeforeShowingNextLevel;
            }
        }
        else
        {
            if (GameStatus == GameStatus_LevelLost)
            {
                GameStatus = GameStatus_WaitForDelay;
                LevelTimerMS = MaxLevelTime;
            }
            else if (GameStatus == GameStatus_LevelWon)
            {
                LevelTimerMS += LevelCompleteBonusTime; // Add n seconds to the current time
                if (LevelTimerMS > MaxLevelTime)
                {
                    // Player completed level so quickly that they've got more than MaxLevelTime seconds remaining
                    // Limit this to MaxLevelTime
                    // We could give another bonus here.
                    LevelTimerMS = MaxLevelTime;
                }

                int originalIndex = ItemsStillToGuessList[LastLevelPlayedIndex].OriginalIndex;
                if (TimeTakenToCompletePhrase < OriginalItemsToGuessList[originalIndex].NameGuessedQuickestTimeInMS)
                {
                    OriginalItemsToGuessList[originalIndex].NameGuessedQuickestTimeInMS = TimeTakenToCompletePhrase;
                }

                if (AttemptsTakenToCompletePhrase < OriginalItemsToGuessList[originalIndex].LowestNumberOfAttemptsToSolve)
                {
                    OriginalItemsToGuessList[originalIndex].LowestNumberOfAttemptsToSolve = AttemptsTakenToCompletePhrase;
                }
                if (AttemptsTakenToCompletePhrase == 0)
                {
                    PerfectLevel = true;
                }

                // Level completed. So remove this from the available levels to still play
                // (We don't want to randomly select it again)
                ItemsCorrectlyGuessedList.Add(ItemsStillToGuessList[LastLevelPlayedIndex]);
                ItemsStillToGuessList.RemoveAt(LastLevelPlayedIndex);
                if (ItemsStillToGuessList.Count > 0)
                {
                    LastLevelPlayedIndex = GetLevelIndexToPlay();
                }
                else
                {
                    GameStatus = GameStatus_ShowTitles;    // All levels are complete - so is the game.
                    return;
                }
            }
            if (GameStatus != GameStatus_GameWon)
            {
                InitLevel(ItemsStillToGuessList[LastLevelPlayedIndex], LevelTimerMS);

            }

        }
    }


    public void UpdateHangManGame()
    {
        string letter = GetLetterFromPlayer();      // Wait for the player to type a letter
        if (letter != "")   // Has the player typed anything valid? i.e. A letter A-Z that hasn't been used before?
        {
            UpdateLettersSearchedFor(letter);   // Add this letter to the letters which have now been searched for
            SearchForLetterInPhrase(letter);    // See if this letter is within the phrase. If so, update phrase. Otherwise, decrease the remaining attempt count
        }
    }

    /*
     * Add the new letter to the string of previously searched for letters
     * Replace the letter with a * for displaying which letters have already been searched for.
     */
    void UpdateLettersSearchedFor(string letter)
    {
        LettersSearchedFor += letter;
        SearchedLettersToDisplay = SearchedLettersToDisplay.Replace(letter, "*");
    }


    /*
     * Check if the level is won
     */
    bool CheckForLevelWon()
    {
        if (LettersToFind.Length == 0)  // No letters still to find = You've won
        {
            return true;
        }
        return false;
    }

    /*
     * Check if the level is lost
     */
    bool CheckForLevelLost()
    {
        if (LevelTimerMS <= 0)
        {
            return true;    // out of time
        }
        return false;
    }

    /*
     * Search for a letter in the phrase
     * 
     * If found:
     * a) Update the string that is displayed on screen (replace *'s with the letters)
     * b) Remove the letter from the remaining letters to find.
     * 
     * If not found:
     * a) Reduce remaining attempts
     */
    void SearchForLetterInPhrase(string letter)
    {
        if (LettersToFind.Contains(letter) == true) // Is the letter in the phrase that we are solving?
        {
            UpdatePhraseToDisplay(letter);   // Yes. Show letter in the string that is displayed on screen
            LettersToFind = LettersToFind.Replace(letter, "");  // Remove letter from the remaining letters to find
        }
        else
        {
            AttemptsTakenToCompletePhrase++;    // Letter isn't in the phrase. Increase the counter for the number of attempts made
            DecreaseLevelTimer(IncorrectGuessTimePenalty); // Remove 3 seconds from the timer
        }
    }

    /*
     * Show the letter in the phrase, replacing * with the letter where necessary
     */
    void UpdatePhraseToDisplay(string letterToShow)
    {
        char letterChar = letterToShow[0];  // letterChar is now the first letter in the string...

        string newString = "";  // Start with an empty string..

        for (int i = 0; i < PhraseToDisplay.Length; i++)    // We loop around each letter in the phrase
        {
            char letterFromPhrase = UpperCasePhrase[i]; // eg. when i is 0, letterFromPhrase was be "W" (first letter in the phrase for "Winston Churchill")
            if (letterFromPhrase != letterChar)   // Does the letter in the phrase at this position match
            {
                newString += PhraseToDisplay[i];    // No. So use whatever letter is currently in the phrase to display
            }
            else
            {
                newString += UpperCasePhrase[i];    // Yes. So use the actual letter.
            }
        }
        PhraseToDisplay = newString;    // This is now the new string to display on screen, with the correct *'s replaced with letters
    }


    /*
     * Get input from the player
     */
    string GetLetterFromPlayer()
    {
        string letter = Input.inputString;
        if (letter.Length == 0)
            return "";

        letter = letter.ToUpper();  // Convert the text that the player typed to upper case. Makes all future code easier to write as we don't need to worry about lower or upper cases

        if (letter.Length > 1)  // Did the player type more than one character?
            return "";          // Yes... We will ignore this then. We only want single characters.

        if (LettersSearchedFor.Contains(letter) == true)    // Has the player typed a character that they've already searched for?
        {
            return "";  // Yes. So we ignore this
        }

        char letterChar = letter[0];

        if (letterChar >= 'A' && letterChar <= 'Z') // Check that player has typed a valid letter A-Z
        {
            return letter;      // We have a valid character to search for
        }
        else
        {
            return "";  // Player typed an invalid character (eg.. a number or other symbol)
        }
    }

    /*
     * Display the information on screen
     */
    void DisplayScreen()
    {
        if (GameStatus == GameStatus_ShowTitles)
        {
            TitleCanvas.enabled = true;
            GameCanvas.enabled = false;
        }
        else
        {
            TitleCanvas.enabled = false;
            GameCanvas.enabled = true;


            phraseToGuessText.text = PhraseToDisplay;
            lettersToDisplayText.text = SearchedLettersToDisplay;
            AttemptsTakenText.text = "Attempts:" + AttemptsTakenToCompletePhrase.ToString() + " (Best:" + LowestNumberOfGuessesToSolve.ToString() + ")";
            TimeRemainingText.text = "Time:" + LevelTimerMS.ToString("00.00");

            if ((GameStatus & GameStatus_LevelWon) == 0)
            {
                HintText.enabled = true;
                InformationText.enabled = false;
                if ((GameStatus & GameStatus_LevelLost) != 0)
                {
                    LevelLostText.enabled = true;
                    IncreaseWinOrLoseTextSizeAndFadeout("Failed!", LevelLostText, 300, DelayGameTimer, DelayBeforeShowingNextLevel, DefaultFailedColour);
                }
                else
                {
                    LevelLostText.enabled = false;
                }
            }
            else
            {
                IncreaseWinOrLoseTextSizeAndFadeout("Complete!", LevelLostText, 300, DelayGameTimer, DelayBeforeShowingNextLevel, DefaultWinColour);
                LevelLostText.enabled = true;

                HintText.enabled = false;
                InformationText.enabled = true;
            }

            if (GameStatus == GameStatus_ShowHint)
            {
                LevelStartCountdownText.enabled = true;

                int justSeconds = (int)DelayGameTimer;
                if (justSeconds == 0)
                    LevelStartCountdownText.text = "Go!";
                else
                    LevelStartCountdownText.text = justSeconds.ToString();

                Increase321TextSizeAndFadeout(LevelStartCountdownText, 300, DelayGameTimer, Default321Colour);
            }
            else
                LevelStartCountdownText.enabled = false;

            bool displayGameInfo = true;
            if (GameStatus != GameStatus_PlayingGame)
            {
                displayGameInfo = false;
            }

            lettersToDisplayText.enabled = displayGameInfo;
            AttemptsTakenText.enabled = displayGameInfo;

            float scaledTimerValue = LevelTimerMS / MaxLevelTime;
            if (scaledTimerValue < 0)
                scaledTimerValue = 0;
            else if (scaledTimerValue > 1)
                scaledTimerValue = 1;
            TimeRemainingImage.fillAmount = scaledTimerValue;
        }
    }

    void IncreaseWinOrLoseTextSizeAndFadeout(string textToShow, TMP_Text text, float maxTextSize, float currentTime, float maxTime, Color originalColor)
    {
        float normalizedFraction = currentTime / maxTime;
        text.fontSize = 64 + (maxTextSize * (1 - normalizedFraction));

        originalColor.a = normalizedFraction;
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a);
        text.text = textToShow;
    }


    void Increase321TextSizeAndFadeout(TMP_Text text, float maxTextSize, float currentTime, Color originalColor)
    {
        int justSeconds = (int)currentTime;
        float timeFraction = currentTime - justSeconds;
        text.fontSize = 64 + (maxTextSize - (maxTextSize * timeFraction));

        float normalizedFraction = timeFraction / 1.0f;
        originalColor.a = normalizedFraction;
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a);

    }


    /*
     * Initialise game variables for this level before the game starts
     */
    void InitLevel(ItemToGuess itemToGuess, float timeToPlayInSeconds)
    {
        PerfectLevel = false;

        LevelTimerMS = timeToPlayInSeconds;  // Put n seconds on the clock..
        TimeTakenToCompletePhrase = 0;
        AttemptsTakenToCompletePhrase = 0;
        PhraseToGuess = itemToGuess.NameToGuess;

        GameStatus = GameStatus_ShowHint;
        DelayGameTimer = DelayWhilstShowingHint;

        int originalIndex = itemToGuess.OriginalIndex;
        LowestNumberOfGuessesToSolve = OriginalItemsToGuessList[originalIndex].LowestNumberOfAttemptsToSolve;

        HintText.text = "Hint:" + itemToGuess.HintText;
        InformationText.text = itemToGuess.FullInformationText;

        UpperCasePhrase = PhraseToGuess.ToUpper();  // Upper case version of the phrase
        PhraseToDisplay = GeneratePhraseToDisplay(UpperCasePhrase); // Version to display with *'s initially covering the phrase
        LettersToFind = GetAllUniqueLetters(UpperCasePhrase);       // A string containing all of the unique letters in the phrase
        LettersToFind = LettersToFind.Replace(" ", ""); // Remove spaces between words so that there are only letters to find in the phrase
        LettersSearchedFor = "";    // Clear the string containing previously searched for letters
        SearchedLettersToDisplay = "ABCDEFGHIJLKLMNOPQRSTUVWXYZ";   // Set the initial display of the searched letters. These are replaced with * as the player searches for them
    }

    /*
     * Find all unique letters within a string
     * So, for Winston Churchill, it should find: "WINSTOCHURL"
     */
    string GetAllUniqueLetters(string originalString)
    {
        string newString = "";  // This will contain the unique characters

        for (int i = 0; i < originalString.Length; i++) // Loop through the original text ("WINSTONCHURCHILL")
        {
            char c = originalString[i];
            if (newString.Contains(c) == false) // Does our new string contain this letter yet?
            {
                newString += c; // No, so add it to the string.
            }
        }
        return newString;
    }


    /*
     * Generate the phrase to display on screen.
     * Initially, you want each letter in each word to be replaced with *
     */
    string GeneratePhraseToDisplay(string originalString)
    {
        string newString = "";
        for (int i = 0; i < originalString.Length; i++) // Get each character from the original phrase ("Winston Churchill")
        {
            if (originalString[i] == ' ')   // Is the character a space?
            {
                newString += " ";   // Yes. So we add a space into the phrase to be displayed
            }
            else
            {
                newString += "*";   // No. We add a * to the phrase to be displayed
            }
        }
        return newString;
    }

    public void DecreaseLevelTimer(float timeInMilliseconds)
    {
        LevelTimerMS -= timeInMilliseconds;
        if (LevelTimerMS <= 0)
        {
            LevelTimerMS = 0;   // Just so it doesn't display negative values in the time bar
        }
    }
}
