using UnityEngine;

public class ParentWalker : MonoBehaviour, IWalker
{
    [Header("Limbs")]
    public Transform leftHip, rightHip;
    public Transform leftKnee, rightKnee;
    public Transform leftAnkle, rightAnkle;
    public Transform leftShoulder, rightShoulder;
    public Transform leftElbow, rightElbow;

    [Header("Corrections (Tweak these first!)")]
    [Tooltip("If walking backwards, set this to -1.")]
    public float moveDirection = 1f; 

    [Tooltip("If arms rotate backwards, set this to -1.")]
    public float armDirection = 1f;

    [Header("Gait Speed")]
    [Tooltip("LOWER this number to slow down the animation speed. Default was 1.8.")]
    public float stepRate = 1.4f; // <--- CHANGE THIS FOR SPEED

    [Tooltip("Hip swing amplitude (degrees).")]
    public float strideSize = 28f;
    public float kneeLiftAmt = 50f;
    public float toeUp = 12f;
    public float toeDown = -10f;

    [Header("Body / Motion")]
    public float maxForwardSpeed = 1.6f; 
    public float speedToCadence = 0.35f; 

    [Header("Arms")]
    public float shoulderSwing = 18f;
    public float shoulderBase = 10f;     
    public float armLag = 0.06f;         
    public float elbowBendSwing = 35f;
    public float elbowBendBack = 10f;

    [Header("Smoothing")]
    public float jointResponse = 18f;
    
    [Header("Grounding")]
    public LayerMask groundLayer;
    public float groundOffset = 0f;

    [Header("Curves")]
    public AnimationCurve hipSwingCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 0.10f), new Keyframe(0.50f, 0.65f), new Keyframe(0.85f, 0.95f), new Keyframe(1f, 1f));
    public AnimationCurve kneeSwingCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.18f, 0.15f), new Keyframe(0.50f, 1f), new Keyframe(0.82f, 0.20f), new Keyframe(1f, 0f));
    public AnimationCurve ankleSwingCurve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.35f, 1f), new Keyframe(0.75f, 0.7f), new Keyframe(1f, 0.2f));
    public AnimationCurve ankleStanceCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.65f, 0.0f), new Keyframe(0.88f, 1f), new Keyframe(1f, 0.2f));
    
    public bool isWalking = true;

    // internal smoothed targets
    float lHipT, rHipT, lKneeT, rKneeT, lAnkleT, rAnkleT;
    float lShT, rShT, lElT, rElT;
    
    public void SetMovementEnabled(bool enabled)
    {
        isWalking = enabled;
    }

    void Update()
    {
        float lHip = 0, rHip = 0, lKnee = 0, rKnee = 0, lAnkle = 0, rAnkle = 0;
        float lShoulder = 0, rShoulder = 0, lElbow = 0, rElbow = 0;

        if (isWalking)
        {
            SnapToGround();
            
            // 1. FIXED: Added moveDirection to flip walking direction
            transform.Translate(Vector3.forward * maxForwardSpeed * moveDirection * Time.deltaTime);

            float cadence = stepRate * (1f + speedToCadence); 
            float cycle = Mathf.Repeat(Time.time * cadence, 1f);
            
            bool leftSwing = cycle < 0.5f;
            float halfT = leftSwing ? (cycle * 2f) : ((cycle - 0.5f) * 2f); 

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

            float lagPhase = Mathf.Repeat((Time.time - armLag) * cadence, 1f);
            float armWave = Mathf.Sin(lagPhase * Mathf.PI * 2f);

            lShoulder = shoulderBase + (-armWave * shoulderSwing);
            rShoulder = shoulderBase + ( armWave * shoulderSwing);

            lElbow = (lShoulder > shoulderBase + 2f) ? -elbowBendSwing : -elbowBendBack;
            rElbow = (rShoulder > shoulderBase + 2f) ? -elbowBendSwing : -elbowBendBack;
        }
        else
        {
            lHip = 0; rHip = 0; lKnee = 0; rKnee = 0; lAnkle = 0; rAnkle = 0;
            lShoulder = 0; rShoulder = 0; lElbow = 0; rElbow = 0;
        }

        // SMOOTHING
        float s = 1f - Mathf.Exp(-jointResponse * Time.deltaTime);
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

        // APPLY ROTATIONS
        SetBone(leftHip,   lHipT);
        SetBone(rightHip,  rHipT);
        SetBone(leftKnee,  -lKneeT);
        SetBone(rightKnee, -rKneeT);
        SetBone(leftAnkle,  lAnkleT);
        SetBone(rightAnkle, rAnkleT);

        // 2. FIXED: Multiplied by armDirection to fix backward arms
        SetBone(leftShoulder,  lShT * armDirection);
        SetBone(rightShoulder, rShT * armDirection);
        SetBone(leftElbow,  lElT * armDirection); 
        SetBone(rightElbow, rElT * armDirection);
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

    void SetBone(Transform bone, float val)
    {
        if (!bone) return;
        bone.localRotation = Quaternion.Euler(val, 0, 0);
    }
    
    void SnapToGround()
    {
        RaycastHit hit;
        // Shoot a ray from the robot's center DOWNWARDS
        // (Origin is usually Hips or Feet. adjust Vector3.up * 1f if origin is at feet)
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f, groundLayer))
        {
            // Move the robot to exactly where the ray hit
            transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
        }
    }
}