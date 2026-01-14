window.audioRecorder = (() => {
  const defaultOptions = {
    targetSampleRate: 16000,
    channelCount: 1
  };

  let recorder = null;
  let stream = null;
  let isRecording = false;
  let options = { ...defaultOptions };
  let antiforgeryToken = null;

  const getAntiforgeryToken = async () => {
    if (antiforgeryToken) {
      return antiforgeryToken;
    }

    const response = await fetch("/api/antiforgery/token", {
      method: "GET",
      credentials: "same-origin"
    });

    if (!response.ok) {
      throw new Error("Failed to get antiforgery token.");
    }

    const payload = await response.json();
    antiforgeryToken = payload.token;
    return antiforgeryToken;
  };

  const ensureRecordRtc = () => {
    if (!window.RecordRTC) {
      throw new Error("RecordRTC is not available.");
    }
  };

  const cleanup = () => {
    if (stream) {
      stream.getTracks().forEach((track) => track.stop());
    }

    if (recorder) {
      recorder.destroy();
    }

    recorder = null;
    stream = null;
    isRecording = false;
  };

  const start = async (customOptions = {}) => {
    if (isRecording) {
      return true;
    }

    options = { ...defaultOptions, ...customOptions };

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      throw new Error("Audio capture is not supported in this browser.");
    }

    ensureRecordRtc();

    stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        channelCount: options.channelCount,
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true,
        sampleRate: { ideal: 48000 },
        sampleSize: 16
      }
    });

    try {
      recorder = new RecordRTC(stream, {
        type: "audio",
        mimeType: "audio/wav",
        disableLogs: true,
        recorderType: RecordRTC.StereoAudioRecorder,
        desiredSampRate: options.targetSampleRate,
        numberOfAudioChannels: options.channelCount
      });

      recorder.startRecording();
      isRecording = true;
      return true;
    } catch (error) {
      cleanup();
      throw error;
    }
  };

  const stopInternal = async () => {
    if (!isRecording) {
      return null;
    }

    if (!recorder) {
      cleanup();
      return null;
    }

    let blob = null;
    try {
      blob = await new Promise((resolve) => {
        recorder.stopRecording(() => resolve(recorder.getBlob()));
      });
    } finally {
      cleanup();
    }

    if (!blob || blob.size === 0) {
      return null;
    }

    if (!blob.type) {
      return new Blob([blob], { type: "audio/wav" });
    }

    return blob;
  };

  const stop = async () => {
    const blob = await stopInternal();
    if (!blob) {
      return { base64: "", contentType: "" };
    }

    const base64 = await blobToBase64(blob);
    return { base64, contentType: blob.type || "audio/wav" };
  };

  const parseJsonResponse = (responseText, context) => {
    if (!responseText || !responseText.trim()) {
      return null;
    }

    try {
      return JSON.parse(responseText);
    } catch {
      const responseSummary = responseText || "<empty>";
      throw new Error(`Failed to parse server response in ${context}. Response: ${responseSummary}`);
    }
  };

  const stopAndProcess = async (subjectReference, subjectDisplay) => {
    const blob = await stopInternal();
    if (!blob) {
      throw new Error("No audio captured.");
    }

    const audioBlob = blob.type ? blob : new Blob([blob], { type: "audio/wav" });
    const formData = new FormData();
    formData.append("audio", audioBlob, "recording.wav");

    if (subjectReference) {
      formData.append("subjectReference", subjectReference);
    }

    if (subjectDisplay) {
      formData.append("subjectDisplay", subjectDisplay);
    }

    const token = await getAntiforgeryToken();
    const response = await fetch("/api/audio/process", {
      method: "POST",
      body: formData,
      headers: {
        "X-CSRF-TOKEN": token
      },
      credentials: "same-origin"
    });

    const responseText = await response.text();
    if (!response.ok) {
      let message = "Audio processing failed.";
      try {
        const problem = JSON.parse(responseText);
        message = problem.detail || problem.title || message;
      } catch {
        message = responseText || message;
      }
      throw new Error(message);
    }

    const parsedResponse = parseJsonResponse(responseText, "stopAndProcess");
    if (!parsedResponse) {
      throw new Error("Audio processing returned an empty response.");
    }

    return parsedResponse;
  };

  const blobToBase64 = (blob) =>
    new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => {
        if (typeof reader.result !== "string") {
          reject(new Error("Failed to read audio data."));
          return;
        }

        const commaIndex = reader.result.indexOf(",");
        resolve(commaIndex === -1 ? "" : reader.result.slice(commaIndex + 1));
      };
      reader.onerror = () => reject(reader.error || new Error("Failed to read audio data."));
      reader.readAsDataURL(blob);
    });

  return { start, stop, stopAndProcess };
})();
