/* Distributed under the Apache License, Version 2.0.
   See accompanying NOTICE file for details.*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

// Component used to handle a PulseEngine object, advance its simulation time,
// and broadcast the resulting data
[ExecuteInEditMode]
public class PulseEngineDriver: PulseDataSource
{
    public TextAsset initialStateFile;  // Initial stable state to load
    public SerializationFormat serializationFormat; // state file format

    [Range(0.02f, 2.0f)]
    public double timeStep = 0.02;      // Simulation time step

    [NonSerialized]
    public PulseEngine engine;          // Pulse engine to drive
    public readonly string engineDataPath = "PulseDataFiles";

    float previousTime;                 // Used to sync simulation and app times

    readonly string[] pulseDataFields = // Data fields (hardcoded for now)
    {
        "Simulation Time (s)",
        "ECG Signal (mV)",
        "Heart Rate (1\u2215min)",
        "Arterial Blood Pressure (mmHg)",
        "Mean Arterial Blood Pressure (mmHg)",
        "Systolic Arterial Blood Pressure (mmHg)",
        "Diastolic Arterial Blood Pressure (mmHg)",
        "Oxygen Saturation",
        "End Tidal Carbon Dioxide (mmHg)",
        "Respiration Rate (1\u2215min)",
        "Core Temperature (degC)",
        "Airway Carbon Dioxide (mmHg)",
        "Blood Volume (mL)"
    };


    // MARK: Monobehavior methods

    // Called when the inspector inputs are modified
    void OnValidate()
    {
        // Round down to closest factor of 0.02. Need to use doubles due to
        // issues with floats multiplication (0.1 -> 0.0999999)
        timeStep = Math.Round(timeStep / 0.02) * 0.02;
    }

    // Called when application or editor opens
    void Awake()
    {
        // Ensure we have a data container
        if (data == null)
            data = new PulseData();

        // Store data field names
        data.fields = pulseDataFields;

        // Allocate space for data times and values
        data.timeStampList = new FloatList();
        data.valuesTable = new List<FloatList>(pulseDataFields.Length);
        for (int fieldId = 0; fieldId < pulseDataFields.Length; ++fieldId)
            data.valuesTable.Add(new FloatList());
    }

    // Called at the first frame when the component is enabled
    void Start()
    {
        // Ensure we only read data if the application is playing
        // and we have a state file to initialize the engine with
        if (!Application.isPlaying || initialStateFile == null)
            return;

        // Allocate PulseEngine with path to logs and needed data files
        string dateAndTimeVar = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string logFilePath = Application.persistentDataPath + "/" +
                                        gameObject.name +
                                        dateAndTimeVar + ".log";
        string pulseDataPath = Application.streamingAssetsPath + "/" + engineDataPath;
        DirectoryInfo directoryInfo = new DirectoryInfo(pulseDataPath);
        if (!directoryInfo.Exists) {
            string error = "Data files for " + name + " not found. Expected at " + pulseDataPath + ".\n" +
            "Make sure you have copied them from the Pulse package inner 'StreamingAssets' folder.";
            throw new Exception(error);
        }
        engine = new PulseEngine(logFilePath, pulseDataPath);

        // Initialize engine state from tje state file content
        engine.SerializeFromString(initialStateFile.text,
                                   null, // requested data currently hardcoded
                                   serializationFormat,
                                   Time.time);

        previousTime = Time.time;
    }

    // Called before every frame
	void Update()
    {
        // Ensure we only broadcast data if the application is playing
        // and there a valid pulse engine to simulate data from
        if (!Application.isPlaying || engine == null)
            return;

        // Clear PulseData container
        data.timeStampList.Clear();
        for (int j = 0; j < data.valuesTable.Count; ++j)
            data.valuesTable[j].Clear();

        // Don't advance simulation if we have waited less than the time step
        float timeElapsed = Time.time - previousTime;
        if (timeElapsed < timeStep)
            return;

        // Iterate over multiple time steps if needed
        var numberOfDataPointsNeeded = Math.Round(timeElapsed / timeStep);
        for (int i = 0; i < numberOfDataPointsNeeded; ++i)
        {
            // Increment previousTime to currentTime (factored by the time step)
            previousTime += (float)timeStep;
            data.timeStampList.Add(previousTime);

            // Advance simulation by time step
            bool success = engine.AdvanceTime_s(timeStep);
            if (!success)
                continue;

            // Copy simulated data to data container
            IntPtr results = engine.PullData();
            int nbrOfValues = data.valuesTable.Count;
            double[] array = new double[nbrOfValues];
            Marshal.Copy(results, array, 0, nbrOfValues);
            for (int j = 0; j < nbrOfValues; ++j)
                data.valuesTable[j].Add((float)array[j]);
        }
	}
}
