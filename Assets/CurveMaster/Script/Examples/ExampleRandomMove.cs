using UnityEngine;

namespace CurveMaster.Examples
{
    /// <summary>
    /// 範例隨機移動腳本
    /// 使用多個正弦波組合產生流暢的隨機移動
    /// </summary>
    public class ExampleRandomMove : MonoBehaviour
    {
        [Header("移動範圍")]
        [SerializeField] private Vector3 moveRange = new Vector3(5f, 2f, 5f);
        
        [Header("移動速度")]
        [SerializeField] private float baseSpeed = 1f;
        
        [Header("波動參數")]
        [SerializeField] private int waveCount = 3; // 每個軸使用的波數量
        [SerializeField] private float frequencyRange = 2f; // 頻率變化範圍
        [SerializeField] private float phaseRandomness = 360f; // 相位隨機範圍
        
        // 內部參數
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
            // 記錄起始位置
            startPosition = transform.position;
            
            // 初始化波參數
            InitializeWaveParameters();
        }
        
        private void InitializeWaveParameters()
        {
            // 初始化陣列
            xFrequencies = new float[waveCount];
            yFrequencies = new float[waveCount];
            zFrequencies = new float[waveCount];
            xPhases = new float[waveCount];
            yPhases = new float[waveCount];
            zPhases = new float[waveCount];
            xAmplitudes = new float[waveCount];
            yAmplitudes = new float[waveCount];
            zAmplitudes = new float[waveCount];
            
            // 為每個波設定隨機參數
            for (int i = 0; i < waveCount; i++)
            {
                // 頻率 - 使用不同的倍數以產生複雜的移動模式
                float baseFreq = 0.5f + i * 0.3f;
                xFrequencies[i] = baseFreq + Random.Range(-frequencyRange * 0.5f, frequencyRange * 0.5f);
                yFrequencies[i] = baseFreq + Random.Range(-frequencyRange * 0.5f, frequencyRange * 0.5f);
                zFrequencies[i] = baseFreq + Random.Range(-frequencyRange * 0.5f, frequencyRange * 0.5f);
                
                // 相位 - 隨機初始相位
                xPhases[i] = Random.Range(0f, phaseRandomness);
                yPhases[i] = Random.Range(0f, phaseRandomness);
                zPhases[i] = Random.Range(0f, phaseRandomness);
                
                // 振幅 - 遞減以使主要波影響較大
                float amplitudeFactor = 1f / (i + 1);
                xAmplitudes[i] = amplitudeFactor;
                yAmplitudes[i] = amplitudeFactor;
                zAmplitudes[i] = amplitudeFactor;
            }
        }
        
        private void Update()
        {
            // 計算新位置
            Vector3 offset = CalculateOffset(Time.time * baseSpeed);
            
            // 套用位置
            transform.position = startPosition + offset;
        }
        
        private Vector3 CalculateOffset(float time)
        {
            Vector3 result = Vector3.zero;
            
            // 計算每個軸的偏移
            for (int i = 0; i < waveCount; i++)
            {
                // X軸
                result.x += Mathf.Sin(time * xFrequencies[i] + xPhases[i] * Mathf.Deg2Rad) * xAmplitudes[i];
                
                // Y軸
                result.y += Mathf.Sin(time * yFrequencies[i] + yPhases[i] * Mathf.Deg2Rad) * yAmplitudes[i];
                
                // Z軸
                result.z += Mathf.Sin(time * zFrequencies[i] + zPhases[i] * Mathf.Deg2Rad) * zAmplitudes[i];
            }
            
            // 正規化並套用移動範圍
            result.x *= moveRange.x;
            result.y *= moveRange.y;
            result.z *= moveRange.z;
            
            return result;
        }
        
        private void OnDrawGizmosSelected()
        {
            // 繪製移動範圍
            Vector3 center = Application.isPlaying ? startPosition : transform.position;
            
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireCube(center, moveRange * 2f);
            
            // 繪製當前位置到中心的連線
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPosition, transform.position);
            }
        }
    }
}