using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class REBACalculator : MonoBehaviour
{
    public Transform neckBone, trunkBone, rightThighBone, rightCalfBone, Spine2Bone, SpineBodyBone;
    public Transform UpperArmBoneRight, referencePointRight, LowerArmBoneRight; // Right arm
    public Transform UpperArmBoneLeft, referencePointLeft, LowerArmBoneLeft;   // Left arm
    public float ergoScore = 0f;
    public Transform RightHandBone;
    public Transform LeftHandBone;
    public Transform NeckBone;
    private Vector3 referenceDirectionRight;
    private Vector3 referenceDirectionLeft;
    private float pronationThreshold = 10f;
    void Start()
    {
        referenceDirectionRight = UpperArmBoneRight.position - referencePointRight.position;
        referenceDirectionLeft = UpperArmBoneLeft.position - referencePointLeft.position;
    }

    void Update()
    {
        ergoScore = 0f;  // Reset the score each frame

        CalculateNeckReba();
        CalculateTrunkReba();
        CalculateLegReba();
        CalculateAbduction();
        float rebaRightArm = CalculateRightArmReba();
        float rebaLeftArm = CalculateLeftArmReba();

    // Use the maximum Postural score from both arms
    ergoScore += Mathf.Max(rebaRightArm, rebaLeftArm);


        Debug.Log("Total Postural Score: " + ergoScore);
    }
    //Calculating Neck Score
    void CalculateNeckReba()
    {
        Vector3 localNeckRotation = neckBone.localEulerAngles;
        Vector3 adjustedNeckRotation = ConvertRotationToMinus180To180(localNeckRotation);

        if (Mathf.Abs(adjustedNeckRotation.x) > 20 || Mathf.Abs(adjustedNeckRotation.y) > 20 || Mathf.Abs(adjustedNeckRotation.z) > 20)
        {
            ergoScore += 2;
        }
        else
        {
            ergoScore += 1;
        }
    }
    //Calculating Trunk Score
    void CalculateTrunkReba()
    {
        Vector3 localSpine2Rotation = Spine2Bone.localEulerAngles;
        Vector3 adjustedSpine2Rotation = ConvertRotationToMinus180To180(localSpine2Rotation);

        if (adjustedSpine2Rotation.y > 2 || adjustedSpine2Rotation.y < - 2)
        {
            ergoScore += 1; // Twisted trunk score 
        }

        Vector3 localTrunkRotation = trunkBone.localEulerAngles;
        Vector3 adjustedTrunkRotation = ConvertRotationToMinus180To180(localTrunkRotation);
        
        if (adjustedTrunkRotation.x >= -3 && adjustedTrunkRotation.x <= 3)
        {
            ergoScore += 1;
        }
        else if (adjustedTrunkRotation.x < -3)
        {
            ergoScore += 2;
        }
        else if (adjustedTrunkRotation.x > 3)
        {
            ergoScore += 3;
        }
    }
    //Caculating arm abduction
    void CalculateAbduction(){
        float NeckBonePosition = NeckBone.position.y;
        float RightHandBonePosition = RightHandBone.position.y;
        float LeftHandBonePosition = LeftHandBone.position.y;
        if (NeckBonePosition < RightHandBonePosition || NeckBonePosition < LeftHandBonePosition )
        {
            ergoScore += 1;
        }

    }


    //Calculating the Leg Score
    void CalculateLegReba()
    {
        float legAngle = Vector3.Angle(rightThighBone.forward, rightCalfBone.forward);

        ergoScore += 1;  // Bilateral leg weightbearing

        if (legAngle > 2 && legAngle < 60)
        {
            ergoScore += 1;
        }
        else if (legAngle > 60)
        {
            ergoScore += 2;
        }
    }
    //Calculating Right arm score
    float CalculateRightArmReba()
    {
        float rebaArm = 0f;

        Vector3 currentDirection = UpperArmBoneRight.up;
        float ArmBodyangle = -1 * (90 - Vector3.Angle(referenceDirectionRight, currentDirection));

        if (ArmBodyangle >= -20 && ArmBodyangle <= 20)
        {
            rebaArm += 1;
        }
        else if (ArmBodyangle > 20)
        {
            rebaArm += 2;
        }

        float ArmLowerangle = Vector3.Angle(UpperArmBoneRight.up, LowerArmBoneRight.up);
        if (ArmLowerangle >= 60 && ArmLowerangle <= 100)
        {
            rebaArm += 1;
        }
        else if (ArmLowerangle > 100)
        {
            rebaArm += 2;
        }

        return rebaArm;
    }
    //Calculating Left arm score
    float CalculateLeftArmReba()
    {
        float rebaArm = 0f;

        Vector3 currentDirection = UpperArmBoneLeft.up;
        float ArmBodyangle = -1 * (90 - Vector3.Angle(referenceDirectionLeft, currentDirection));

        if (ArmBodyangle >= -20 && ArmBodyangle <= 20)
        {
            rebaArm += 1;
        }
        else if (ArmBodyangle > 20)
        {
            rebaArm += 2;
        }

        float ArmLowerangle = Vector3.Angle(UpperArmBoneLeft.up, LowerArmBoneLeft.up);
        if (ArmLowerangle >= 60 && ArmLowerangle <= 100)
        {
            rebaArm += 1;
        }
        else if (ArmLowerangle > 100)
        {
            rebaArm += 2;
        }

        return rebaArm;
    }
        //Metric conversion
        float ConvertToMinus180To180(float angle)
        {
            if (angle > 180)
                angle -= 360;
            return angle;
        }
    
        Vector3 ConvertRotationToMinus180To180(Vector3 rotation)
        {
            rotation.x = ConvertToMinus180To180(rotation.x);
            rotation.y = ConvertToMinus180To180(rotation.y);
            rotation.z = ConvertToMinus180To180(rotation.z);
            return rotation;
        }
}
