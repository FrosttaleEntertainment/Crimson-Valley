using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class CameraControl : MonoBehaviour
{
    public enum MoveStyle { Loose, Stiff }
    public MoveStyle FollowingStyle;

    public enum TrackingStyle { PositionalAverage, AimingAverage }
    public TrackingStyle Tracking;

    public float TrackDistance;
    public float TrackSpeed;

    public GameObject Target;
    [Tooltip("By changing the offset make sure to change the zoom values as well !")]
    public Vector3 Offset;

    public GameObject Arbiter;
    [Space(5)]
    [Header("Zoom")]
    [Space(5)]
    public float ZoomDuration = 1.5f;
    public int[] ZoomValues;

    private int m_currentZoom = 0;
    private bool isZooming = false;
    private Vector3 m_averagePosition;

    public GameObject GetArbiter()
    {
        return Arbiter;
    }

    private void Reset()
    {
        FollowingStyle = MoveStyle.Loose;
        Tracking = TrackingStyle.AimingAverage;
        TrackDistance = 2f;
        TrackSpeed = 5f;
        Offset = new Vector3(0.5f, 10.0f, -0.5f);
    }
    void OnEnable()
    {
        if (Arbiter != null) return;
        Arbiter = new GameObject { name = "Camera Arbiter" };
        Arbiter.transform.SetParent(transform);
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!Application.isPlaying)
        {
            FixedUpdate();
        }
    }
#endif

    void FixedUpdate()
    {
        if(Target == null)
        {
            return;
        }
        if(((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetAxis("Mouse ScrollWheel") != 0))
        {
            Zoom(Input.GetAxis("Mouse ScrollWheel"));
        }

        FollowTarget();
        SetArbiterTransform();

        m_averagePosition.Set(0, 0, 0); // precaution
    }

    private void Zoom(float dir)
    {
        if((dir < 0 && m_currentZoom == 0) || (dir > 0 && m_currentZoom == ZoomValues.Length - 1) || (isZooming == true)){
            return;
        }

        if(dir > 0)
        {
            m_currentZoom++;
            StartCoroutine(LerpZoom(ZoomValues[m_currentZoom], ZoomDuration));
        } else
        {
            m_currentZoom--;
            StartCoroutine(LerpZoom(ZoomValues[m_currentZoom], ZoomDuration));
        }
    }

    private IEnumerator LerpZoom(float value, float time)
    {
        float elapsedTime = 0;
        float startValue = Offset.y;
        while (elapsedTime< time)
        {
            Offset.y = Mathf.Lerp(startValue, value, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        Offset.y = value;
    }

    private void FollowTarget()
    {
        m_averagePosition = GetAveragePos();

        if (FollowingStyle == MoveStyle.Loose)
        {
            transform.position = Vector3.Lerp(transform.position, m_averagePosition + Offset, Time.deltaTime * TrackSpeed);
        }
        else
        {
            transform.position = m_averagePosition + Offset;
        }

        transform.LookAt(m_averagePosition);
    }

    private void SetArbiterTransform()
    {
        if (Arbiter == null)
        {
            return;
        }

        Arbiter.transform.position = new Vector3(m_averagePosition.x, gameObject.transform.position.y, m_averagePosition.z);
        Arbiter.transform.rotation = Quaternion.Euler(0, gameObject.transform.rotation.eulerAngles.y, 0);
    }

    private Vector3 GetAveragePos()
    {
        Vector3 tempVec;

        tempVec = Tracking == TrackingStyle.PositionalAverage
            ? Target.transform.position
            : Target.transform.position + (Target.transform.forward * TrackDistance);

        m_averagePosition += tempVec;


        return m_averagePosition;
    }

    void OnDestroy()
    {
        DestroyImmediate(Arbiter);
    }
}
