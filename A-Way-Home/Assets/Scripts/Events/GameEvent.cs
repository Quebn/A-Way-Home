using UnityEngine;
using UnityEngine.SceneManagement;

public enum LevelLoadType{ NewGame, LoadGame, RestartGame}// <- should not exist

public static class GameEvent
{
    private static PlayerLevelData endData;
    public static LevelLoadType loadType;
    public static uint restartCounter;
    // [HideInInspector] public bool isPaused = false; //TODO: should be in GameEvent.cs
    
    // private static
    public static void LoadEndScene()
    {
        SceneManager.LoadScene("EndScene");
    }
    public static void NextLevel()
    {
        Debug.Log("No other level found Redirecting to main menu!");
        SceneManager.LoadScene("MainMenu");
        // string nextScene = "";// <- TODO: this should contain the name of the next level scene of the character
        // SceneManager.LoadScene(nextScene);

    }
    public static void NewGame(string sceneLevelName)
    {
        loadType = LevelLoadType.NewGame;
        restartCounter = 0;
        if (!GameData.Instance.unlockLevels.Contains(sceneLevelName))
        {
            Debug.Log(sceneLevelName + " Does not Exist");
            return;
        }
        SceneManager.LoadScene(sceneLevelName);
    }

    public static void RestartGame()
    {
        if (PlayerLevelData.Instance.levelData.lives > 1)
        {
            loadType = LevelLoadType.RestartGame;
            restartCounter++;       
            Debug.Log($"GameEvent Restart counter :{restartCounter}");
            SceneManager.LoadScene(PlayerLevelData.Instance.levelData.sceneName);
        } else {
            Debug.Log($"You have {PlayerLevelData.Instance.levelData.lives}! you cant restart anymore!");
        }
    }

    public static void LoadGame(int slotNumber)
    {
        loadType = LevelLoadType.LoadGame;
        restartCounter = 0;
        GameData.loadedLevelData = GameData.saveFileDataList[slotNumber];
        SceneManager.LoadScene(GameData.loadedLevelData.levelData.sceneName);
    }

    public static void SetEndWindowActive(EndGameType endGameType)
    {
        InGameUI inGameUI = InGameUI.Instance;
        Debug.Assert(inGameUI != false, $"ERROR:{inGameUI.gameObject.name} instance is null");
        Debug.Assert(inGameUI.getGameEndWindow != false, $"ERROR: Game End window is null/not found!");
        inGameUI.getGameEndWindow.SetActive(true);
        inGameUI.endGameType = endGameType;
    }
    // public static void On
    public static void PauseGame()//TODO: should be in GameEvent.cs
    {
        Debug.Assert(!InGameUI.Instance.isPaused, "Game is Already Paused");
        InGameUI.Instance.isPaused = true;
        Time.timeScale = 0f;
    }

    public static void UnpauseGame()//TODO: should be in GameEvent.cs
    {
        Debug.Assert(InGameUI.Instance.isPaused, "Game is not Paused");
        InGameUI.Instance.isPaused = false;
        Time.timeScale = 1f;
    }
}