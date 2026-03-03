using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.CorgiEngine;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class easterEgg : PickableItem, MMEventListener<PickableItemEvent>
{
    [AddComponentMenu("Corgi Engine/Items/Easter Egg")]

    public int starID = 1;
    protected override void Start()
    {
        base.Start();
        DisableIfAlreadyCollected();
    }

    protected virtual void DisableIfAlreadyCollected()
    {
        foreach (RetroAdventureScene scene in RetroAdventureProgressManager.Instance.Scenes)
        {
            if (scene.SceneName == SceneManager.GetActiveScene().name)
            {
                if (scene.CollectedStars.Length >= starID)
                {
                    if (scene.CollectedStars[starID])
                    {
                        Disable();
                    }
                }
            }
        }
    }
    protected virtual void Disable()
    {
        this.gameObject.SetActive(false);
    }
    void onEnable()
    {
        this.MMEventStartListening<PickableItemEvent>();
    }

        void onDisable()
    {
        this.MMEventStopListening<PickableItemEvent>();
    }

    void OnCollisionEnter(Collision collision)
    {


        Debug.Log("libro EE");
        Debug.Log(this);
    }
    public virtual void OnMMEvent(PickableItemEvent e)
    {
        
       
        Debug.Log("libro EE");
        Debug.Log(e.PickedItem);
    }
}
