using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class QLearningAgent : MonoBehaviour
{   private Renderer cubeRenderer;
    public float epsilon = 0.5f;
    private int iterations = 0;
    private float cubeScore = 0.0f;
    private List<string> csvData = new List<string>();
    public REBACalculator REBACalculator;
    public Transform Guy;
    public GameObject cube;
    private int X_SIZE = 0;
    private int Y_SIZE = 0;
    private int Z_SIZE = 0;
    private int ACTION_COUNT = 6;
    private float[,,,] Q;
    private bool shouldContinue = true;  // Flag to control the loop based on REBA score
    private float startTime; 
    private Vector3 optimalPosition;  // Variable to save the optimal cube's position
    private const float extensionX = 0.20f;
    private const float extensionZ = 0.60f; 
    public float tau = 0.7f; 
    private const float extensionY = 1.2f;
    public float LearningRate = 0.1f;
    public float DiscountFactor = 0.9f;
    private float leftBoundary = 0f;
    private float rightBoundary = 0f;
    private float downBoundary = 0f;
    private float upBoundary = 0f;
    private float inBoundary = 0f;
    private float outBoundary = 0f;   
    private float deltaQ = 0.0f;
    private const float convergenceThreshold = 0.005f;
    private int convergenceCount = 0;
    private const int convergenceConsistency = 100;
    private List<string> debugLogData = new List<string>();
    private const float MAX_TIME = 720.0f; // 10 minutes in seconds
    private Vector3 positionWithMinREBA;
    private float minREBAScore = float.MaxValue;
    private float timeElapsed = 0.0f;
    private bool timeLimitReached = false;
    private Vector3 minPosition = Vector3.zero;
    private float currentREBAScore = 0.0f;

    private void Start()
    {    
        leftBoundary = Guy.position.x - extensionX;
        rightBoundary = Guy.position.x + extensionX;
        downBoundary = 6*(Guy.position.y + extensionY)/10;
        upBoundary = Guy.position.y + extensionY*1.2f;
        inBoundary = Guy.position.z+ ((0.5f)*extensionZ);
        outBoundary = Guy.position.z + extensionZ;
        startTime = Time.time;
        // Determine the sizes based on the boundaries
                                    
        X_SIZE = Mathf.FloorToInt((rightBoundary - leftBoundary) * 333.333f); 
        Y_SIZE = Mathf.FloorToInt((upBoundary - downBoundary) * 333.333f);
        Z_SIZE = Mathf.FloorToInt((outBoundary - inBoundary) * 333.333f);

        cubeRenderer = cube.GetComponent<Renderer>();
        Q = new float[X_SIZE, Y_SIZE, Z_SIZE, ACTION_COUNT];
        for (int x = 0; x < X_SIZE; x++)
            for (int y = 0; y < Y_SIZE; y++)
                for (int z = 0; z < Z_SIZE; z++)
                    for (int action = 0; action < ACTION_COUNT; action++)
                        Q[x, y, z, action] = 0.0f;

        StartCoroutine(QLearningRoutine());
    }
    private void CustomLog(string message)
    {
        Debug.Log(message);
        debugLogData.Add(message);
    }
    void OnApplicationQuit()
    {
        SaveDebugLogToCSV();
    }
    private void SaveDebugLogToCSV()
    {   //saving utility function
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string path = Application.dataPath + "/DebugLogData1_" + timestamp + ".csv";
        
        System.IO.File.WriteAllLines(path, debugLogData.ToArray());
        Debug.Log($"Debug log saved to {path}");
    }
    private void SavePositionToCSV(Vector3 position)
{
    string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    string path = Application.dataPath + "/MinREBAPosition2323_2min_" + timestamp + ".csv";
    
    List<string> lines = new List<string>
    {
        "X,Y,Z",
        $"{Guy.position.x - position.x:F4},{Guy.position.y-position.y:F4},{Guy.position.z-position.z:F4},{minREBAScore:F4},{Time.time - startTime:F4}"
    };

    System.IO.File.WriteAllLines(path, lines.ToArray());
    Debug.Log($"Min REBA position saved to {path}");
}

//Qlearning Routine to find the most optimal position for object handover getting feedback from the Score calculator
private IEnumerator QLearningRoutine()
{
    startTime = Time.time;
    while (iterations < 10 && !timeLimitReached)
    {
        // Check the total elapsed time for the whole coroutine.

        if (!shouldContinue && !timeLimitReached)
        {
            // Save the data to CSV list
            csvData.Add($"{optimalPosition.x:F4},{optimalPosition.y:F4},{optimalPosition.z:F4},{Time.time - startTime:F4}");
            iterations++;
            if (iterations == 10)
            {
                // Save the CSV data to a file
                yield break;  // Exit the coroutine
            }
            else
            {
                // Randomize cube position
                cube.transform.position = new Vector3(
                leftBoundary + Random.Range(0, X_SIZE) * 0.003f,
                upBoundary + Random.Range(0, Y_SIZE) * 0.003f,
                outBoundary + Random.Range(0, Z_SIZE) * 0.003f
                );
                // Reset the flag and restart the Q-learning process
                shouldContinue = true;
            }
        }
        while (shouldContinue && !timeLimitReached)
        {
            timeElapsed = Time.time - startTime;
            Debug.Log($"Time Elapsed: {timeElapsed:F2} seconds");

            int x = Random.Range(0, X_SIZE);
            int y = Random.Range(0, Y_SIZE);
            int z = Random.Range(0, Z_SIZE);

            while (!IsTerminalState(x, y, z) && shouldContinue)
            {   
                Debug.Log(Time.time - startTime);
                float currentREBAScore = GetREBAScore();
                if (currentREBAScore < minREBAScore)
                {
                    minREBAScore = currentREBAScore;
                    positionWithMinREBA = cube.transform.position;
                    SavePositionToCSV(positionWithMinREBA);
                }
                //10 minute cap
                if (Time.time - startTime >= MAX_TIME)
                    {
                        Debug.Log("10 minutes passed. Stopping Q-Learning routine.");
                        timeLimitReached = true;
                        
                        yield break;
                        
                    }

                int action = ChooseAction(x, y, z);
                (int nextX, int nextY, int nextZ) = GetNextPosition(x, y, z, action);
                Vector3 newPosition = new Vector3(leftBoundary + nextX * 0.003f, downBoundary + nextY * 0.003f, inBoundary + nextZ * 0.003f);
                cube.transform.position = newPosition;
                float reward = ComputeRewardForREBA(GetREBAScore());

                // Update the Q-value
                float maxQValueNextState = MaxQValue(nextX, nextY, nextZ);
                float oldQValue = Q[x, y, z, action];
                Q[x, y, z, action] = oldQValue + LearningRate * (reward + DiscountFactor * maxQValueNextState - oldQValue);

                if (GetREBAScore() <= 4)
                {
                   
                    cubeRenderer.material.color = Color.blue;
                    yield return new WaitForSeconds(4f); // Wait for 4 seconds
                     float duration = Time.time - startTime;
                    optimalPosition = (Guy.transform.position - cube.transform.position);
                    shouldContinue = false;
                    CustomLog($"Optimal position found at: ({optimalPosition.x:F4}, {optimalPosition.y:F4}, {optimalPosition.z:F4}). Time taken: {duration:F4} seconds.");
                    cubeRenderer.material.color = Color.green;

                }

                x = nextX;
                y = nextY;
                z = nextZ;

                yield return null;
            }
        }
    }
}

private float ComputeRewardForREBA(float rebaScore)
{   
    float scalingFactor = 0.5f;
    float tau = 0.7f;
    if (cubeRenderer.material.color == Color.green)
    {
        cubeScore = 5f;
        

        if (rebaScore == 5)
        {
            tau = 0.8f;
        }

        {
            cubeScore = 10f;
        }
    }
    else
    {   
        tau = 0.9f;
        cubeScore = -15f; 
    }

    // Compute the absolute difference in x-coordinates between Guy and the cube.
    float distanceToLeftOfGuy = Guy.position.x - cube.transform.position.x;
    float distanceToRightOfGuy = cube.transform.position.x - Guy.position.x;
    float difference = Mathf.Abs(distanceToLeftOfGuy - distanceToRightOfGuy);

    const float tolerance = 0.001f;

    // Update the cubeScore based on the difference.
    cubeScore = -scalingFactor * (difference - tolerance) * (difference - tolerance);

    // Compute and return the final reward.
    return ((1.0f / (rebaScore * rebaScore)) + cubeScore); 
}
    private bool IsTerminalState(int x, int y, int z)
    {
        return false;  
    }

private int ChooseAction(int x, int y, int z)
{
    float[] probabilities = new float[ACTION_COUNT];
    float sum = 0; // temperature parameter; adjust based on your needs

    for (int action = 0; action < ACTION_COUNT; action++)
    {
        probabilities[action] = Mathf.Exp(Q[x, y, z, action] / tau);
        sum += probabilities[action];
    }

    // Normalize probabilities
    for (int action = 0; action < ACTION_COUNT; action++)
    {
        probabilities[action] /= sum;
    }

    float randomValue = Random.Range(0f, 1f);
    float cumulativeProbability = 0;
    for (int action = 0; action < ACTION_COUNT; action++)
    {
        cumulativeProbability += probabilities[action];
        if (randomValue <= cumulativeProbability)
        {
            return action;
        }
    }

    return ACTION_COUNT - 1;  // Default fallback, should ideally not be reached
}
    private (int, int, int) GetNextPosition(int x, int y, int z, int action)
    {
        switch (action)
        {
            case 0: return (Mathf.Min(x + 1, X_SIZE - 1), y, z);
            case 1: return (Mathf.Max(x - 1, 0), y, z);
            case 2: return (x, Mathf.Min(y + 1, Y_SIZE - 1), z);
            case 3: return (x, Mathf.Max(y - 1, 0), z);
            case 4: return (x, y, Mathf.Min(z + 1, Z_SIZE - 1));
            case 5: return (x, y, Mathf.Max(z - 1, 0));
            default: return (x, y, z);
        }
    }

    private float MaxQValue(int x, int y, int z)
    {
        float maxValue = float.MinValue;
        for (int action = 0; action < ACTION_COUNT; action++)
            if (Q[x, y, z, action] > maxValue)
                maxValue = Q[x, y, z, action];
        return maxValue;
    }

    private float GetREBAScore()
    {
        return REBACalculator.rebaScore;
    }

    
}
