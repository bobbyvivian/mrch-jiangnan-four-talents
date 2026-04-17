using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    public int totalPieces;
    private int placedPieces = 0;

    [Header("Events")]
    public UnityEvent onPuzzleComplete;

    public void PieceCompleted()
    {
        placedPieces++;
        if (placedPieces >= totalPieces)
        {
            OnPuzzleSolved();
        }
    }

    void OnPuzzleSolved()
    {
        Debug.Log(gameObject.name + " complete!");
        onPuzzleComplete.Invoke();
    }
}