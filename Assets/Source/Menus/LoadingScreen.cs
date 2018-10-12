using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public Image Background;
    public Text LoadingText;
    public Text DoneText;
    public UIProgressBar ProgressBar;

    private bool isDone = false;

    // starting value for the Lerp
    public float minimum = 0F;
    public float maximum = 1.0F;
    private static float t = 0.0f;

    private void Awake()
    {
        this.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if(gameObject.active)
        {
            if (ProgressBar)
            {
                ProgressBar.fillAmount = Mathf.Lerp(minimum, maximum, t);

                // .. and increase the t interpolater
                t += Time.deltaTime;

                // now check if the interpolator has reached 1.0
                // and swap maximum and minimum so game object moves
                // in the opposite direction.
                if (t > 1.0f || t < 0)
                {
                    float temp = maximum;
                    maximum = minimum;
                    minimum = temp;
                    t = 0.0f;
                }
            }
        }
    }

    public bool Show()
    {
        if (Background == null)
        {
            return false;
        }

        this.gameObject.SetActive(true);

        // Loading screen will wait for all players, then will manually be unloaded
        DontDestroyOnLoad(this.gameObject);

        return true;
    }

    public void Done()
    {
        isDone = true;
        ProgressBar.fillAmount = 1;

        if (LoadingText)
        {
            LoadingText.text = "Loading done";
        }

        if(DoneText)
        {
            DoneText.text = "Waiting for other players";
        }
    }
}