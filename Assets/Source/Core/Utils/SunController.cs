using UnityEngine;

public class SunController : MonoBehaviour {

	public Gradient nightDayColor;

	public float maxIntensity = 3f;
	public float minIntensity = 0f;
	public float minPoint = -0.2f;

	public float maxAmbient = 1f;
	public float minAmbient = 0f;
	public float minAmbientPoint = -0.2f;

	public Gradient nightDayFogColor;
	public AnimationCurve fogDensityCurve;
	public float fogScale = 1f;

	public float dayAtmosphereThickness = 0.4f;
	public float nightAtmosphereThickness = 0.87f;

    public Transform m_dayStartPos;
    public Transform m_nightStartPos;

    bool isMoving;

    private float m_currentStateDuration;
    private float m_angle;

    Light mainLight;
	Skybox sky;
	Material skyMat;

    void Start()
    {
        GameController.Instance.onPhaseChanged += OnPhaseChangedImpl;

        mainLight = GetComponent<Light>();
        skyMat = RenderSettings.skybox;

        transform.rotation = m_dayStartPos.rotation;

        isMoving = false;

        m_angle = 360 - Quaternion.Angle(m_dayStartPos.rotation, m_nightStartPos.rotation);
    }

    private void OnDestroy()
    {
        if(GameController.Instance != null)
        {
            GameController.Instance.onPhaseChanged -= OnPhaseChangedImpl;
        }
    }

	void Update () 
	{
        if(isMoving)
        {
            TranslateSun();
        }
    }

    private void TranslateSun()
    {
        float tRange = 1 - minPoint;
        float dot = Mathf.Clamp01((Vector3.Dot(mainLight.transform.forward, Vector3.down) - minPoint) / tRange);
        float i = ((maxIntensity - minIntensity) * dot) + minIntensity;

        mainLight.intensity = i;

        tRange = 1 - minAmbientPoint;
        dot = Mathf.Clamp01((Vector3.Dot(mainLight.transform.forward, Vector3.down) - minAmbientPoint) / tRange);
        i = ((maxAmbient - minAmbient) * dot) + minAmbient;
        RenderSettings.ambientIntensity = i;

        mainLight.color = nightDayColor.Evaluate(dot);
        RenderSettings.ambientLight = mainLight.color;

        RenderSettings.fogColor = nightDayFogColor.Evaluate(dot);
        RenderSettings.fogDensity = fogDensityCurve.Evaluate(dot) * fogScale;

        i = ((dayAtmosphereThickness - nightAtmosphereThickness) * dot) + nightAtmosphereThickness;
        skyMat.SetFloat("_AtmosphereThickness", i);

        // calculate rotation speed
        float rotationSpeed = m_angle / m_currentStateDuration;

        transform.Rotate(-Vector3.right * Time.deltaTime * rotationSpeed);
    }

    private void OnPhaseChangedImpl(GameState currentMode, float timeLeft)
    {
        m_currentStateDuration = timeLeft;
        
        if (currentMode == GameState.Day)
        {
            //set sun moving
            isMoving = true;
        }
        else
        {
            // stop the sun
            isMoving = false;

            // move it to start possition
            transform.rotation = m_dayStartPos.rotation;
        }
    }
}
