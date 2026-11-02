using UnityEngine;

public class TimeController : MonoBehaviour
{
    public int NightDuration = 300; // Duration of night in seconds
    public int DayDuration = 300; // Duration of day in seconds

    public int NightsSurvived { get; private set; } = 0;

    public GameObject SkyDay;
    public GameObject SkyNight;

    public bool IsNight { get; private set; } = false;

    private float timeElapsed = 0f;

    public void UpdateTime(float deltaTime)
    {
        timeElapsed += deltaTime;

        if (IsNight && timeElapsed >= NightDuration)
        {
            // Transition to day
            IsNight = false;
            timeElapsed = 0f;
            NightsSurvived++;
            SkyDay.SetActive(true);
            SkyNight.SetActive(false);
        }
        else if (!IsNight && timeElapsed >= DayDuration)
        {
            // Transition to night
            IsNight = true;
            timeElapsed = 0f;
            SkyDay.SetActive(false);
            SkyNight.SetActive(true);
        }
    }
    
}
