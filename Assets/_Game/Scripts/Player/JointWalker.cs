using UnityEngine;

public class JointWalker : MonoBehaviour, IWalker
{
   [Header("Joints")]
    public HingeJoint leftHip, rightHip;
    public HingeJoint leftKnee, rightKnee;
    public HingeJoint leftAnkle, rightAnkle;
    public HingeJoint leftShoulder, rightShoulder;
    public HingeJoint leftElbow, rightElbow;

    [Header("Gait")]
    [Tooltip("Steps per second (cadence). 1.6–2.2 feels natural for a walk.")]
    public float stepRate = 1.8f;

    [Tooltip("Hip swing amplitude (degrees).")]
    public float strideSize = 28f;

    [Tooltip("Knee peak bend during swing (degrees).")]
    public float kneeLiftAmt = 50f;

    [Tooltip("Ankle dorsiflex (toe up) during swing (degrees).")]
    public float toeUp = 12f;

    [Tooltip("Ankle plantarflex (toe down) near toe-off (degrees).")]
    public float toeDown = -10f;

    [Header("Body / Motion")]
    public float pushForce = 0.12f;     // forward accel (VelocityChange)
    public float maxForwardSpeed = 1.6f;
    public float speedToCadence = 0.35f; 

    [Header("Arms")]
    public float shoulderSwing = 18f;
    public float shoulderBase = 10f;     
    public float armLag = 0.06f;         
    public float elbowBendSwing = 35f;
    public float elbowBendBack = 10f;

    [Header("Smoothing")]
    [Tooltip("Higher = snappier joints. 10–25 is usually good.")]
    public float jointResponse = 18f;

