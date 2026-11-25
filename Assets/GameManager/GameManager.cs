using System.Collections.Generic;
using System.Xml.Serialization;
using NUnit.Framework;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "GameManager", menuName = "Managers/GameManager")]
public class GameManager : ScriptableObject
{
    #region enumy
    public enum GameState
    {
        None,
        Menu,
        Ship,
        Casino,
        EndGame // kij wie czy do dawaæ
    }

    public enum WeatherState
    {
        SunnyDay,
        RainyDay
    }

    #endregion
    public GameState State;

    [Header("Game things")]
    public int Day; // który dzieñ w grze
    public WeatherState Weather; // obecna pogoda w grze

    [Header("Rzeczy do UI")]
    public int Money;
    public int WskaznikCzystosci;

    [Header("Scenes")] // sceny, by ³atwiej by³o po³¹czyæ - roboczo na razie, ale trudno
    //public List<SceneAsset> SceneAssets;
    public SceneAsset MenuScene;
    public SceneAsset ShipScene;
    public SceneAsset CasinoScene;


    //[Header("Scenes")]
    //public List<Scene> scenes = new List<Scene>();

    public void StartGame()
    {
        //MenuScene = GameObject.Find("Hand"); -- nie dzia³a kij wie czemu

    }
    #region zmiana GameState + scen
    public void OpenStartScene()
    {
        State = GameState.Menu;
        SceneManager.LoadScene(MenuScene.name);
    }

    public void OpenShipScene()
    {
        State = GameState.Ship;
        SceneManager.LoadScene(ShipScene.name);
    }

    public void OpenCasinoScene()
    {
        State = GameState.Casino;
        SceneManager.LoadScene(CasinoScene.name);
    }

    #endregion

    public void GoToNextDay()
    {
        Day = Day + 1;
    }

    // TODO: dodaj obs³ugê wskaŸnika czystoœci i dzia³anie tego
}
