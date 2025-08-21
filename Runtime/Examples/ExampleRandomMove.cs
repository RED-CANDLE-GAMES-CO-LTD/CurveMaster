using UnityEngine;

namespace CurveMaster.Examples
{
    /// <summary>
    /// Example random movement script
    /// Uses multiple sine wave combinations to generate smooth random movement
    /// </summary>
    public class ExampleRandomMove : MonoBehaviour
    {
        [Header("Movement Range")]
        [SerializeField] private Vector3 moveRange = new Vector3(5f, 2f, 5f);
        
        [Header("Movement Speed")]
        [SerializeField] private float baseSpeed = 1f;
        
        [Header("Wave Parameters")]
        [SerializeField] private int waveCount = 3; // Number of waves per axis
        [SerializeField] private float frequencyRange = 2f; // Frequency variation range
        [SerializeField] private float phaseRandomness = 360f; // Phase randomness range
        
        // Internal parameters
        private Vector3 startPosition;
        private float[] xFrequencies;
        private float[] yFrequencies;
        private float[] zFrequencies;
        private float[] xPhases;
        private float[] yPhases;
        private float[] zPhases;
        private float[] xAmplitudes;
        private float[] yAmplitudes;
        private float[] zAmplitudes;
        
        private void Start()
        {
            // Record starting position
            startPosition = transform.position;
            
            // Initialize wave parameters
            InitializeWaveParameters();
        }
        
        private void InitializeWaveParameters()
        {
            // Initialize arrays
            xFrequencies = new float[waveCount];
            yFrequencies = new float[waveCount];
            zFrequencies = new float[waveCount];
            xPhases = new float[waveCount];
            yPhases = new float[waveCount];
            zPhases = new float[waveCount];
            xAmplitudes = new float[waveCount];
            yAmplitudes = new float[waveCount];
            zAmplitudes = new float[waveCount];
            
            // Set random parameters for each wave
            for (int i = 0; i < waveCount; i++)
            {
                // Frequency - use different multiples to create complex movement patterns
                float baseFreq = 0.5f + i * 0.3f;
                xFrequencies[i] = baseFreq + Random.Range(-frequencyRange * 0.5f, frequencyRange * 0.5f);
                yFrequencies[i] = baseFreq + Random.Range(-frequencyRange * 0.5f, frequencyRange * 0.5f);
                zFrequencies[i] = baseFreq + Random.Range(-frequencyRange * 0.5f, frequencyRange * 0.5f);
                
                // Phase - random initial phase
                xPhases[i] = Random.Range(0f, phaseRandomness);
                yPhases[i] = Random.Range(0f, phaseRandomness);
                zPhases[i] = Random.Range(0f, phaseRandomness);
                
                // Amplitude - decreases to make primary waves have more influence
                float amplitudeFactor = 1f / (i + 1);
                xAmplitudes[i] = amplitudeFactor;
                yAmplitudes[i] = amplitudeFactor;
                zAmplitudes[i] = amplitudeFactor;
            }
        }
        
        private void Update()
        {
            // Calculate new position
            Vector3 offset = CalculateOffset(Time.time * baseSpeed);
            
            // Apply position
            transform.position = startPosition + offset;
        }
        
        private Vector3 CalculateOffset(float time)
        {
            Vector3 result = Vector3.zero;
            
            // Calculate offset for each axis
            for (int i = 0; i < waveCount; i++)
            {
                // X axis
                result.x += Mathf.Sin(time * xFrequencies[i] + xPhases[i] * Mathf.Deg2Rad) * xAmplitudes[i];
                
                // Y axis
                result.y += Mathf.Sin(time * yFrequencies[i] + yPhases[i] * Mathf.Deg2Rad) * yAmplitudes[i];
                
                // Z axis
                result.z += Mathf.Sin(time * zFrequencies[i] + zPhases[i] * Mathf.Deg2Rad) * zAmplitudes[i];
            }
            
            // Normalize and apply movement range
            result.x *= moveRange.x;
            result.y *= moveRange.y;
            result.z *= moveRange.z;
            
            return result;
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw movement range
            Vector3 center = Application.isPlaying ? startPosition : transform.position;
            
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireCube(center, moveRange * 2f);
            
            // Draw line from current position to center
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPosition, transform.position);
            }
        }
    }
}