    [Header("Curves")]
    public AnimationCurve hipSwingCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 0.10f), new Keyframe(0.50f, 0.65f), new Keyframe(0.85f, 0.95f), new Keyframe(1f, 1f));
    public AnimationCurve kneeSwingCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.18f, 0.15f), new Keyframe(0.50f, 1f), new Keyframe(0.82f, 0.20f), new Keyframe(1f, 0f));
    public AnimationCurve ankleSwingCurve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.35f, 1f), new Keyframe(0.75f, 0.7f), new Keyframe(1f, 0.2f));
    public AnimationCurve ankleStanceCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.65f, 0.0f), new Keyframe(0.88f, 1f), new Keyframe(1f, 0.2f));
    
    public bool isWalking = true;

    Rigidbody rb;

    // internal smoothed targets
    float lHipT, rHipT, lKneeT, rKneeT, lAnkleT, rAnkleT;
    float lShT, rShT, lElT, rElT;
    
    public void SetMovementEnabled(bool enabled)
    {
        isWalking = enabled;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Ensure constraints keep it upright
        if(rb) rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        // TARGET VALUES
        float lHip = 0, rHip = 0, lKnee = 0, rKnee = 0, lAnkle = 0, rAnkle = 0;
        float lShoulder = 0, rShoulder = 0, lElbow = 0, rElbow = 0;

        if (isWalking)
        {
            // --- WALKING LOGIC ---

            // Local forward speed
            float localForward = transform.InverseTransformDirection(rb.linearVelocity).z;
            float cadence = stepRate * (1f + Mathf.Clamp(localForward, 0f, maxForwardSpeed) * speedToCadence);

            // Apply Force
            if (localForward < maxForwardSpeed)
            {
                rb.AddForce(transform.forward * pushForce, ForceMode.VelocityChange);
            }

            // Master gait phase
            float cycle = Mathf.Repeat(Time.time * cadence, 1f);
            bool leftSwing = cycle < 0.5f;
            float halfT = leftSwing ? (cycle * 2f) : ((cycle - 0.5f) * 2f); 

            // Legs
            if (leftSwing)
            {
                ApplySwing(halfT, out lHip, out lKnee, out lAnkle);
                ApplyStance(halfT, out rHip, out rKnee, out rAnkle);
            }
            else
            {
                ApplySwing(halfT, out rHip, out rKnee, out rAnkle);
                ApplyStance(halfT, out lHip, out lKnee, out lAnkle);
            }

            // Arms
            float lagPhase = Mathf.Repeat((Time.time - armLag) * cadence, 1f);
            float armWave = Mathf.Sin(lagPhase * Mathf.PI * 2f);

            lShoulder = shoulderBase + (-armWave * shoulderSwing);
            rShoulder = shoulderBase + ( armWave * shoulderSwing);

            lElbow = (lShoulder > shoulderBase + 2f) ? -elbowBendSwing : -elbowBendBack;
            rElbow = (rShoulder > shoulderBase + 2f) ? -elbowBendSwing : -elbowBendBack;
        }
        else
        {
            // --- STOPPING LOGIC ---
            
            // 1. Kill Velocity (Prevents sliding/moonwalking)
            Vector3 vel = rb.linearVelocity;
            vel.x = Mathf.MoveTowards(vel.x, 0, Time.fixedDeltaTime * 5f);
            vel.z = Mathf.MoveTowards(vel.z, 0, Time.fixedDeltaTime * 5f);
            rb.linearVelocity = vel;

            // 2. Set Neutral Targets (Standing Pose)
            lHip = 0; rHip = 0;
            lKnee = 0; rKnee = 0; // Straight legs
            lAnkle = 0; rAnkle = 0;
            lShoulder = 0; rShoulder = 0; // Arms at sides
            lElbow = 0; rElbow = 0;
        }

        // --- APPLY SMOOTHING (Happens for both Walk and Stop) ---
        float s = 1f - Mathf.Exp(-jointResponse * Time.fixedDeltaTime);
        
        lHipT   = Mathf.Lerp(lHipT,   lHip,   s);
        rHipT   = Mathf.Lerp(rHipT,   rHip,   s);
        lKneeT  = Mathf.Lerp(lKneeT,  lKnee,  s);
        rKneeT  = Mathf.Lerp(rKneeT,  rKnee,  s);
        lAnkleT = Mathf.Lerp(lAnkleT, lAnkle, s);
        rAnkleT = Mathf.Lerp(rAnkleT, rAnkle, s);

        lShT = Mathf.Lerp(lShT, lShoulder, s);
        rShT = Mathf.Lerp(rShT, rShoulder, s);
        lElT = Mathf.Lerp(lElT, lElbow, s);
        rElT = Mathf.Lerp(rElT, rElbow, s);

        // Apply to joints
        SetJoint(leftHip,   lHipT);
        SetJoint(rightHip,  rHipT);
        SetJoint(leftKnee,  -lKneeT);
        SetJoint(rightKnee, -rKneeT);
        SetJoint(leftAnkle,  lAnkleT);
        SetJoint(rightAnkle, rAnkleT);
        SetJoint(leftShoulder,  lShT);
        SetJoint(rightShoulder, rShT);

        if (leftElbow)  SetJoint(leftElbow,  lElT);
        if (rightElbow) SetJoint(rightElbow, rElT);
    }

    void ApplySwing(float t, out float hip, out float knee, out float ankle)
    {
        float hs = hipSwingCurve.Evaluate(t);
        hip = Mathf.Lerp(-strideSize, strideSize, hs);

        float ks = kneeSwingCurve.Evaluate(t);
        knee = ks * kneeLiftAmt;

        float a = ankleSwingCurve.Evaluate(t);
        ankle = Mathf.Lerp(0f, toeUp, a);
    }

    void ApplyStance(float t, out float hip, out float knee, out float ankle)
    {
        hip = Mathf.Lerp(strideSize, -strideSize, t);
        knee = Mathf.Lerp(2f, 0f, Mathf.Abs(t - 0.5f) * 2f);
        float a = ankleStanceCurve.Evaluate(t);
        ankle = Mathf.Lerp(0f, toeDown, a);
    }

    void SetJoint(HingeJoint joint, float val)
    {
        if (!joint) return;
        JointSpring js = joint.spring;
        js.targetPosition = val;
        joint.spring = js;
    }
}