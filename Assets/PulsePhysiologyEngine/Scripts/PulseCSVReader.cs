using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PulseCSVReader: PulseDataSource
{
    public TextAsset CSVInput;
    public float timeElapsedAtStart;
    public AudioSource audioSource;
    public AudioLowPassFilter audioLowPassFilter;
    public float updateStep = 0.001f;
    public int sampleDataLength = 400;

    [RangeAttribute(0,10000)]
    public int lowFrequencyCutoff = 400;

    List<string[]> CSVValues;
    int lineId;
    float startTime;
    private float[] clipData;
    private float currentUpdateTime = 0f;
    private float clipLoudness;

    void Awake()
    {
        if (data == null)
            data = new PulseData();
        if (!audioSource) {
            Debug.LogError(GetType() + ".Awake: there was no audioSource set.");
        }
        clipData = new float[sampleDataLength];
        audioLowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        ComputeHeaders();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        ComputeHeaders();
    }

    void Start()
    {
        if (!Application.isPlaying || CSVInput == null)
            return;

        startTime = Time.time;

        string[] lines = CSVInput.text.Split('\n');
        if (lines == null || lines.Length < 2)
            return;

        data.timeStampList = new FloatList(lines.Length - 1);
        int numberOfColumns = data.fields.Length;
        data.valuesTable = new List<FloatList>(numberOfColumns);
        for (int columnId = 0; columnId < numberOfColumns; ++columnId)
            data.valuesTable.Add(new FloatList(lines.Length - 1));

        CSVValues = new List<string[]>(lines.Length - 1);
        for (int lineId = 1; lineId < lines.Length; ++lineId)
        {
            var lineData = lines[lineId].Trim();
            var values = lineData.Split(',');

            if (values.Length != numberOfColumns)
                continue;

            CSVValues.Add(values);
        }
    }

    void Update()
    {
        currentUpdateTime += Time.deltaTime;
        if (currentUpdateTime >= updateStep) {
            audioLowPassFilter.cutoffFrequency = lowFrequencyCutoff;
            currentUpdateTime = 0f;
            //I read 400 samples, which is about 80 ms on a 44khz stereo clip, beginning at the current sample position of the clip.
            audioSource.clip.GetData(clipData, audioSource.timeSamples);
            clipLoudness = 0f;
            foreach (var clip in clipData) {
                clipLoudness += clip;
            }
            //clipLoudness /= sampleDataLength; 
            
            clipLoudness /= audioLowPassFilter.cutoffFrequency;
            //Debug.Log(audioLowPassFilter.cutoffFrequency);
            Debug.Log(clipLoudness);
        }
        
        if (!Application.isPlaying || CSVValues == null)
            return;

        data.timeStampList.Clear();
        foreach (FloatList column in data.valuesTable)
            column.Clear();

        if (lineId >= CSVValues.Count)
            return;

        var currentTime = Time.time;
        var lineValues = CSVValues[lineId];
        string dataTimeStr = lineValues[0];
        float dataTime = float.Parse(dataTimeStr);

        while (dataTime - timeElapsedAtStart <= currentTime - startTime)
        {
            data.timeStampList.Add(dataTime);
            for (int columnId = 0; columnId < lineValues.Length; ++columnId)
            {
                string valueStr = lineValues[columnId];
                float value = float.Parse(valueStr);
                data.valuesTable[columnId].Add(clipLoudness);
            }

            if (++lineId >= CSVValues.Count)
                return;

            lineValues = CSVValues[lineId];
            dataTimeStr = lineValues[0];
            dataTime = float.Parse(dataTimeStr);
        }
    }

    void ComputeHeaders()
    {
        if (CSVInput == null)
        {
            data.fields = null;
            return;
        }

        string[] lines = CSVInput.text.Split('\n');
        if (lines == null || lines.Length <= 0)
        {
            data.fields = null;
            return;
        }

        string firstLineData = lines[0].Trim();
        data.fields = firstLineData.Split(',');

        for (uint headerId = 0; headerId < data.fields.Length; ++headerId)
        {
            string header = data.fields[headerId];
            data.fields[headerId] = header.Replace("(", " (").Replace("/", "\u2215");
        }
    }
}
