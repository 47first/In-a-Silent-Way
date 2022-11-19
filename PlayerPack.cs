using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPack : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    [SerializeField] private ViewController viewController;
    public PlayerController PlayerController { get { return controller; } }
    public ViewController ViewController { get { return viewController; } }
}
