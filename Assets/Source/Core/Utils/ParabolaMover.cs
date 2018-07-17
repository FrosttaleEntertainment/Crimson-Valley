using Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParabolaMover : MonoBehaviour {

    public float Speed; 
    public Entity Owner;
    public LayerMask Mask;

    public GameObject Projectile;
    public Vector3 Target;

    public float Gravity;
    public float FiringAngle;

    private Rigidbody m_rigidBody;

    public static ParabolaMover AddMover(GameObject onThis, Vector3 target, float speed, Entity owner, LayerMask mask, float firingAngle = 60f,float gravity = 9.8f)
    {
        ParabolaMover mover = onThis.AddComponent<ParabolaMover>();
        
        mover.Projectile = onThis;
        mover.Target = target;
        mover.Speed = speed;
        mover.Owner = owner;
        mover.Mask = mask;
        
        mover.Gravity = gravity;
        mover.FiringAngle = firingAngle;

        return mover;
    }

    void Start () {
        var m_rigidBody = GetComponent<Rigidbody>();

        // Selected angle in radians
        float angle = FiringAngle * Mathf.Deg2Rad;

        // Positions of this object and the target on the same plane
        Vector3 planarTarget = new Vector3(Target.x, 0, Target.z);
        Vector3 planarPostion = new Vector3(transform.position.x, 0, transform.position.z);

        // Planar distance between objects
        float distance = Vector3.Distance(planarTarget, planarPostion);
        // Distance along the y axis between objects
        float yOffset = transform.position.y - Target.y;

        float initialVelocity = (1 / Mathf.Cos(angle)) * Mathf.Sqrt((0.5f * Gravity * Mathf.Pow(distance, 2)) / (distance * Mathf.Tan(angle) + yOffset));

        Vector3 velocity = new Vector3(0, initialVelocity * Mathf.Sin(angle), initialVelocity * Mathf.Cos(angle));

        // Rotate our velocity to match the direction between the two objects
        float angleBetweenObjects = Vector3.Angle(Vector3.forward, planarTarget - planarPostion) * (Target.x > transform.position.x ? 1 : -1);
        Vector3 finalVelocity = Quaternion.AngleAxis(angleBetweenObjects, Vector3.up) * velocity;

        // Fire!
        //m_rigidBody.velocity = finalVelocity;

        m_rigidBody.AddForce(finalVelocity * m_rigidBody.mass, ForceMode.Impulse);


        //StartCoroutine(SimulateProjectile());
    }

    private IEnumerator SimulateProjectile()
    {
        // Short delay added before Projectile is thrown
        //yield return new WaitForSeconds(0.5f);

        // Move projectile to the position of throwing object + add some offset if needed.
        Projectile.transform.position = transform.position + new Vector3(0, 0.0f, 0);

        // Calculate distance to target
        float target_Distance = Vector3.Distance(Projectile.transform.position, Target);

        // Calculate the velocity needed to throw the object to the target at specified angle.
        float projectile_Velocity = target_Distance / (Mathf.Sin(2 * FiringAngle * Mathf.Deg2Rad) / Gravity);

        // Extract the X  Y componenent of the velocity
        float Vx = Mathf.Sqrt(projectile_Velocity) * Mathf.Cos(FiringAngle * Mathf.Deg2Rad);
        float Vy = Mathf.Sqrt(projectile_Velocity) * Mathf.Sin(FiringAngle * Mathf.Deg2Rad);

        // Calculate flight time.
        float flightDuration = target_Distance / Vx;

        // Rotate projectile to face the target.
        Projectile.transform.rotation = Quaternion.LookRotation(Target - Projectile.transform.position);

        float elapse_time = 0;

        while (elapse_time < flightDuration)
        {
            Projectile.transform.Translate(0, (Vy - (Gravity * elapse_time)) * Time.deltaTime, Vx * Time.deltaTime);

            elapse_time += Time.deltaTime;

            yield return null;
        }

        if (m_rigidBody)
        {
            m_rigidBody.useGravity = true;
            m_rigidBody.isKinematic = false;
        }
    }
}
