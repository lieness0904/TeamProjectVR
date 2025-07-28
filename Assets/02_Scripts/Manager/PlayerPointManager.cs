using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class PlayerPointManager : MonoBehaviour
{
    public static PlayerPointManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI pointText;
    private int currentPoints;

    public event Action<int> OnPointChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }   
    }
   
    public void Initialize(int startingPoints)
    {

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.Points = currentPoints;
        }

        currentPoints = startingPoints;

        UpdateUI();
    }

    public void AddPoints(int amount)
    {
        currentPoints += amount;

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.Points = currentPoints;
        }

        UpdateUI();
    }

    public bool TrySpend(int amount)
    {
        if (currentPoints >= amount)
        {
            currentPoints -= amount;

            if(PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.Points = currentPoints;
            }

            UpdateUI();
            return true;
        }
        Debug.LogWarning("포인트 부족!");
        return false;
    }

    public int GetPoints() => currentPoints;

    public void SetPointText(TextMeshProUGUI text)
    {
        pointText = text;
        UpdateUI(); 
    }

    public void UpdateUI()
    {
        if (pointText != null)
        {
            pointText.text = $"포인트 : {currentPoints}";
        }
        OnPointChanged?.Invoke(currentPoints);
    }
    
}
