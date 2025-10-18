using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DepositModal : MonoBehaviour
{
    [SerializeField] private GameObject walletCard;
    [SerializeField] private GameObject ephemeralCard;

    private bool isDepositingToWallet = true;
    [SerializeField] private Button swapButton;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button halfInputButton;
    [SerializeField] private Button allInputButton;

    public void Swap()
    {
        isDepositingToWallet = !isDepositingToWallet;
        //swap those vertically around, animate the swap, disable the input for one, make it be dependent on the other.
    }

    public void Send()
    {
        if (isDepositingToWallet)
        {
            // send to wallet
        }
    }
}