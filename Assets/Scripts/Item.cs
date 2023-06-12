using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(BoxCollider2D))]
public class Item : MonoBehaviour
{ 
    public enum InteractionType { NONE, Pickup, Examine, Next}
    public InteractionType type;
    [Header("Examine")]
    public string descriptionText;
    public UnityEvent customEvent;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        gameObject.layer = 10;
    }

    public void Interact()
    {
        switch (type)
        {
            case InteractionType.Pickup:
                FindObjectOfType<InteractionSystem>().PickUpItem(gameObject);
                gameObject.SetActive(false);
                Debug.Log("PICK UP");
                break;
            case InteractionType.Examine:
                FindObjectOfType<InteractionSystem>().ExamineItem(this);
                Debug.Log("EXAMINE");
                break;
            case InteractionType.Next:
                FindObjectOfType<InteractionSystem>().NextScene();
                break;
            default:
                Debug.Log("NULL");
                break;
        }

        customEvent.Invoke();
    }
}
