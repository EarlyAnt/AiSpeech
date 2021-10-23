//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
// <code>
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class HelloWorld : MonoBehaviour
{
    // Hook up the three properties below with a Text, InputField and Button object in your UI.
    public Text outputText;
    public InputField inputField;
    public Button speakButton;
    public AudioSource audioSource;

    private object threadLocker = new object();
    private bool waitingForSpeak;
    private string message;

    private SpeechConfig speechConfig;
    private SpeechSynthesizer synthesizer;

    public async void ButtonClick()
    {
        await this.ButtonClickAsync();
    }

    private async Task ButtonClickAsync()
    {
        lock (threadLocker)
        {
            waitingForSpeak = true;
        }

        string content = inputField.text.Trim();
        if (string.IsNullOrEmpty(content))
        {
            content = "请在文本框中输入文字";
            inputField.ActivateInputField();
        }

        string newMessage = string.Empty;
        message = "step 1\n";

        // Starts speech synthesis, and returns after a single utterance is synthesized.
        using (var result = synthesizer.SpeakTextAsync(content).Result)
        {
            message += string.Format("step 2: {0}\n", result.Reason);
            // Checks result.
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                // Native playback is not supported on Unity yet (currently only supported on Windows/Linux Desktop).
                // Use the Unity API to play audio here as a short term solution.
                // Native playback support will be added in the future release.
                var sampleCount = result.AudioData.Length / 2;
                var audioData = new float[sampleCount];
                for (var i = 0; i < sampleCount; ++i)
                {
                    audioData[i] = (short)(result.AudioData[i * 2 + 1] << 8 | result.AudioData[i * 2]) / 32768.0F;
                }

                // The output audio format is 16K 16bit mono
                var audioClip = AudioClip.Create("SynthesizedAudio", sampleCount, 1, 16000, false);
                audioClip.SetData(audioData, 0);
                audioSource.clip = audioClip;
                audioSource.Play();

                newMessage = "Speech synthesis succeeded!";
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                newMessage = $"CANCELED:\nReason=[{cancellation.Reason}]\nErrorDetails=[{cancellation.ErrorDetails}]\nDid you update the subscription info?";
            }
        }

        //message += "step 3\n";
        //await this.SpeakSsmlAsync();
        //message += "step 4\n";

        lock (threadLocker)
        {
            message += "step 5\n";
            message += newMessage + "\n";
            waitingForSpeak = false;
        }
        message += "step 6\n";
    }

    async Task SpeakSsmlAsync()
    {
        message += "step 3.1\n";
        var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using (var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig))
        {
            message += "step 3.2\n";
            // Subscribes to viseme received event
            synthesizer.VisemeReceived += (s, e) =>
            {
                System.Console.WriteLine($"Viseme event received. Audio offset: " +
                    $"{e.AudioOffset / 10000}ms, viseme id: {e.VisemeId}.");
            };

            message += "step 3.3\n";
            var ssml = File.ReadAllText("./ssml.xml");
            var result = await synthesizer.SpeakSsmlAsync(ssml);
            message += "step 3.4\n";
        }
    }

    void Start()
    {
        if (outputText == null)
        {
            UnityEngine.Debug.LogError("outputText property is null! Assign a UI Text element to it.");
        }
        else if (inputField == null)
        {
            message = "inputField property is null! Assign a UI InputField element to it.";
            UnityEngine.Debug.LogError(message);
        }
        else if (speakButton == null)
        {
            message = "speakButton property is null! Assign a UI Button to it.";
            UnityEngine.Debug.LogError(message);
        }
        else
        {
            // Continue with normal initialization, Text, InputField and Button objects are present.
            inputField.text = "Enter text you wish spoken here.";
            message = "Click button to synthesize speech";
            speakButton.onClick.AddListener(ButtonClick);

            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            speechConfig = SpeechConfig.FromSubscription("ac1c9bf5779e420d8b3483e2b9fb5559", "chinaeast2");
            speechConfig.SpeechSynthesisLanguage = "zh-CN";

            // The default format is Riff16Khz16BitMonoPcm.
            // We are playing the audio in memory as audio clip, which doesn't require riff header.
            // So we need to set the format to Raw16Khz16BitMonoPcm.
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm);

            // Creates a speech synthesizer.
            // Make sure to dispose the synthesizer after use!
            synthesizer = new SpeechSynthesizer(speechConfig, null);
        }
    }

    void Update()
    {
        lock (threadLocker)
        {
            if (speakButton != null)
            {
                speakButton.interactable = !waitingForSpeak;
            }

            if (outputText != null)
            {
                outputText.text = message;
            }
        }
    }

    void OnDestroy()
    {
        synthesizer.Dispose();
    }
}
// </code>
