using System;
using UnityEngine;

public class ResourceState : MonoBehaviour
{
    [SerializeField] private int startingFood = 5;
    [SerializeField] private int startingGold = 5;
    [SerializeField] private int startingArmy = 5;
    [SerializeField] private int startingCrown = 5;

    public int Food { get; private set; }
    public int Gold { get; private set; }
    public int Army { get; private set; }
    public int Crown { get; private set; }

    public event Action Changed;

    private void Awake()
    {
        Food = startingFood;
        Gold = startingGold;
        Army = startingArmy;
        Crown = startingCrown;
    }

    public void Apply(ResourceChange change)
    {
        Food += change.food;
        Gold += change.gold;
        Army += change.army;
        Crown += change.crown;
        Changed?.Invoke();
    }
}
